output "aws_region" {
  description = "Região AWS em uso"
  value       = var.aws_region
}

output "environment" {
  description = "Ambiente em uso"
  value       = var.environment
}

output "localstack_endpoint" {
  description = "Endpoint do LocalStack (null quando use_localstack = false)"
  value       = var.use_localstack ? var.localstack_endpoint : null
}
