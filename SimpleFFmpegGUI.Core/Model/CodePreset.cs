using FzLib;
using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public class CodePreset : ModelBase, INotifyPropertyChanged
    {
        private bool @default;
        private OutputArguments arguments;
        private string name;
        private TaskType type;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Thông số đầu ra
        /// </summary>
        public OutputArguments Arguments
        {
            get => arguments;
            set => this.SetValueAndNotify(ref arguments, value, nameof(Arguments));
        }

        /// <summary>
        /// Cho dù đó là cài đặt trước mặc định trong lớp
        /// </summary>
        public bool Default
        {
            get => @default;
            set => this.SetValueAndNotify(ref @default, value, nameof(Default));
        }

        /// <summary>
        /// Tên đặt trước
        /// </summary>
        public string Name
        {
            get => name;
            set => this.SetValueAndNotify(ref name, value, nameof(Name));
        }

        /// <summary>
        /// Loại tương ứng với cài đặt trước
        /// </summary>
        public TaskType Type
        {
            get => type;
            set => this.SetValueAndNotify(ref type, value, nameof(Type));
        }
    }
}
