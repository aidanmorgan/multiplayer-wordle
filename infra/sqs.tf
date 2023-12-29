resource "aws_sqs_queue" "wordle_timeout_queue" {
  name       = "${var.environment}-wordle-timeout"
  fifo_queue = false
}

resource "aws_sqs_queue" "wordle_event_handling" {
  name = "${var.environment}-wordle-eventhandling"
  fifo_queue = false
}

resource "aws_sqs_queue_policy" "game_events_policy" {
  queue_url = aws_sqs_queue.wordle_event_handling.id

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
        Resource  = aws_sqs_queue.wordle_event_handling.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = aws_cloudwatch_event_rule.all_events.arn
          }
        }
      }
    ]
  })
}

resource "aws_sqs_queue" "wordle_board_generator" {
  name = "${var.environment}-wordle-boardgenerator"
  fifo_queue = false
}

resource "aws_sqs_queue_policy" "board_generator_policy" {
  queue_url = aws_sqs_queue.wordle_board_generator.id

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
        Resource  = aws_sqs_queue.wordle_board_generator.arn
        Condition = {
          ArnEquals = {
            "aws:SourceArn" = aws_cloudwatch_event_rule.round_ended_events.arn
          }
        }
      }
    ]
  })
}

resource "aws_sqs_queue" "wordle_websocket_push" {
  name = "${var.environment}-wordle-websocket"
  fifo_queue = false
}

