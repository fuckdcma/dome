namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class CodecBase
    {
        /// <summary>
        /// Tên codec
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Tên thư viện codec trong FFmpeg
        /// </summary>
        public abstract string Lib { get; }
    }

}
