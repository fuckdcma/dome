using FzLib;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class OutputArguments : INotifyPropertyChanged
    {
        private AudioCodeArguments audio;
        private CombineArguments combine;
        private bool disableAudio;
        private bool disableVideo;
        private string extra;
        private string format;
        private StreamArguments stream;
        private VideoCodeArguments video;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Thông số âm thanh
        /// </summary>
        public AudioCodeArguments Audio
        {
            get => audio;
            set => this.SetValueAndNotify(ref audio, value, nameof(Audio));
        }

        /// <summary>
        /// Thông số hợp nhất âm thanh và video
        /// </summary>
        public CombineArguments Combine
        {
            get => combine;
            set => this.SetValueAndNotify(ref combine, value, nameof(Combine));
        }

        /// <summary>
        /// Âm thanh có bị tắt hay không
        /// </summary>
        public bool DisableAudio
        {
            get => disableAudio;
            set => this.SetValueAndNotify(ref disableAudio, value, nameof(DisableAudio));
        }

        /// <summary>
        /// Có nên tắt video không (hình ảnh)
        /// </summary>
        public bool DisableVideo
        {
            get => disableVideo;
            set => this.SetValueAndNotify(ref disableVideo, value, nameof(DisableVideo));
        }

        /// <summary>
        /// Thông số bổ sung
        /// </summary>
        public string Extra
        {
            get => extra;
            set => this.SetValueAndNotify(ref extra, value, nameof(Extra));
        }

        /// <summary>
        /// Định dạng vùng chứa (hậu tố)
        /// </summary>
        public string Format
        {
            get => format;
            set => this.SetValueAndNotify(ref format, value, nameof(Format));
        }

        /// <summary>
        /// Thông số stream
        /// </summary>
        public StreamArguments Stream
        {
            get => stream;
            set => this.SetValueAndNotify(ref stream, value, nameof(Stream));
        }

        /// <summary>
        /// Thông số video
        /// </summary>
        public VideoCodeArguments Video
        {
            get => video;
            set => this.SetValueAndNotify(ref video, value, nameof(Video));
        }

        /// <summary>
        /// Đặt thời gian sửa đổi của tệp đầu ra thành thời gian sửa đổi của tệp đầu vào cuối cùng
        /// </summary>
        public bool SyncModifiedTime { get; set; }
    }
}
