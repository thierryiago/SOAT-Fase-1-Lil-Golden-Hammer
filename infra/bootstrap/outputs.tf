output "state_bucket_name" {
  description = "Bucket do state remoto"
  value       = aws_s3_bucket.state.bucket
}
