# O ECR é opcional: as imagens da aplicação ficam no Docker Hub (images.tf), porque o k3d do LocalStack não
# consegue puxar do ECR emulado. Numa AWS real, ligar create_ecr e trocar o newName do overlay basta.
variable "create_ecr" {
  description = "Cria os repositórios ECR da aplicação (desligado: as imagens vêm do Docker Hub)"
  type        = bool
  default     = false
}

variable "ecr_repositories" {
  description = "Repositórios ECR das imagens da aplicação (API e migrations)"
  type        = set(string)
  default     = ["oficina-api", "oficina-migrations"]
}

resource "aws_ecr_repository" "app" {
  for_each = var.create_ecr ? var.ecr_repositories : toset([])

  name                 = each.key
  image_tag_mutability = "MUTABLE" # tags dev/latest são reescritas a cada push
  force_delete         = true      # permite destroy com imagens publicadas

  image_scanning_configuration {
    scan_on_push = true
  }
}

resource "aws_ecr_lifecycle_policy" "app" {
  for_each = aws_ecr_repository.app

  repository = each.value.name

  policy = jsonencode({
    rules = [{
      rulePriority = 1
      description  = "Mantem apenas as 10 imagens mais recentes"
      selection = {
        tagStatus   = "any"
        countType   = "imageCountMoreThan"
        countNumber = 10
      }
      action = {
        type = "expire"
      }
    }]
  })
}

output "ecr_repository_urls" {
  description = "URLs dos repositórios ECR (nome => URL); vazio quando create_ecr = false"
  value       = { for name, repo in aws_ecr_repository.app : name => repo.repository_url }
}
