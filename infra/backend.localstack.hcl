# Uso: terraform init -backend-config=backend.localstack.hcl
# O endpoint repete o valor de var.localstack_endpoint: blocos backend não aceitam variáveis.
bucket       = "oficina-tfstate-local"
key          = "infra/terraform.tfstate"
region       = "us-east-1"
use_lockfile = true

endpoints = {
  s3 = "http://localhost:4566"
}

access_key                  = "test"
secret_key                  = "test"
use_path_style              = true
skip_credentials_validation = true
skip_metadata_api_check     = true
skip_requesting_account_id  = true
skip_region_validation      = true
