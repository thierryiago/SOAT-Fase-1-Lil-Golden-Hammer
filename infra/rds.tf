resource "random_password" "db" {
  length  = 24
  special = false
}

resource "aws_db_subnet_group" "main" {
  name       = "${local.cluster_name}-db"
  subnet_ids = aws_subnet.private[*].id

  tags = {
    Name = "${local.cluster_name}-db"
  }
}

resource "aws_security_group" "db" {
  name        = "${local.cluster_name}-db"
  description = "Security group do banco, acessivel apenas pelo cluster"
  vpc_id      = aws_vpc.main.id

  ingress {
    description     = "PostgreSQL a partir do security group do cluster"
    from_port       = 5432
    to_port         = 5432
    protocol        = "tcp"
    security_groups = [aws_security_group.eks_cluster.id]
  }

  tags = {
    Name = "${local.cluster_name}-db"
  }
}

resource "aws_db_instance" "main" {
  identifier = "${local.cluster_name}-db"

  engine         = "postgres"
  engine_version = "16"
  instance_class = var.db_instance_class

  allocated_storage = 20
  storage_type      = "gp3"

  db_name  = var.db_name
  username = var.db_username
  password = random_password.db.result

  db_subnet_group_name   = aws_db_subnet_group.main.name
  vpc_security_group_ids = [aws_security_group.db.id]
  publicly_accessible    = false

  skip_final_snapshot = true
  deletion_protection = false
}

output "db_endpoint" {
  description = "Endereço do banco (host)"
  value       = aws_db_instance.main.address
  sensitive   = true
}

output "db_port" {
  description = "Porta do banco"
  value       = aws_db_instance.main.port
  sensitive   = true
}

output "db_name" {
  description = "Nome do banco"
  value       = aws_db_instance.main.db_name
  sensitive   = true
}

output "db_username" {
  description = "Usuário do banco"
  value       = aws_db_instance.main.username
  sensitive   = true
}

output "db_password" {
  description = "Senha do banco"
  value       = random_password.db.result
  sensitive   = true
}

output "db_connection_string" {
  description = "Connection string no formato Npgsql"
  value       = "Host=${aws_db_instance.main.address};Port=${aws_db_instance.main.port};Database=${aws_db_instance.main.db_name};Username=${aws_db_instance.main.username};Password=${random_password.db.result}"
  sensitive   = true
}
