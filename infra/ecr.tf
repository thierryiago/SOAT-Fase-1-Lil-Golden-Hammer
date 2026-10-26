variable "ecr_repositories" {
  description = "Repositórios ECR das imagens da aplicação (API e migrations)"
  type        = set(string)
  default     = ["oficina-api", "oficina-migrations"]
}

resource "aws_ecr_repository" "app" {
  for_each = var.ecr_repositories

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
  description = "URLs dos repositórios ECR (nome => URL)"
  value       = { for name, repo in aws_ecr_repository.app : name => repo.repository_url }
}
