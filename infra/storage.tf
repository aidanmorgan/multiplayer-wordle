resource "aws_s3_bucket" "images_bucket" {
  bucket = "${var.environment}-wordle-images"
}

data "aws_iam_policy_document" "s3_images_policy" {
  statement {
    actions   = ["s3:GetObject"]
    resources = ["${aws_s3_bucket.images_bucket.arn}/*"]

    principals {
      type        = "AWS"
      identifiers = [aws_cloudfront_origin_access_identity.origin_access_identity.iam_arn]
    }
  }
}

resource "aws_s3_bucket_policy" "s3_images_bucket_policy" {
  bucket = aws_s3_bucket.images_bucket.id
  policy = data.aws_iam_policy_document.s3_images_policy.json
}

resource "aws_s3_bucket_public_access_block" "s3_images_access_block" {
  bucket = aws_s3_bucket.images_bucket.id

  block_public_acls       = true
  block_public_policy     = true
  //ignore_public_acls      = true
  //restrict_public_buckets = true
}