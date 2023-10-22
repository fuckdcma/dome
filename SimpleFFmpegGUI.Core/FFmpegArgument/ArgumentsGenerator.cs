using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    /// <summary>
    /// Trình tạo tham số lệnh FFmpeg
    /// </summary>
    public static class ArgumentsGenerator
    {
        /// <summary>
        /// Tạo tham số chuỗi FFmpeg
        /// </summary>
        /// <param name="task">Nhiệm vụ</param>
        /// <param name="pass">Khi mã hóa hai lần, hãy chỉ định lần mã hóa thứ mấy</param>
        /// <param name="output">Đường dẫn đầu ra</param>
        /// <returns></returns>
        public static string GetArguments(TaskInfo task, int pass, string output = null)
        {
            StringBuilder str = new StringBuilder();
            str.Append("-hide_banner ");
            foreach (var input in task.Inputs)
            {
                str.Append(GetInputArguments(input));
                str.Append(' ');
            }
            str.Append(GetOutputArguments(task.Arguments, pass));
            str.Append(" \"");
            str.Append(output ?? task.RealOutput);
            str.Append("\" -y");
            return str.ToString();
        }

        /// <summary>
        /// Tạo tham số chuỗi FFmpeg
        /// </summary>
        /// <param name="inputs">Tệp đầu vào</param>
        /// <param name="outputArguments">Tham số đầu ra</param>
        /// <param name="output">Đường dẫn đầu ra</param>
        /// <returns></returns>
        public static string GetArguments(IEnumerable<InputArguments> inputs, string outputArguments, string output = null)
        {
            StringBuilder str = new StringBuilder();
            foreach (var input in inputs)
            {
                str.Append(GetInputArguments(input));
                str.Append(' ');
            }
            str.Append(outputArguments);
            str.Append(' ');
            str.Append(output == null ? "" : $"\"{output}\"");
            str.Append(" -y");
            return str.ToString();
        }

        /// <summary>
        /// Tạo tham số chuỗi đầu vào
        /// </summary>
        /// <param name="ia">Tệp đầu vào</param>
        /// <returns></returns>
        public static string GetInputArguments(InputArguments ia)
        {
            InputArgumentsGenerator ig = new InputArgumentsGenerator();
            ig.Seek(ia.From);
            ig.To(ia.To);
            ig.Duration(ia.Duration);
            ig.Format(ia.Format);
            ig.Framerate(ia.Framerate);
            ig.Input(ia.FilePath);
            return ia.Extra == null ? ig.GetArguments() : $"{ia.Extra}  {ig.GetArguments()}";
        }

        /// <summary>
        /// Tạo tham số chuỗi phần đầu ra
        /// </summary>
        /// <param name="video">Video</param>
        /// <param name="audio">Audio</param>
        /// <param name="stream">Số luồng</param>
        /// <returns></returns>
        public static string GetOutputArguments(
            Func<VideoArgumentsGenerator, VideoArgumentsGenerator> video,
            Func<AudioArgumentsGenerator, AudioArgumentsGenerator> audio,
            Func<StreamArgumentsGenerator, StreamArgumentsGenerator> stream)
        {
            VideoArgumentsGenerator vg = new VideoArgumentsGenerator();
            AudioArgumentsGenerator ag = new AudioArgumentsGenerator();
            StreamArgumentsGenerator sg = new StreamArgumentsGenerator();
            vg = video(vg);
            ag = audio(ag);
            sg = stream(sg);

            return string.Join(' ', vg.GetArguments(), ag.GetArguments(), sg.GetArguments());
        }

        /// <summary>
        /// Tạo tham số chuỗi phần đầu ra
        /// </summary>
        /// <param name="oa">Tham số đầu ra</param>
        /// <param name="pass">Khi mã hóa lần thứ hai, hãy chỉ định lần mã hóa thứ bao nhiêu</param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        public static string GetOutputArguments(OutputArguments oa, int pass)
        {
            VideoArgumentsGenerator vg = new VideoArgumentsGenerator();
            AudioArgumentsGenerator ag = new AudioArgumentsGenerator();
            StreamArgumentsGenerator sg = new StreamArgumentsGenerator();
            CheckOutputArguments(oa);
            if (oa.DisableVideo)
            {
                vg.Disable();
            }
            else if (oa.Video == null)
            {
                vg.Copy();
            }
            else
            {
                vg.Codec(oa.Video.Code);
                vg.Speed(oa.Video.Preset);
                vg.CRF(oa.Video.Crf);
                vg.AverageBitrate(oa.Video.AverageBitrate);
                vg.MaxBitrate(oa.Video.MaxBitrate);
                if (oa.Video.MaxBitrate != null)
                {
                    vg.BufferRatio(oa.Video.MaxBitrateBuffer);
                }
                vg.Aspect(oa.Video.AspectRatio);
                vg.PixelFormat(oa.Video.PixelFormat);
                vg.FrameRate(oa.Video.Fps);
                vg.Scale(oa.Video.Size);
                vg.Pass(pass);
            }

            if (oa.DisableAudio || pass == 1)
            {
                ag.Disable();
            }
            else if (oa.Audio == null)
            {
                ag.Copy();
            }
            else
            {
                ag.Codec(oa.Audio.Code);
                ag.Bitrate(oa.Audio.Bitrate);
                ag.SamplingRate(oa.Audio.SamplingRate);
            }

            if (oa.Stream != null && oa.Stream.Maps != null && oa.Stream.Maps.Count > 0)
            {
                foreach (var map in oa.Stream.Maps)
                {
                    sg.Map(map.InputIndex, map.Channel, map.StreamIndex);
                }
            }

            string extra = "";
            //Hủy bỏ việc chỉ định format, vì một số định dạng khác nhau có thể có format giống nhau, chỉ định hậu tố cũng có thể đạt được hiệu quả tương tự
            //if (oa.Format != null && pass!=1)
            //{
            //    extra = $"-f {oa.Format}";
            //}
            if (pass == 1)
            {
                extra = $"-f {oa.Format}";
            }
            if (oa.Extra != null)
            {
                extra = $"{extra} {oa.Extra}";
            }

            return string.Join(' ', vg.GetArguments(), ag.GetArguments(), sg.GetArguments(), extra);
        }

        private static void CheckOutputArguments(OutputArguments oa)
        {
            if (oa.DisableVideo && oa.DisableAudio)
            {
                throw new FFmpegArgumentException("Không thể tắt video và âm thanh cùng một lúc");
            }
            if ((oa.Video?.TwoPass ?? false) && string.IsNullOrWhiteSpace(oa.Format))
            {
                throw new FFmpegArgumentException("Khi cần mã hóa hai lần, bạn phải chỉ định định dạng（Format）");
            }
        }
    }
}
