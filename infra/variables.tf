variable "use_localstack" {
  description = "Quando true, o provider AWS aponta para o LocalStack; quando false, usa a AWS real"
  type        = bool
  default     = true
}

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

variable "project" {
  description = "Nome do projeto (tag Project)"
  type        = string
  default     = "oficina"
}

variable "environment" {
  description = "Nome do ambiente (tag Environment)"
  type        = string
  default     = "local"
}
