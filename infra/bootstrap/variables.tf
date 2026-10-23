variable "localstack_endpoint" {
  description = "Endpoint do LocalStack"
  type        = string
  default     = "http://localhost:4566"
}

variable "aws_region" {
  description = "Região AWS"
  type        = string
  default     = "us-east-1"
}

variable "state_bucket_name" {
  description = "Nome do bucket que guarda o state remoto da infraestrutura"
  type        = string
  default     = "oficina-tfstate-local"
}
