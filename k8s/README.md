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
