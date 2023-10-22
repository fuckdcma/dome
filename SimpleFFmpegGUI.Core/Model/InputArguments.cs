using FzLib;
using System;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class InputArguments : INotifyPropertyChanged
    {
        private TimeSpan? duration;

        private string filePath;

        private string format;

        private double? framerate;

        private TimeSpan? from;

        private bool image2;

        private string extra;

        private TimeSpan? to;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Khoảng thời gian
        /// </summary>
        public TimeSpan? Duration
        {
            get => duration;
            set => this.SetValueAndNotify(ref duration, value, nameof(Duration));
        }

        /// <summary>
        /// Các thông số khác
        /// </summary>
        public string Extra
        {
            get => extra;
            set => this.SetValueAndNotify(ref extra, value, nameof(Extra));
        }

        /// <summary>
        /// Nhập đường dẫn đến tệp
        /// </summary>
        public string FilePath
        {
            get => filePath;
            set => this.SetValueAndNotify(ref filePath, value, nameof(FilePath));
        }

        /// <summary>
        /// Định dạng đầu vào
        /// </summary>
        public string Format
        {
            get
            {
                if (!string.IsNullOrEmpty(format) && format != "image2" && image2)
                {
                    throw new Exception("Bạn không thể chỉ định cả đầu vào dưới dạng chuỗi khung và định dạng đầu vào");
                }
                return image2 ? "image2" : format;
            }
            set => this.SetValueAndNotify(ref format, value, nameof(Format));
        }

        /// <summary>
        /// Tốc độ khung hình đầu vào (chủ yếu cho chuỗi hình ảnh)
        /// </summary>
        public double? Framerate
        {
            get => framerate;
            set => this.SetValueAndNotify(ref framerate, value, nameof(Framerate));
        }

        /// <summary>
        /// Thời gian bắt đầu
        /// </summary>
        public TimeSpan? From
        {
            get => from;
            set => this.SetValueAndNotify(ref from, value, nameof(From));
        }

        /// <summary>
        /// Nhập xem đó có phải là chuỗi khung hình ảnh hay không
        /// </summary>
        public bool Image2
        {
            get => image2;
            set
            {
                this.SetValueAndNotify(ref image2, value, nameof(Image2));
                if (value && !Framerate.HasValue)
                {
                    Framerate = 30;
                }
            }
        }

        /// <summary>
        /// Thời gian kết thúc
        /// </summary>
        public TimeSpan? To
        {
            get => to;
            set => this.SetValueAndNotify(ref to, value, nameof(To));
        }
    }
}
