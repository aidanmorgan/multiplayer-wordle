resource "aws_sqs_queue" "round_end_events" {
  name       = "${var.environment}-wordle_timeout_queue"
  fifo_queue = false
}

resource "aws_sqs_queue" events {
  name       = "${var.environment}-wordle_events_queue"
  fifo_queue = false
}
