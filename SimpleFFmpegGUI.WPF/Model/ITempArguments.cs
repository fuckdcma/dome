using System.ComponentModel;

namespace SimpleFFmpegGUI.WPF.Model
{
    public interface ITempArguments : INotifyPropertyChanged
    {
        /// <summary>
        /// Cập nhật dữ liệu gốc vào dữ liệu UI
        /// </summary>
        public void Update();

        /// <summary>
        /// Áp dụng từ dữ liệu UI cho dữ liệu thô
        /// </summary>
        public void Apply();
    }
}
