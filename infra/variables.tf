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

variable "vpc_cidr" {
  description = "CIDR da VPC"
  type        = string
  default     = "10.0.0.0/16"
}

variable "kubernetes_version" {
  description = "Versão do Kubernetes do cluster EKS (a confirmar contra o LocalStack)"
  type        = string
  default     = "1.31"
}

variable "node_instance_type" {
  description = "Tipo de instância do node group"
  type        = string
  default     = "t3.medium"
}

variable "node_min_size" {
  description = "Mínimo de nodes"
  type        = number
  default     = 2
}

variable "node_desired_size" {
  description = "Quantidade desejada de nodes"
  type        = number
  default     = 2
}

variable "node_max_size" {
  description = "Máximo de nodes"
  type        = number
  default     = 4
}
