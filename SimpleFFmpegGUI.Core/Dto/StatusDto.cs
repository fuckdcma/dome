using FzLib;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Dto
{
    public class StatusDto : INotifyPropertyChanged
    {
        /// <summary>
        /// Biểu thức chính quy để nhận dạng thông tin tiến trình đầu ra của FFmpeg
        /// </summary>
        private static readonly Regex rFFmpegOutput = new Regex(
            @"frame= *(?<f>[0-9]+) *fps= *(?<fps>[0-9\.]+) *(q= *(?<q>[0-9\.\-]+) *)+size= *(?<size>([0-9\.a-zA-Z]+)|(N/A)) *time= *(?<time>[0-9\.\-:]+) *bitrate= *(?<b>([0-9\.a-z/]+)|(N/A)).*speed= *(?<speed>([0-9\.]+)|(N/A))x?", RegexOptions.Compiled);

        private string bitrate;

        private double fps;

        private int frame;

        private bool hasDetail = false;

        private bool isPaused;

        private bool isProcessing;

        private string lastOutput;

        private ProgressDto progress;

        private double q;

        private string size;

        private string speed;

        private TaskInfo task;

        private TimeSpan time;

        public StatusDto()
        {
        }

        public StatusDto(TaskInfo task)
        {
            Task = task;
        }

        public StatusDto(TaskInfo task, ProgressDto progress, string lastOutput, bool paused)
        {
            Task = task;
            LastOutput = lastOutput;
            IsProcessing = true;
            IsPaused = paused;
            if (lastOutput != null && rFFmpegOutput.IsMatch(lastOutput))
            {
                try
                {
                    var match = rFFmpegOutput.Match(lastOutput);
                    Frame = int.Parse(match.Groups["f"].Value);
                    Fps = double.Parse(match.Groups["fps"].Value);
                    Size = match.Groups["size"].Value.ToUpper();
                    Time = TimeSpan.Parse(match.Groups["time"].Value);
                    if (Time < TimeSpan.Zero)
                    {
                        Time = TimeSpan.Zero;
                    }
                    Bitrate = match.Groups["b"].Value;
                    Speed = match.Groups["speed"].Value;
                    Q = double.Parse(match.Groups["q"].Value);

                    if (progress != null)
                    {
                        if (!IsPaused)
                        {
                            progress.Update(Time);
                        }
                        HasDetail = true;
                    }
                    Progress = progress;
                }
                catch
                {
                }
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Bitrate
        /// </summary>
        public string Bitrate
        {
            get => bitrate;
            set => this.SetValueAndNotify(ref bitrate, value, nameof(Bitrate));
        }

        /// <summary>
        /// Chuyển đổi tốc độ FPS
        /// </summary>
        public double Fps
        {
            get => fps;
            set => this.SetValueAndNotify(ref fps, value, nameof(Fps));
        }

        /// <summary>
        /// FPS hiện tại
        /// </summary>
        public int Frame
        {
            get => frame;
            set => this.SetValueAndNotify(ref frame, value, nameof(Frame));
        }

        /// <summary>
                /// Có thông tin chi tiết hơn về tiến độ không?
        /// </summary>
        public bool HasDetail
        {
            get => hasDetail;
            set => this.SetValueAndNotify(ref hasDetail, value, nameof(HasDetail));
        }

        /// <summary>
        /// Đã tạm dừng chưa?
        /// </summary>
        public bool IsPaused
        {
            get => isPaused;
            set => this.SetValueAndNotify(ref isPaused, value, nameof(IsPaused));
        }

        /// <summary>
        /// Đang thực hiện chưa?
        /// </summary>
        public bool IsProcessing
        {
            get => isProcessing;
            set => this.SetValueAndNotify(ref isProcessing, value, nameof(IsProcessing));
        }

        /// <summary>
        /// xuất chuỗi ký tự
        /// </summary>
        public string LastOutput
        {
            get => lastOutput;
            set => this.SetValueAndNotify(ref lastOutput, value, nameof(LastOutput));
        }

        /// <summary>
        /// thông tin tiến độ
        /// </summary>
        public ProgressDto Progress
        {
            get => progress;
            set => this.SetValueAndNotify(ref progress, value, nameof(Progress));
        }

        /// <summary>
        /// chất lượng
        /// </summary>
        public double Q
        {
            get => q;
            set => this.SetValueAndNotify(ref q, value, nameof(Q));
        }

        /// <summary>
        /// kích thước tệp hiện tại
        /// </summary>
        public string Size
        {
            get => size;
            set => this.SetValueAndNotify(ref size, value, nameof(Size));
        }

        /// <summary>
        /// thông tin về tốc độ xử lý
        /// </summary>
        public string Speed
        {
            get => speed;
            set => this.SetValueAndNotify(ref speed, value, nameof(Speed));
        }

        /// <summary>
        /// nhiệm vụ
        /// </summary>
        public TaskInfo Task
        {
            get => task;
            set => this.SetValueAndNotify(ref task, value, nameof(Task));
        }

        /// <summary>
        /// thời gian trong video hiện tại
        /// </summary>
        public TimeSpan Time
        {
            get => time;
            set => this.SetValueAndNotify(ref time, value, nameof(Time));
        }
      
    }
}
