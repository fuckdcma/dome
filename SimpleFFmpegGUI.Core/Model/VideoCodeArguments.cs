using FzLib;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class VideoCodeArguments : INotifyPropertyChanged
    {
        private string aspect;

        private double? averageBitrate;

        private string code;

        private int? crf;

        private double? fps;

        private double? maxBitrate;

        private double? maxBitrateBuffer;

        private string pixelFormat;

        private int preset;

        private string size;

        private bool twoPass;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Tỷ lệ hình ảnh
        /// </summary>
        public string AspectRatio
        {
            get => aspect;
            set => this.SetValueAndNotify(ref aspect, value, nameof(AspectRatio));
        }

        /// <summary>
        /// Tốc độ bit trung bình
        /// </summary>
        public double? AverageBitrate
        {
            get => averageBitrate;
            set => this.SetValueAndNotify(ref averageBitrate, value, nameof(AverageBitrate));
        }

        /// <summary>
        /// mã hóa
        /// </summary>
        public string Code
        {
            get => code;
            set => this.SetValueAndNotify(ref code, value, nameof(Code));
        }

        /// <summary>
        /// CRF (Chất lượng mục tiêu video)
        /// </summary>
        public int? Crf
        {
            get => crf;
            set => this.SetValueAndNotify(ref crf, value, nameof(Crf));
        }

        /// <summary>
        /// Tốc độ khung hình
        /// </summary>
        public double? Fps
        {
            get => fps;
            set => this.SetValueAndNotify(ref fps, value, nameof(Fps));
        }

        /// <summary>
        /// Tốc độ bit tối đa
        /// </summary>
        public double? MaxBitrate
        {
            get => maxBitrate;
            set => this.SetValueAndNotify(ref maxBitrate, value, nameof(MaxBitrate));
        }

        /// <summary>
        /// Hệ số đệm tốc độ bit tối đa
        /// </summary>
        public double? MaxBitrateBuffer
        {
            get => maxBitrateBuffer;
            set => this.SetValueAndNotify(ref maxBitrateBuffer, value, nameof(MaxBitrateBuffer));
        }

        /// <summary>
        /// Định dạng pixel
        /// </summary>
        public string PixelFormat
        {
            get => pixelFormat;
            set => this.SetValueAndNotify(ref pixelFormat, value, nameof(PixelFormat));
        }

        /// <summary>
        /// Mã hóa tốc độ hoặc cài đặt trước tốc độ
        /// </summary>
        public int Preset
        {
            get => preset;
            set => this.SetValueAndNotify(ref preset, value, nameof(Preset));
        } 

        /// <summary>
        /// Kích thước video (độ phân giải)
        /// </summary>
        public string Size
        {
            get => size;
            set => this.SetValueAndNotify(ref size, value, nameof(Size));
        }

        /// <summary>
        /// Mã hóa bậc hai có hay không
        /// </summary>
        public bool TwoPass
        {
            get => twoPass;
            set => this.SetValueAndNotify(ref twoPass, value, nameof(TwoPass));
        }
    }
}
