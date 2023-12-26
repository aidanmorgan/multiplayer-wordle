resource "aws_dynamodb_table" "game-table" {
  name           = "${var.environment}_wordle_game"
  billing_mode   = "PROVISIONED"
  write_capacity = 1
  read_capacity  = 1
  hash_key       = "pk"
  range_key      = "sk"

  attribute {
    name = "pk"
    type = "S"
  }

  attribute {
    name = "sk"
    type = "S"
  }

  // secondary index values (needed)

  attribute {
    name = "tenant"
    type = "S"
  }

  attribute {
    name = "session_state"
    type = "S"
  }

  attribute {
    name = "created_at"
    type = "S"
  }

  global_secondary_index {
    name               = "tenant-session-index"
    hash_key           = "tenant"
    range_key          = "pk"
    write_capacity     = 1
    read_capacity      = 1
    projection_type    = "INCLUDE"
    non_key_attributes = ["state", "active_round_id", "active_round_end"]
  }

  global_secondary_index {
    name               = "round-action-required-index"
    hash_key           = "session_state"
    range_key          = "created_at"
    write_capacity     = 1
    read_capacity      = 1
    projection_type    = "INCLUDE"
    non_key_attributes = ["sk", "active_round_id", "active_round_end"]
  }

  tags = {
    Environment = var.environment
  }
}


resource "aws_dynamodb_table" "dictionary-table" {
  name           = "wordle_dictionary"
  billing_mode   = "PROVISIONED"
  write_capacity = 1
  read_capacity  = 1
  hash_key       = "pk"
  range_key      = "sk"

  attribute {
    name = "pk"
    type = "S"
  }

  attribute {
    name = "sk"
    type = "S"
  }
}

