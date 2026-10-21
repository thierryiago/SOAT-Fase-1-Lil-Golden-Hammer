terraform {
  required_version = ">= 1.10"

  required_providers {
    aws = {
      source  = "hashicorp/aws"
      version = "~> 6.69"
    }
  }

  # Configuração parcial: informe -backend-config=backend.localstack.hcl no init.
  backend "s3" {}
}
