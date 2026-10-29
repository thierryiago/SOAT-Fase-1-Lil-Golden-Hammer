// Teste de carga da Oficina API (k6). Rampa, patamar e descida de usuários virtuais (VUs), com
// uma mistura de listagens (a maioria) e algumas aberturas de OS com itens.
//
// Variáveis (-e CHAVE=valor): BASE_URL, TARGET_VUS, RAMP_UP, HOLD, RAMP_DOWN, WRITE_RATIO,
// CUSTOMER_DOCUMENT e VEHICLE_PLATE (cliente e veículo existentes, os do seed-data.sql por padrão).
import http from 'k6/http';
import { check, sleep } from 'k6';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:8080';
const TARGET_VUS = Number(__ENV.TARGET_VUS || 30);
const WRITE_RATIO = Number(__ENV.WRITE_RATIO || 0.05);
const CUSTOMER_DOCUMENT = __ENV.CUSTOMER_DOCUMENT || '11144477735';
const VEHICLE_PLATE = __ENV.VEHICLE_PLATE || 'ABC-1234';

export const options = {
  stages: [
    { duration: __ENV.RAMP_UP || '1m', target: TARGET_VUS },
    { duration: __ENV.HOLD || '3m', target: TARGET_VUS },
    { duration: __ENV.RAMP_DOWN || '30s', target: 0 },
  ],
  thresholds: {
    http_req_failed: ['rate<0.05'],
  },
};

const json = { 'Content-Type': 'application/json' };

function authHeaders(token) {
  return { headers: { ...json, Authorization: `Bearer ${token}` } };
}

// Roda uma vez: token técnico (sem usuário/senha) e os dados usados nas aberturas de OS.
export function setup() {
  const token = http.post(`${BASE_URL}/api/v1/auth/token`).json('accessToken');
  const opts = authHeaders(token);
  const suffix = `${Date.now()}`;

  const service = http.post(`${BASE_URL}/api/v1/workshop-services`, JSON.stringify({
    name: `Carga ${suffix}`, description: 'Serviço do teste de carga', unitPrice: 100, estimatedDurationMinutes: 30,
  }), opts).json();
  const part = http.post(`${BASE_URL}/api/v1/parts`, JSON.stringify({
    name: `Peça carga ${suffix}`, code: `LT-${suffix}`, unitPrice: 10, kind: 1,
  }), opts).json();
  // Estoque grande o bastante para as aberturas de OS do teste inteiro.
  http.put(`${BASE_URL}/api/v1/stocks/stocks-part/${part.id}/entries`, JSON.stringify({ quantity: 100000 }), opts);

  return { token, serviceId: service.id, partId: part.id };
}

export default function (data) {
  const opts = authHeaders(data.token);

  if (Math.random() < WRITE_RATIO) {
    const res = http.post(`${BASE_URL}/api/v1/service-orders/with-items`, JSON.stringify({
      customer: { document: CUSTOMER_DOCUMENT },
      vehicle: { plate: VEHICLE_PLATE },
      description: 'OS do teste de carga',
      workshopServiceIds: [data.serviceId],
      parts: [{ partId: data.partId, quantity: 1 }],
    }), opts);
    check(res, { 'OS aberta (2xx)': (r) => r.status >= 200 && r.status < 300 });
  } else {
    const paths = [
      '/api/v1/customers?page=1&pageSize=50',
      '/api/v1/vehicles?page=1&pageSize=50',
      '/api/v1/parts?page=1&pageSize=50',
      '/api/v1/service-orders?page=1&pageSize=50',
      '/api/v1/metrics/workshop-service/execution-time',
    ];
    const res = http.get(`${BASE_URL}${paths[Math.floor(Math.random() * paths.length)]}`, opts);
    check(res, { 'listagem 200': (r) => r.status === 200 });
  }
  sleep(Math.random() * 0.5);
}
