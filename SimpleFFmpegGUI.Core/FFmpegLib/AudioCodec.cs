using SimpleFFmpegGUI.FFmpegArgument;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class AudioCodec : CodecBase
    {
        public static readonly AudioCodec[] AudioCodecs = new AudioCodec[]
        {
            new AAC(),
            new OPUS()
        };

        /// <summary>
        /// Bitrate
        /// </summary>
        /// <param name="kb"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public virtual FFmpegArgumentItem Bitrate(double kb)
        {
            if (kb < 0)
            {
                throw new FFmpegArgumentException("Bitrate vượt quá phạm vi");
            }
            return new FFmpegArgumentItem("b:a", $"{kb}K");
        }

        /// <summary>
        /// Tần số lấy mẫu
        /// </summary>
        /// <param name="hz"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public virtual FFmpegArgumentItem SamplingRate(int hz)
        {
            if (hz < 9600)
            {
                throw new FFmpegArgumentException("Tần số lấy mẫu vượt quá phạm vi");
            }
            return new FFmpegArgumentItem("ar", hz.ToString());
        }
    }

}
