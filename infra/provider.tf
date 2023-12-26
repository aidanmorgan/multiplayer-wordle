provider "aws" {
  region = var.region
  profile = "default"
}

terraform {
  required_providers {
    aws = {
      source = "hashicorp/aws"
    }
  }

  backend "s3" {
    bucket  = "wordle-core"
    key     = "tf-state/terraform.tfstate"
    region  = "us-west-2"
    encrypt = true
  }
}
