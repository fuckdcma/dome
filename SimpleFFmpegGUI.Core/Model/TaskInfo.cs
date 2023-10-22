using System;
using System.Collections.Generic;

namespace SimpleFFmpegGUI.Model
{
    /// <summary>
    /// Nhiệm vụ FFmpeg
    /// </summary>
    public class TaskInfo : ModelBase
    {
        public TaskInfo()
        {
            CreateTime = DateTime.Now;
            Status = TaskStatus.Queue;
        }

        /// <summary>
        /// Loại nhiệm vụ
        /// </summary>
        public TaskType Type { get; set; }

        /// <summary>
        /// Trạng thái nhiệm vụ hiện tại
        /// </summary>
        public TaskStatus Status { get; set; }

        /// <summary>
        /// Nhập tệp và tham số
        /// </summary>
        public List<InputArguments> Inputs { get; set; }

        /// <summary>
        /// Đường dẫn đầu ra được chỉ định
        /// </summary>
        public string Output { get; set; }

        /// <summary>
        /// Đường dẫn đầu ra thực tế
        /// </summary>
        public string RealOutput { get; set; }

        /// <summary>
        /// Thông số đầu ra
        /// </summary>
        public OutputArguments Arguments { get; set; }

        /// <summary>
        /// Thời gian tạo tác vụ
        /// </summary>
        public DateTime CreateTime { get; set; }

        /// <summary>
        /// Thời gian bắt đầu
        /// </summary>
        public DateTime? StartTime { get; set; }

        /// <summary>
        /// Thời gian kết thúc
        /// </summary>
        public DateTime? FinishTime { get; set; }

        /// <summary>
        /// Thông tin liên quan, bao gồm thông báo lỗi, kết quả thực thi, v.v
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Thông số khớp lệnh của FFmpeg tại thời điểm khớp lệnh
        /// </summary>
        public string FFmpegArguments { get; set; }
    }
}
