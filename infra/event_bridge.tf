resource "aws_cloudwatch_event_bus" "game_event_bus" {
  name = "${var.environment}-wordle-events"
  
}

resource "aws_cloudwatch_event_rule" "all_events" {
  name    = "${var.environment}_game_events"
  event_bus_name = aws_cloudwatch_event_bus.game_event_bus.name
  
  event_pattern  = <<EOF
  {
    "detail-type": [
      { "prefix": "wordle" }
    ]
  }
  EOF
  depends_on = [
    aws_cloudwatch_event_bus.game_event_bus
  ]
}

resource "aws_cloudwatch_event_target" "event-bus-to-sqs" {
  event_bus_name = aws_cloudwatch_event_bus.game_event_bus.name
  rule = aws_cloudwatch_event_rule.all_events.name
  
  depends_on = [
    aws_cloudwatch_event_bus.game_event_bus,
    aws_cloudwatch_event_rule.all_events,
    aws_sqs_queue.wordle_game_events
  ]
  arn = aws_sqs_queue.wordle_game_events.arn
}

