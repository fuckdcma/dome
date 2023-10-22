using FFMpegCore.Arguments;
using FFMpegCore.Enums;
using FzLib.DataAnalysis;
using SimpleFFmpegGUI.FFmpegArgument;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public abstract class VideoCodec : CodecBase
    {
        public static readonly X264 X264 = new X264();
        public static readonly X265 X265 = new X265();
        public static readonly XVP9 XVP9 = new XVP9();
        public static readonly AomAV1 AomAV1 = new AomAV1();
        public static readonly SVTAV1 SVTAV1 = new SVTAV1();
        public static readonly GeneralVideoCodec General = new GeneralVideoCodec();
        public static readonly VideoCodec[] VideoCodecs = new VideoCodec[]
        {
           X264,
           X265,
           XVP9,
           AomAV1,
           SVTAV1
        };
        private static Dictionary<string, VideoCodec> name2codec;

        public static VideoCodec GetCodec(string name)
        {
            name2codec ??= VideoCodecs.ToDictionary(p => p.Name, p => p);
            return name == null ? null : name2codec.GetValueOrDefault(name);
        }


        /// <summary>
        /// CRF mặc định
        /// </summary>
        public abstract int DefaultCRF { get; }

        /// <summary>
        /// Mức độ tốc độ chuẩn mặc định
        /// </summary>
        public abstract int DefaultSpeedLevel { get; }

        /// <summary>
        /// CRF tối đa
        /// </summary>
        public abstract int MaxCRF { get; }

        /// <summary>
        /// Mức độ tốc độ tối đa
        /// </summary>
        public abstract int MaxSpeedLevel { get; }

        /// <summary>
        /// Mối quan hệ giữa tốc độ chuẩn và tốc độ mã hóa
        /// </summary>
        /// <remarks>
        /// Gửi một mảng，Độ dài của nó =<see cref="MaxSpeedLevel"/>+1，số lượng tốc độ chuẩn. Chỉ số tương ứng với giá trị tốc độ chuẩn.
        /// SpeedFPSRelationship[i]Đại diện cho tốc độ chuẩn là i，Giá trị tương đối của tốc độ mã hóa thực tế (khung mỗi giây).
        /// Giá trị tuyệt đối của nó không có ý nghĩa，Nhưng giá trị tương đối giữa các giá trị trong một tập hợp biểu thị sự nhanh chậm tương đối của tốc độ mã hóa của chúng
        /// </remarks>
        public abstract double[] SpeedFPSRelationship { get; }

        /// <summary>
        /// Tham số bổ sung
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            return Enumerable.Empty<FFmpegArgumentItem>();
        }

        public virtual FFmpegArgumentItem AverageBitrate(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("Bitrate trung bình vượt giới hạn");
            }
            return new FFmpegArgumentItem("b:v", $"{mb}M");
        }

        public virtual FFmpegArgumentItem BufferSize(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("Kích thước bộ đệm vượt giới hạn");
            }
            return new FFmpegArgumentItem("bufsize", $"{mb}M");
        }

        public virtual FFmpegArgumentItem CRF(int level)
        {
            if (level < 0 || level > MaxCRF)
            {
                throw new FFmpegArgumentException("Giá trị CRF vượt giới hạn");
            }
            return new FFmpegArgumentItem("crf", level.ToString());
        }

        public virtual FFmpegArgumentItem FrameRate(double fps)
        {
            if (fps < 0)
            {
                throw new FFmpegArgumentException("Tốc độ khung hình vượt giới hạn");
            }
            return new FFmpegArgumentItem("r", fps.ToString());
        }

        public virtual FFmpegArgumentItem MaxBitrate(double mb)
        {
            if (mb < 0)
            {
                throw new FFmpegArgumentException("Bitrate tối đa vượt giới hạn");
            }
            return new FFmpegArgumentItem("maxrate", $"{mb}M");
        }

        public virtual FFmpegArgumentItem PixelFormat(string format)
        {
            if (string.IsNullOrWhiteSpace(format))
            {
                throw new FFmpegArgumentException("Không tìm thấy định dạng pixal");
            }
            return new FFmpegArgumentItem("pix_fmt", format);
        }

        public virtual FFmpegArgumentItem Pass(int pass)
        {
            if (pass is not (1 or 2 or 3))
            {
                throw new FFmpegArgumentException("Tham số Pass vượt giới hạn");
            }
            return new FFmpegArgumentItem("pass", pass.ToString());
        }

        public virtual FFmpegArgumentItem Speed(int speed)
        {
            if (speed > MaxSpeedLevel)
            {
                throw new FFmpegArgumentException("Giá trị tốc độ vượt giới hạn");
            }
            return null;
        }
    }

}
