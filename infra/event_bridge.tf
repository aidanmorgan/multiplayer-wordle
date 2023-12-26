resource "aws_cloudwatch_event_bus" "event-bus" {
  name = "${var.environment}-wordle-events"
}

resource "aws_cloudwatch_event_rule" "all_events" {
  name          = "all_events"
  event_pattern = <<EOF
{
  "source": [
    { "prefix": "wordle" }
  ]
}
EOF
  targets = {
    processing = [
      {
        name            = "forward-events-to-sqs"
        arn             = aws_sqs_queue.events.arn
      }
    ]
  }

  event_bus_name = aws_cloudwatch_event_bus.event-bus.name
}

