namespace RtmpStreamer
{
    public class RtmpConfig
    {
        public int Width { get; set; }
        public int Height { get; set; }
        public int FrameRate { get; set; }
        public int BitRate { get { return Width * Height; } }
    }
}
