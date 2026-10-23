provider "aws" {
  region = var.aws_region

  # Tudo roda no LocalStack: credenciais fictícias e validações desligadas.
  access_key                  = "test"
  secret_key                  = "test"
  s3_use_path_style           = true
  skip_credentials_validation = true
  skip_metadata_api_check     = true
  skip_requesting_account_id  = true

  endpoints {
    s3  = var.localstack_endpoint
    ec2 = var.localstack_endpoint
    eks = var.localstack_endpoint
    rds = var.localstack_endpoint
    ecr = var.localstack_endpoint
    iam = var.localstack_endpoint
    sts = var.localstack_endpoint
  }

  default_tags {
    tags = {
      Project     = var.project
      Environment = var.environment
    }
  }
}
