using DotNetPusher.Encoders;
using DotNetPusher.Pushers;
using DotNetPusher.VideoPackets;
using System.Drawing;

namespace RtmpStreamer
{
    public class BitmapStreamer
    {
        private Pusher _pusher;
        private Encoder _encoder;

        public string PushUrl { get; set; }
        public RtmpConfig Config { get; set; }
        public bool IsStarted { get; private set; }

        internal void StartPush()
        {
            try
            {
                _pusher = new Pusher();
                _pusher.StartPush(PushUrl, Config.Width, Config.Height, Config.FrameRate);
                _encoder = new Encoder(Config.Width, Config.Height, Config.FrameRate, Config.BitRate);
                _encoder.FrameEncoded += Encoder_FrameEncoded;
                IsStarted = true;
            }
            catch
            {
            }
        }

        internal void Stop()
        {
            try
            {
                _encoder.FrameEncoded -= Encoder_FrameEncoded;
                _encoder.Flush();
                _encoder.Dispose();
                _pusher.StopPush();
                _pusher.Dispose();
                IsStarted = false;
            }
            catch
            {
            }
        }

        internal void AddImage(Bitmap bitmap)
        {
            if (!IsStarted)
                return;

            try
            {
                _encoder.AddImage(bitmap);
            }
            catch
            {
            }
        }

        private void Encoder_FrameEncoded(object sender, FrameEncodedEventArgs e)
        {
            try
            {
                VideoPacket packet = e.Packet;
                _pusher.PushPacket(packet);
            }
            catch
            {
            }
        }
    }
}
