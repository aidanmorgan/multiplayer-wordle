resource "aws_sqs_queue" "wordle_timeout_queue" {
  name       = "${var.environment}-wordle-timeout"
  fifo_queue = false
}

resource "aws_sqs_queue" "wordle_game_events" {
  name       = "${var.environment}-wordle-game"
  fifo_queue = false
}

resource "aws_sqs_queue_policy" "game_events_policy" {
  queue_url = aws_sqs_queue.wordle_game_events.id

  policy = jsonencode({
    Version   = "2012-10-17"
    Id        = "AllowSendMessageToSqs"
    Statement = [
      {
        Sid       = "AllowEventBridgeToSendMessages"
        Effect    = "Allow"
        Principal = {
          Service = "events.amazonaws.com"
        }
        Action    = "sqs:SendMessage"
        Resource  = aws_sqs_queue.wordle_game_events.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = aws_cloudwatch_event_rule.all_events.arn
          }
        }
      }
    ]
  })
}