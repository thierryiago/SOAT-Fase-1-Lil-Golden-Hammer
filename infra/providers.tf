provider "aws" {
  region = var.aws_region

  # Credenciais fictícias e validações desligadas apenas no LocalStack.
  access_key = var.use_localstack ? "test" : null
  secret_key = var.use_localstack ? "test" : null

  s3_use_path_style           = var.use_localstack
  skip_credentials_validation = var.use_localstack
  skip_metadata_api_check     = var.use_localstack
  skip_requesting_account_id  = var.use_localstack

  dynamic "endpoints" {
    for_each = var.use_localstack ? [var.localstack_endpoint] : []

    content {
      s3  = endpoints.value
      ec2 = endpoints.value
      eks = endpoints.value
      rds = endpoints.value
      ecr = endpoints.value
      iam = endpoints.value
      sts = endpoints.value
    }
  }

  default_tags {
    tags = {
      Project     = var.project
      Environment = var.environment
    }
  }
}
