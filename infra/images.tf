# Referências das imagens da aplicação. O Terraform não implanta workloads (isso é do Kustomize em /k8s);
# ele só guarda o repositório e expõe as referências completas para os scripts e o CD.
variable "container_image_repository" {
  description = "Repositório (privado) do Docker Hub com as imagens da API e de migrations, separadas por tag"
  type        = string
  default     = "docker.io/thierryiago27/fiap-oficina-image-docker-hub"
}

variable "app_image_tag" {
  description = "Tag publicada pelo workflow publish-images (ex.: o SHA curto do commit); informada pelo CD"
  type        = string
  default     = null
  nullable    = true
}

output "app_images" {
  description = "Referências completas das imagens (api e migrations); null se app_image_tag não for informada"
  value = var.app_image_tag == null ? null : {
    api        = "${var.container_image_repository}:api-${var.app_image_tag}"
    migrations = "${var.container_image_repository}:migrations-${var.app_image_tag}"
  }
}
