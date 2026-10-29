// Teste de ponta a ponta da Oficina API (k6), executado dentro do cluster. Percorre o fluxo de negócio do
// oficina.http (cadastros, OS de Created a Delivered, orçamento, histórico, estoque, métricas e e-mails no
// Mailpit) e gera um relatório HTML com cada chamada feita e o que a API devolveu.
//
// O fluxo roda inteiro no setup() (1 VU, 1 iteração) e o handleSummary() monta o relatório a partir do
// setup_data. O pod do Job some ao terminar, então o relatório sai pelo stdout, em base64, entre marcadores;
// k8s/e2e/run-e2e.ps1 extrai e grava os arquivos.
//
// Variáveis (-e CHAVE=valor): BASE_URL, MAILPIT_URL, API_IMAGE e RUN_URL (os dois últimos só aparecem
// no cabeçalho do relatório).
import http from 'k6/http';
import { check, sleep } from 'k6';
import encoding from 'k6/encoding';

const BASE_URL = __ENV.BASE_URL || 'http://oficina-api.oficina.svc.cluster.local';
const MAILPIT_URL = __ENV.MAILPIT_URL || 'http://mailpit.oficina.svc.cluster.local:8025';
const REPORT_BEGIN = '@@OFICINA-E2E-BEGIN@@';
const REPORT_END = '@@OFICINA-E2E-END@@';
const MAX_BODY_CHARS = 6000;

export const options = {
  vus: 1,
  iterations: 1,
  setupTimeout: '5m',
  thresholds: {
    checks: ['rate==1'],
  },
};

// Documento com dígitos verificadores válidos e dados únicos: o fluxo pode rodar de novo no mesmo banco.
function generateCpf() {
  const digits = Array.from({ length: 9 }, () => Math.floor(Math.random() * 10));
  if (new Set(digits).size === 1) digits[0] = (digits[0] + 1) % 10;
  for (const length of [9, 10]) {
    let sum = 0;
    for (let i = 0; i < length; i++) sum += digits[i] * (length + 1 - i);
    const remainder = (sum * 10) % 11;
    digits.push(remainder === 10 ? 0 : remainder);
  }
  return digits.join('');
}

function generateMercosulPlate() {
  const letter = () => String.fromCharCode(65 + Math.floor(Math.random() * 26));
  const digit = () => Math.floor(Math.random() * 10);
  return `${letter()}${letter()}${letter()}${digit()}${letter()}${digit()}${digit()}`;
}

