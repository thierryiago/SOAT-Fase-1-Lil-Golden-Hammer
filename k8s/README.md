# Kubernetes (Kustomize)

Manifestos da aplicação em Kustomize puro (sem Helm), aplicados com `kubectl`. O Kustomize embutido no `kubectl` é suficiente; não é preciso instalar o `kustomize`.

## Estrutura

```text
k8s/
  base/                  recursos comuns a todos os ambientes
    kustomization.yaml
    namespace.yaml       Namespace "oficina"
  overlays/
    local/               cluster local (Docker Desktop, kind, minikube)
    aws/                 cluster EKS criado pelo Terraform no LocalStack
```

A base declara `namespace: oficina`, então os recursos adicionados a ela herdam o namespace. Os overlays só ajustam o que muda por ambiente (imagens, patches, geradores) e acrescentam a label `app.kubernetes.io/environment`.

Convenção de labels dos recursos: `app.kubernetes.io/name`, `app.kubernetes.io/part-of: oficina` e `app.kubernetes.io/component`.

## Overlays

| Overlay | Cluster | Observações |
|---|---|---|
| `local` | Docker Desktop, kind ou minikube | Usa imagens locais (`oficina-api:dev`) |
| `aws` | EKS do LocalStack (k3d), criado em `/infra` | O projeto não usa a AWS real; o nome `aws` indica o EKS provisionado pelo Terraform. Imagens vêm do ECR do LocalStack |

## Como aplicar

Renderizar sem aplicar (não precisa de cluster):

```bash
kubectl kustomize k8s/overlays/local
kubectl kustomize k8s/overlays/aws
```

Aplicar no cluster do contexto atual:

```bash
kubectl apply -k k8s/overlays/local
kubectl apply -k k8s/overlays/aws
```

Para o overlay `aws`, obtenha primeiro o kubeconfig do cluster do LocalStack (veja [`infra/README.md`](../infra/README.md)); o `.` na frente é necessário no PowerShell:

```powershell
. .\infra\scripts\kubeconfig.ps1
kubectl apply -k k8s/overlays/aws
```

## Configuração e Secrets

A API recebe a configuração por variáveis de ambiente (`envFrom`), de duas origens:

| Origem | Conteúdo | Quem cria |
|---|---|---|
| ConfigMap `oficina-config-<hash>` | `ASPNETCORE_ENVIRONMENT`, `Smtp__Host/Port/From/EnableSsl`, `Jwt__Issuer/Audience`, `App__PublicBaseUrl` | Kustomize (`configMapGenerator` na base, com overrides em cada overlay) |
| Secret `oficina-secrets` | `ConnectionStrings__Database`, `Jwt__SigningKey`, `Webhooks__BudgetDecisionToken` e, opcionalmente, `Smtp__Username`/`Smtp__Password` | `k8s/scripts/create-secrets.ps1` |

O nome do ConfigMap leva um hash do conteúdo: alterar um valor gera um novo ConfigMap e dispara rollout dos pods que o referenciam. A migration **não** roda na subida da API (não existe `Database__MigrateOnStartup`); ela é aplicada pelo Job de migrations.

> `App__PublicBaseUrl` e `Webhooks__BudgetDecisionToken` ainda não são lidos pela API (issue #110). As chaves já existem para não exigir novo deploy de configuração quando forem usadas. No overlay `aws`, `App__PublicBaseUrl` é um placeholder até a exposição da API ser definida.

### Criar o Secret

Nenhum valor real é versionado: apenas [`base/secret.example.yaml`](base/secret.example.yaml), que **não** faz parte do Kustomize. O Secret é criado por script, depois do Namespace:

```powershell
. .\infra\scripts\kubeconfig.ps1                    # cluster do LocalStack (overlay aws)
$env:Jwt__SigningKey = "<chave com pelo menos 32 bytes>"
$env:Webhooks__BudgetDecisionToken = "<token>"
.\k8s\scripts\create-secrets.ps1 -Overlay aws       # connection string vem do terraform output
.\k8s\scripts\create-secrets.ps1 -Overlay aws -DryRun   # só mostra as chaves, valores mascarados
```

Cada chave vem da primeira origem disponível: variável de ambiente, valor já existente no cluster ou, só no overlay `local`, valor gerado. No `aws`, uma chave sem origem é erro. O script é idempotente (rodar de novo não altera o Secret) e nunca imprime valores.

| Overlay | `ConnectionStrings__Database` |
|---|---|
| `aws` | `terraform -chdir=infra output -raw db_connection_string`; `-DbHost` troca o host (o endpoint do RDS no LocalStack é `localhost`, que não vale dentro do pod) |
| `local` | `Host=oficina-db` (Postgres do cluster) com a senha de `POSTGRES_PASSWORD` ou gerada |

Variáveis opcionais: `POSTGRES_DB`, `POSTGRES_USER`, `Smtp__Username`, `Smtp__Password`.

### Rotacionar

O Secret fica fora do Kustomize, então mudar um valor **não** reinicia os pods. Rode o script com a nova variável e reinicie:

```bash
kubectl -n oficina rollout restart deployment/oficina-api
```

### No CD

O workflow de CD (CICD-4) faz o mesmo passo, lendo os secrets do GitHub como variáveis de ambiente e rodando `kubectl create secret generic oficina-secrets ... --dry-run=client -o yaml | kubectl apply -f -`.
