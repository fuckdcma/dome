using SimpleFFmpegGUI.FFmpegLib;
using System.Collections.Generic;
using System.Linq;
using VideoCodec = SimpleFFmpegGUI.FFmpegLib.VideoCodec;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    public class VideoArgumentsGenerator : ArgumentsGeneratorBase
    {
        /// <summary>
        /// Bitrate tối đa
        /// </summary>
        private double? maxBitrate;
        
        /// <summary>
        /// Video encoding
        /// </summary>
        public VideoCodec VideoCodec { get; private set; }

        /// <summary>
        /// Tỷ lệ khung hình
        /// </summary>
        /// <param name="aspect"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public VideoArgumentsGenerator Aspect(string aspect)
        {
            if (string.IsNullOrEmpty(aspect))
            {
                return this;
            }
            if (double.TryParse(aspect, out _))
            {
                arguments.Add(new FFmpegArgumentItem("aspect", aspect));
                return this;
            }
            string[] parts = aspect.Split(':');
            if (parts.Length == 2 || double.TryParse(parts[0], out _) && double.TryParse(parts[1], out _))
            {
                arguments.Add(new FFmpegArgumentItem("aspect", aspect));
                return this;
            }
            throw new FFmpegArgumentException("Định dạng tỷ lệ khung hình không thể giải mã");
        }

        /// <summary>
        /// Bitrate trung bình
        /// </summary>
        /// <param name="mb"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator AverageBitrate(double? mb)
        {
            if (mb.HasValue)
            {
                arguments.Add(VideoCodec.AverageBitrate(mb.Value));
            }
            return this;
        }

        /// <summary>
        /// Tỷ lệ giữa bộ đệm và tốc độ bit tối đa khi đặt tốc độ bit tối đa
        /// </summary>
        /// <param name="ratio"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public VideoArgumentsGenerator BufferRatio(double? ratio)
        {
            if (ratio.HasValue && VideoCodec.Name != VideoCodec.SVTAV1.Name)
            {
                if (maxBitrate == null)
                {
                    throw new FFmpegArgumentException("Nên đặt tốc độ bit tối đa trước, sau đó đặt tỷ lệ bộ đệm");
                }
                arguments.Add(VideoCodec.BufferSize(ratio.Value * maxBitrate.Value));
            }
            return this;
        }

        /// <summary>
        /// Video encoding
        /// </summary>
        /// <param name="codec"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator Codec(string codec)
        {
            codec = codec.ToLower();
            foreach (var c in VideoCodec.VideoCodecs)
            {
                if (c.Name.ToLower() == codec || c.Lib.ToLower() == codec)
                {
                    VideoCodec = c;
                    arguments.Add(new FFmpegArgumentItem("c:v", c.Lib));
                    return this;
                }
            }
            VideoCodec = new GeneralVideoCodec();
            if (codec is not ("Tự động" or "auto") && !string.IsNullOrEmpty(codec))
            {
                arguments.Add(new FFmpegArgumentItem("c:v", codec));
            }
            return this;
        }

        /// <summary>
        /// Sao chép video codec
        /// </summary>
        /// <returns></returns>
        public VideoArgumentsGenerator Copy()
        {
            arguments.Add(new FFmpegArgumentItem("c:v", "copy"));
            return this;
        }

        /// <summary>
        /// Hệ số CRF (chất lượng hình ảnh không đổi)
        /// </summary>
        /// <param name="crf"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator CRF(int? crf)
        {
            if (crf.HasValue)
            {
                arguments.Add(VideoCodec.CRF(crf.Value));
            }
            return this;
        }

        /// <summary>
        /// Tắt luồng video
        /// </summary>
        /// <returns></returns>
        public VideoArgumentsGenerator Disable()
        {
            arguments.Add(new FFmpegArgumentItem("vn"));
            return this;
        }

        /// <summary>
        /// Bitrate tối đa
        /// </summary>
        /// <param name="mb"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator MaxBitrate(double? mb)
        {
            if (mb.HasValue)
            {
                maxBitrate = mb;
                arguments.Add(VideoCodec.MaxBitrate(mb.Value));
            }
            return this;
        }

        /// <summary>
        /// Số lần mã hóa (mã hóa hai lần)
        /// </summary>
        /// <param name="pass"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator Pass(int? pass)
        {
            if (!pass.HasValue || pass.Equals(0))
            {
                return this;
            }
            arguments.Add(VideoCodec.Pass(pass.Value));
            return this;
        }

        /// <summary>
        /// Định dạng màu (RGB,CMYK,...)
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator PixelFormat(string format)
        {
            if (!string.IsNullOrEmpty(format))
            {
                arguments.Add(VideoCodec.PixelFormat(format));
                return this;
            }
            return this;
        }

        /// <summary>
        /// Kích thước màn hình
        /// </summary>
        /// <param name="scale"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator Scale(string scale)
        {
            if (!string.IsNullOrEmpty(scale))
            {
                arguments.Add(new FFmpegArgumentItem("scale", scale, "vf", ','));
            }
            return this;
        }

        /// <summary>
        /// Các thông số tốc độ mã hóa/chất lượng
        /// </summary>
        /// <param name="speed"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator Speed(int? speed)
        {
            if (speed.HasValue)
            {
                arguments.Add(VideoCodec.Speed(speed.Value));
            }
            return this;
        }

        /// <summary>
        /// Khung hình
        /// </summary>
        /// <param name="fps"></param>
        /// <returns></returns>
        public VideoArgumentsGenerator FrameRate(double? fps)
        {
            if (fps.HasValue)
            {
                arguments.Add(VideoCodec.FrameRate(fps.Value));
            }
            return this;
        }

        /// <summary>
        /// Các thông số bổ sung dành cho một số định dạng mã hóa video
        /// </summary>
        /// <returns></returns>
        public override IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            return VideoCodec == null ? Enumerable.Empty<FFmpegArgumentItem>() : VideoCodec.ExtraArguments();
        }
    }
}
