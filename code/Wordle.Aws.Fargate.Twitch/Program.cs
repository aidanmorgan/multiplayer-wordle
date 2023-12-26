// See https://aka.ms/new-console-template for more information

// need to work out WTF to do here - assume we need to send RTSP?
var streamKey = Environment.GetEnvironmentVariable("TWITCH_KEY");
var streamUri = $"rtmp://syd02.contribute.live-video.net/app/{streamKey}";

