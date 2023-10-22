using System.ComponentModel;

namespace SimpleFFmpegGUI.Model
{
    public enum TaskStatus
    {
        [Description("Xếp hàng")]
        Queue = 1,

        [Description("Xử lý")]
        Processing = 2,

        [Description("Hoàn thành")]
        Done = 3,

        [Description("Đã xảy ra lỗi")]
        Error = 4,

        [Description("Hủy")]
        Cancel = 5
    }
}
