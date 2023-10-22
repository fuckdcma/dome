using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.Pages
{
    public static class PageHelper
    {
        public static string GetTitle<T>()
        {
            return typeof(T).Name switch
            {
                nameof(AddTaskPage) => "Thêm nhiệm vụ mới",
                nameof(MediaInfoPage) => "Thông tin truyền thông",
                nameof(PresetsPage) => "Tất cả các cài đặt trước",
                nameof(TasksPage) => "Tất cả nhiệm vụ",
                nameof(LogsPage) => "Nhật ký",
                nameof(SettingPage) => "Cài đặt",
                nameof(FFmpegOutputPage) => "FDòng lệnh đầu ra FMPEG",
                _ => throw new NotImplementedException(),
            };
        }
    }
}
