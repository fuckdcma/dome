namespace SimpleFFmpegGUI.FFmpegLib
{
    public class FFmpegEnums
    {
        /// <summary>
        /// Speed preset trong X264 và X265
        /// </summary>
        public readonly static string[] Presets = new[] {
            "veryslow",
            "slower",
            "slow",
            "medium",
            "fast",
            "faster",
            "veryfast",
            "superfast",
            "ultrafast",
        };

        /// <summary>
        /// Các định dạng màu được hỗ trợ (8bit-10bit)
        /// </summary>
        public readonly static string[] PixelFormats = new[] {
            "yuv420p",
            "yuvj420p",
            "yuv422p",
            "yuvj422p",
            "rgb24",
            "gray",
            "yuv420p10le"
        };
    }

}
