using SimpleFFmpegGUI.FFmpegLib;
using System;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    public class InputArgumentsGenerator : ArgumentsGeneratorBase
    {
        /// <summary>
        /// Thời lượng
        /// </summary>
        /// <param name="length"></param>
        public void Duration(TimeSpan? length)
        {
            if (!length.HasValue)
            {
                return;
            }
            arguments.Add(new FFmpegArgumentItem("t", length.Value.TotalSeconds.ToString("0.000")));
        }

        /// <summary>
            /// Định dạng đầu vào
        /// </summary>
        /// <param name="format"></param>
        public void Format(string format)
        {
            if (format == null)
            {
                return;
            }
            arguments.Add(new FFmpegArgumentItem("f", format));
        }

        /// <summary>
        /// Khung hình đầu vào
        /// </summary>
        /// <param name="fps"></param>
        public void Framerate(double? fps)
        {
            if (!fps.HasValue)
            {
                return;
            }
            arguments.Add(new FFmpegArgumentItem("framerate", fps.ToString()));
        }

        /// <summary>
        /// Tệp đầu vào
        /// </summary>
        /// <param name="file"></param>
        public void Input(string file)
        {
            if (file == null)
            {
                return;
            }
            arguments.Add(new FFmpegArgumentItem("i", $"\"{file}\""));
        }

        /// <summary>
        /// Thời gian bắt đầu
        /// </summary>
        /// <param name="seek"></param>
        public void Seek(TimeSpan? seek)
        {
            if (!seek.HasValue)
            {
                return;
            }
            arguments.Add(new FFmpegArgumentItem("ss", seek.Value.TotalSeconds.ToString("0.000")));
        }

        /// <summary>
        /// Thời gian kết thúc
        /// </summary>
        /// <param name="to"></param>
        public void To(TimeSpan? to)
        {
            if (!to.HasValue)
            {
                return;
            }
            arguments.Add(new FFmpegArgumentItem("to", to.Value.TotalSeconds.ToString("0.000")));
        }
    }
}