function maskSecrets(text) {
  return text.replace(/("accessToken"\s*:\s*")[^"]+"/g, '$1***"');
}

function formatBody(body) {
  if (body === null || body === undefined || body === '') return '';
  let text = String(body);
  try {
    text = JSON.stringify(JSON.parse(text), null, 2);
  } catch (_) {
    // Corpo não-JSON (ex.: texto do health check) fica como veio.
  }
  text = maskSecrets(text);
  return text.length > MAX_BODY_CHARS
    ? `${text.slice(0, MAX_BODY_CHARS)}\n… (${text.length - MAX_BODY_CHARS} caracteres omitidos)`
    : text;
}

class Flow {
  constructor() {
    this.steps = [];
    this.token = null;
  }

  // Executa uma chamada, registra no relatório e valida status e regras do passo.
  // `validate` recebe o JSON da resposta e devolve uma mensagem de erro (string) ou nada.
  call({ group, name, method, path, body, expectedStatus, anonymous = false, baseUrl = BASE_URL, validate }) {
    const url = `${baseUrl}${path}`;
    const headers = { Accept: 'application/json' };
    const payload = body === undefined ? null : JSON.stringify(body);
    if (payload !== null) headers['Content-Type'] = 'application/json';
    if (!anonymous && this.token) headers.Authorization = `Bearer ${this.token}`;

    const response = http.request(method, url, payload, { headers, tags: { name } });
    let json = null;
    try {
      json = response.json();
    } catch (_) {
      json = null;
    }

    const problems = [];
    if (response.status !== expectedStatus) {
      problems.push(`status ${response.status}, esperado ${expectedStatus}`);
    } else if (validate) {
      const problem = validate(json);
      if (problem) problems.push(problem);
    }
    const passed = problems.length === 0;
    check(response, { [`${group}: ${name}`]: () => passed });

    this.steps.push({
      group,
      name,
      method,
      url,
      requestHeaders: Object.entries(headers).map(([key, value]) =>
        key === 'Authorization' ? `${key}: Bearer ***` : `${key}: ${value}`),
      requestBody: payload === null ? '' : formatBody(payload),
      expectedStatus,
      status: response.status,
      durationMs: Math.round(response.timings.duration),
      responseBody: formatBody(response.body),
      result: passed ? 'passed' : 'failed',
      problem: problems.join('; '),
    });
    return passed ? json ?? {} : null;
  }

  skip(group, name, reason) {
    check(null, { [`${group}: ${name}`]: () => false });
    this.steps.push({ group, name, result: 'skipped', problem: reason });
    return null;
  }

  // Encadeia passos: se uma dependência falhou, o passo é registrado como não executado em vez de lançar.
  then(dependencies, group, name, run) {
    const missing = dependencies.filter((dependency) => !dependency);
    return missing.length > 0 ? this.skip(group, name, 'dependência anterior falhou') : run();
  }
}

function expectStatus(expected) {
  return (json) => (json && json.status === expected ? null : `status da OS ${json && json.status}, esperado ${expected}`);
}

const MAIN_FLOW_STATUSES = ['Created', 'Received', 'InDiagnosis', 'AwaitingApproval', 'InExecution', 'Finalized', 'Delivered'];

function runFlow(flow) {
  const suffix = `${Date.now()}`;
  const document = generateCpf();
  const email = `e2e.${suffix}@example.com`;

  flow.call({
    group: 'Saúde', name: 'API e banco prontos (/health/ready)', method: 'GET', path: '/health/ready',
    expectedStatus: 200, anonymous: true,
  });

  const token = flow.call({
    group: 'Autenticação', name: 'Emitir token técnico', method: 'POST', path: '/api/v1/auth/token',
    expectedStatus: 200, anonymous: true,
    validate: (json) => (json && json.accessToken ? null : 'accessToken ausente'),
  });
  if (!token) return;
  flow.token = token.accessToken;

  const customer = flow.call({
    group: 'Cadastros', name: 'Cadastrar cliente', method: 'POST', path: '/api/v1/customers', expectedStatus: 201,
    body: { name: 'Cliente E2E', email, telephoneNumber: '+5511999990001', document },
  });
  flow.then([customer], 'Cadastros', 'Consultar cliente por ID', () => flow.call({
    group: 'Cadastros', name: 'Consultar cliente por ID', method: 'GET', path: `/api/v1/customers/${customer.id}`,
    expectedStatus: 200, validate: (json) => (json.document === document ? null : 'documento diferente do cadastrado'),
  }));
  const vehicle = flow.then([customer], 'Cadastros', 'Cadastrar veículo', () => flow.call({
    group: 'Cadastros', name: 'Cadastrar veículo', method: 'POST', path: '/api/v1/vehicles', expectedStatus: 201,
    body: { customerId: customer.id, plate: generateMercosulPlate(), brand: 'Honda', model: 'Civic', year: 2022, category: 1 },
  }));
  const mechanic = flow.call({
    group: 'Cadastros', name: 'Cadastrar mecânico', method: 'POST', path: '/api/v1/mechanics', expectedStatus: 201,
    body: { name: `Mecânico E2E ${suffix}` },
  });
  const service = flow.call({
    group: 'Cadastros', name: 'Cadastrar serviço', method: 'POST', path: '/api/v1/workshop-services', expectedStatus: 201,
    body: { name: `Revisão E2E ${suffix}`, description: 'Serviço do teste de ponta a ponta', unitPrice: 180.5, estimatedDurationMinutes: 60 },
  });
  const part = flow.call({
    group: 'Cadastros', name: 'Cadastrar peça', method: 'POST', path: '/api/v1/parts', expectedStatus: 201,
    body: { name: `Filtro E2E ${suffix}`, code: `E2E-${suffix}`, unitPrice: 45.9, kind: 1 },
  });
  const stock = flow.then([part], 'Estoque', 'Entrada de 10 unidades', () => flow.call({
    group: 'Estoque', name: 'Entrada de 10 unidades', method: 'PUT', path: `/api/v1/stocks/stocks-part/${part.id}/entries`,
    expectedStatus: 200, body: { quantity: 10 },
    validate: (json) => (json.quantity === 10 ? null : `quantidade ${json.quantity}, esperado 10`),
  }));

  const group = 'Ordem de serviço';
  let order = flow.then([customer, vehicle], group, 'Abrir OS', () => flow.call({
    group, name: 'Abrir OS', method: 'POST', path: '/api/v1/service-orders', expectedStatus: 201,
    body: { customerId: customer.id, vehicleId: vehicle.id, description: 'OS do teste de ponta a ponta' },
    validate: expectStatus('Created'),
  }));
  const orderId = order && order.id;
  const transitions = [
    { name: 'Receber (checklist)', deps: [], body: () => ({ serviceOrderId: orderId, checkList: 'Inspeção inicial concluída' }), status: 'Received' },
    { name: 'Iniciar diagnóstico (mecânico)', deps: [mechanic], body: () => ({ serviceOrderId: orderId, mechanicId: mechanic.id }), status: 'InDiagnosis' },
    { name: 'Associar serviço e pedir aprovação', deps: [service], body: () => ({ serviceOrderId: orderId, workshopServiceIds: [service.id] }), status: 'AwaitingApproval' },
  ];
  for (const transition of transitions) {
    order = flow.then([order, ...transition.deps], group, transition.name, () => flow.call({
      group, name: transition.name, method: 'PUT', path: '/api/v1/service-orders', expectedStatus: 200,
      body: transition.body(), validate: expectStatus(transition.status),
    }));
  }

  const budget = flow.then([order], 'Orçamento', 'Localizar orçamento da OS', () => findBudget(flow, orderId));
  flow.then([budget], 'Orçamento', 'Consultar orçamento por ID', () => flow.call({
    group: 'Orçamento', name: 'Consultar orçamento por ID', method: 'GET', path: `/api/v1/budgets/${budget.id}`,
    expectedStatus: 200, validate: (json) => (json.totalValue > 0 ? null : 'valor total do orçamento zerado'),
  }));

  for (const [action, status] of [['approve', 'InExecution'], ['finalize', 'Finalized'], ['deliver', 'Delivered']]) {
    order = flow.then([order], group, `${action} → ${status}`, () => flow.call({
      group, name: `${action} → ${status}`, method: 'POST', path: `/api/v1/service-orders/${orderId}/${action}`,
      expectedStatus: 200, validate: expectStatus(status),
    }));
  }

  flow.then([order], 'Histórico', 'Histórico com os 7 status', () => flow.call({
    group: 'Histórico', name: 'Histórico com os 7 status', method: 'GET',
    path: `/api/v1/service-order-history/service-order/${orderId}`, expectedStatus: 200,
    validate: (json) => {
      const recorded = (json || []).map((entry) => entry.statusName);
      const missing = MAIN_FLOW_STATUSES.filter((status) => !recorded.includes(status));
      return missing.length === 0 ? null : `faltam no histórico: ${missing.join(', ')}`;
    },
  }));
  flow.then([order], 'Histórico', 'Acompanhamento público pelo documento', () => flow.call({
    group: 'Histórico', name: 'Acompanhamento público pelo documento', method: 'GET',
    path: `/api/v1/service-orders/${orderId}/track?document=${document}`, expectedStatus: 200, anonymous: true,
    validate: expectStatus('Delivered'),
  }));

  flow.then([customer, vehicle, service, stock], 'OS com itens', 'Abrir OS com serviço e 2 peças', () => flow.call({
    group: 'OS com itens', name: 'Abrir OS com serviço e 2 peças', method: 'POST', path: '/api/v1/service-orders/with-items',
    expectedStatus: 201,
    body: {
      customer: { document }, vehicle: { plate: vehicle.plate }, description: 'OS com itens do teste de ponta a ponta',
      workshopServiceIds: [service.id], parts: [{ partId: part.id, quantity: 2 }],
    },
    validate: expectStatus('AwaitingApproval'),
  }));
  flow.then([stock], 'OS com itens', 'Consultar estoque da peça', () => flow.call({
    group: 'OS com itens', name: 'Consultar estoque da peça', method: 'GET', path: `/api/v1/stocks/${stock.id}`,
    expectedStatus: 200,
  }));

  flow.call({
    group: 'Métricas', name: 'Tempo médio de execução por serviço', method: 'GET',
    path: '/api/v1/metrics/workshop-service/execution-time', expectedStatus: 200,
  });

  flow.then([customer], 'Notificações', 'E-mails do cliente no Mailpit', () => findEmails(flow, email));
}

// A listagem de orçamentos não filtra por OS: percorre as páginas e registra só a página onde encontrou.
function findBudget(flow, serviceOrderId) {
  for (let page = 1; page <= 20; page++) {
    const path = `/api/v1/budgets?page=${page}&pageSize=100`;
    const response = http.get(`${BASE_URL}${path}`, { headers: { Authorization: `Bearer ${flow.token}` } });
    if (response.status !== 200) break;
    const result = response.json();
    const found = (result.items || []).some((budget) => budget.serviceOrderId === serviceOrderId);
    if (found || page >= result.totalPages) {
      return flow.call({
        group: 'Orçamento', name: 'Localizar orçamento da OS', method: 'GET', path, expectedStatus: 200,
        validate: (json) => (findBudgetIn(json, serviceOrderId) ? null : 'nenhuma versão de orçamento para a OS'),
      }) && findBudgetIn(result, serviceOrderId);
    }
  }
  return flow.skip('Orçamento', 'Localizar orçamento da OS', 'listagem de orçamentos indisponível');
}

function findBudgetIn(page, serviceOrderId) {
  return ((page && page.items) || []).find((budget) => budget.serviceOrderId === serviceOrderId) || null;
}

// O envio é síncrono no caso de uso, mas o Mailpit indexa a mensagem logo depois: tenta algumas vezes.
function findEmails(flow, email) {
  const path = `/api/v1/search?query=${encodeURIComponent(`to:"${email}"`)}`;
  for (let attempt = 1; attempt <= 10; attempt++) {
    const response = http.get(`${MAILPIT_URL}${path}`);
    if (response.status === 200 && (response.json('messages_count') || 0) > 0) break;
    sleep(1);
  }
  return flow.call({
    group: 'Notificações', name: 'E-mails do cliente no Mailpit', method: 'GET', path, expectedStatus: 200,
    anonymous: true, baseUrl: MAILPIT_URL,
    validate: (json) => (json && json.messages_count > 0 ? null : 'nenhum e-mail recebido para o cliente'),
  });
}

export function setup() {
  const flow = new Flow();
  const startedAt = new Date().toISOString();
  try {
    runFlow(flow);
  } catch (error) {
    flow.skip('Execução', 'Erro inesperado no script', String(error));
  }
  return { startedAt, finishedAt: new Date().toISOString(), steps: flow.steps };
}

export default function () {
  // Todo o fluxo roda no setup(); a iteração só existe porque o k6 exige uma.
}

function escapeHtml(text) {
  return String(text ?? '')
    .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
}

const RESULT_LABELS = { passed: 'OK', failed: 'Falhou', skipped: 'Não executado' };

function renderStep(step, index) {
  const label = RESULT_LABELS[step.result];
  const summary = step.method
    ? `<span class="method">${step.method}</span> <code>${escapeHtml(step.url)}</code>`
    : '';
  const timing = step.method ? `<span class="meta">${step.status} · ${step.durationMs} ms</span>` : '';
  const problem = step.problem ? `<p class="problem">${escapeHtml(step.problem)}</p>` : '';
  const details = step.method ? `
      <div class="io">
        <section>
          <h4>Requisição</h4>
          <pre>${escapeHtml(`${step.method} ${step.url}\n${step.requestHeaders.join('\n')}`)}${step.requestBody ? `\n\n${escapeHtml(step.requestBody)}` : ''}</pre>
        </section>
        <section>
          <h4>Resposta <span class="meta">(esperado ${step.expectedStatus}, recebido ${step.status})</span></h4>
          <pre>${escapeHtml(step.responseBody) || '<em>sem corpo</em>'}</pre>
        </section>
      </div>` : '';
  return `
    <details class="step ${step.result}"${step.result === 'passed' ? '' : ' open'}>
      <summary>
        <span class="index">${index + 1}</span>
        <span class="badge ${step.result}">${label}</span>
        <span class="name"><span class="group">${escapeHtml(step.group)}</span> ${escapeHtml(step.name)}</span>
        ${timing}
      </summary>
      <div class="body">${summary ? `<p>${summary}</p>` : ''}${problem}${details}</div>
    </details>`;
}

function renderHtml(report, totals) {
  const runLink = __ENV.RUN_URL ? `<a href="${escapeHtml(__ENV.RUN_URL)}">${escapeHtml(__ENV.RUN_URL)}</a>` : 'execução local';
  const verdict = totals.failed + totals.skipped === 0 ? 'passed' : 'failed';
  return `<!doctype html>
<html lang="pt-BR">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Relatório E2E Oficina</title>
<style>
  :root { --bg:#f7f7f8; --card:#fff; --text:#1d1d1f; --muted:#6b6b70; --border:#e2e2e6; --code:#f1f1f4;
          --ok:#1f7a3d; --ok-bg:#e3f4e8; --fail:#b42318; --fail-bg:#fde7e5; --skip:#8a5a00; --skip-bg:#fff2d6; }
  @media (prefers-color-scheme: dark) {
    :root { --bg:#141416; --card:#1e1e21; --text:#ececef; --muted:#9a9aa1; --border:#323238; --code:#26262a;
            --ok:#6fd38f; --ok-bg:#183a24; --fail:#ff8a7e; --fail-bg:#4a1d19; --skip:#f2c35b; --skip-bg:#3d3015; }
  }
  * { box-sizing: border-box; }
  body { margin:0; background:var(--bg); color:var(--text); font:15px/1.5 system-ui, -apple-system, "Segoe UI", sans-serif; }
  main { max-width:1100px; margin:0 auto; padding:24px 16px 48px; }
  h1 { font-size:24px; margin:0 0 4px; }
  .context { color:var(--muted); margin:0 0 20px; word-break:break-all; }
  .totals { display:grid; grid-template-columns:repeat(auto-fit, minmax(140px, 1fr)); gap:12px; margin-bottom:24px; }
  .tile { background:var(--card); border:1px solid var(--border); border-radius:10px; padding:12px 14px; }
  .tile b { display:block; font-size:22px; }
  .tile span { color:var(--muted); font-size:13px; }
  .verdict.passed b { color:var(--ok); } .verdict.failed b { color:var(--fail); }
  .step { background:var(--card); border:1px solid var(--border); border-radius:10px; margin-bottom:8px; }
  .step.failed { border-color:var(--fail); } .step.skipped { border-color:var(--skip); }
  summary { display:flex; flex-wrap:wrap; align-items:center; gap:10px; padding:10px 14px; cursor:pointer; }
  .index { color:var(--muted); font-variant-numeric:tabular-nums; min-width:22px; }
  .badge { font-size:12px; font-weight:600; padding:2px 8px; border-radius:999px; }
  .badge.passed { color:var(--ok); background:var(--ok-bg); }
  .badge.failed { color:var(--fail); background:var(--fail-bg); }
  .badge.skipped { color:var(--skip); background:var(--skip-bg); }
  .name { flex:1; min-width:200px; } .group { color:var(--muted); margin-right:4px; }
  .meta { color:var(--muted); font-size:13px; font-variant-numeric:tabular-nums; }
  .body { padding:0 14px 14px; } .body p { margin:4px 0 10px; word-break:break-all; }
  .method { font-weight:700; } .problem { color:var(--fail); }
  .io { display:grid; grid-template-columns:1fr 1fr; gap:12px; }
  @media (max-width: 760px) { .io { grid-template-columns:1fr; } }
  h4 { margin:0 0 6px; font-size:13px; }
  pre { margin:0; background:var(--code); border-radius:8px; padding:10px; overflow:auto; max-height:420px;
        font:12px/1.45 ui-monospace, SFMono-Regular, Consolas, monospace; white-space:pre-wrap; word-break:break-word; }
  code { font:13px ui-monospace, SFMono-Regular, Consolas, monospace; }
</style>
</head>
<body>
<main>
  <h1>Teste de ponta a ponta da Oficina API</h1>
  <p class="context">Imagem: <code>${escapeHtml(__ENV.API_IMAGE || 'não informada')}</code> · Execução: ${runLink}<br>
     Início ${escapeHtml(report.startedAt)} · Fim ${escapeHtml(report.finishedAt)} · Chamadas de dentro do cluster para <code>${escapeHtml(BASE_URL)}</code></p>
  <div class="totals">
    <div class="tile verdict ${verdict}"><b>${verdict === 'passed' ? 'Aprovado' : 'Reprovado'}</b><span>resultado</span></div>
    <div class="tile"><b>${totals.passed}/${totals.total}</b><span>passos OK</span></div>
    <div class="tile"><b>${totals.failed}</b><span>falhas</span></div>
    <div class="tile"><b>${totals.skipped}</b><span>não executados</span></div>
    <div class="tile"><b>${totals.durationMs} ms</b><span>tempo nas chamadas</span></div>
  </div>
  ${report.steps.map(renderStep).join('')}
</main>
</body>
</html>`;
}

function renderMarkdown(report, totals) {
  const icon = { passed: '✅', failed: '❌', skipped: '⏭️' };
  const lines = [
    '### Teste de ponta a ponta da API',
    '',
    `**${totals.passed}/${totals.total} passos OK**, ${totals.failed} falha(s), ${totals.skipped} não executado(s). ` +
      'O relatório completo (requisições e respostas) está no artifact `relatorio-e2e`.',
    '',
    '| # | Resultado | Etapa | Chamada | Status | Tempo |',
    '|---|---|---|---|---|---|',
  ];
  report.steps.forEach((step, index) => {
    const call = step.method ? `\`${step.method} ${step.url.replace(/^https?:\/\/[^/]+/, '')}\`` : '';
    const status = step.method ? `${step.status}` : '';
    const detail = step.problem ? ` (${step.problem.replace(/\|/g, '\\|')})` : '';
    lines.push(`| ${index + 1} | ${icon[step.result]} | ${step.group}: ${step.name}${detail} | ${call} | ${status} | ${step.durationMs ?? ''} |`);
  });
  return lines.join('\n');
}

export function handleSummary(data) {
  const report = data.setup_data || { startedAt: '', finishedAt: '', steps: [] };
  if (report.steps.length === 0) {
    report.steps.push({ group: 'Execução', name: 'Fluxo não executado', result: 'skipped', problem: 'setup() não retornou dados' });
  }
  const count = (result) => report.steps.filter((step) => step.result === result).length;
  const totals = {
    total: report.steps.length,
    passed: count('passed'),
    failed: count('failed'),
    skipped: count('skipped'),
    durationMs: report.steps.reduce((sum, step) => sum + (step.durationMs || 0), 0),
  };
  const payload = JSON.stringify({ html: renderHtml(report, totals), markdown: renderMarkdown(report, totals) });
  return { stdout: `\n${REPORT_BEGIN}\n${encoding.b64encode(payload, 'std')}\n${REPORT_END}\n` };
}
