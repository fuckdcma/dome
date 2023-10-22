using CommandLine;

namespace SimpleFFmpegGUI
{
    internal class Options
    {
        [Option('p', Required = false, HelpText = "Đặt tên cho pipe")]
        public string PipeName { get; set; }

        [Option('s', Default = false, Required = false, HelpText = "Đăng ký và bắt đầu")]
        public bool RegisterStartup { get; set; }

        [Option('u', Default = false, Required = false, HelpText = "Hủy khởi động")]
        public bool UnregistereStartup { get; set; }

        [Option('d', Default = false, Required = false, HelpText = "Đặt thư mục làm việc vào thư mục chứa chương trình")]
        public bool WorkingDirectoryHere { get; set; }
    }
}
