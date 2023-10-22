using System;

namespace SimpleFFmpegGUI.Dto
{
    /// <summary>
    /// Thông tin tiến độ
    /// </summary>
    public class ProgressDto
    {
        /// <summary>
        /// Tỷ lệ phần trăm cơ bản, khi được sử dụng cho áp suất thứ hai, lần vượt qua thứ hai được thêm vào tỷ lệ phần trăm bằng một giá trị
        /// </summary>
        public double BasePercent { get; set; } = 0;

        /// <summary>
        /// Thời gian đã trôi qua
        /// </summary>
        public TimeSpan Duration { get; set; }

        /// <summary>
        /// Thời gian đã trôi qua, không tính thời gian tạm dừng
        /// </summary>
        public TimeSpan RealDuration { get; set; }

        /// <summary>
        /// Dự kiến thời gian kết thúc
        /// </summary>
        public DateTime FinishTime { get; set; }

        /// <summary>
        /// Có thể không xác định được tiến độ
        /// </summary>
        public bool IsIndeterminate { get; set; }

        /// <summary>
        /// Thời gian còn lại
        /// </summary>
        public TimeSpan LastTime { get; set; }

        /// <summary>
        /// Tên nhiệm vụ
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Thời gian tạm dừng
        /// </summary>
        public TimeSpan PauseTime { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Phần trăm tiến độ
        /// </summary>
        public double Percent { get; set; }

        /// <summary>
        /// Thời gian bắt đầu nhiệm vụ
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// Thời gian video kéo dài
        /// </summary>
        public TimeSpan VideoDuration { get; set; } = TimeSpan.Zero;

        /// <summary>
        /// Độ dài video
        /// </summary>
        public TimeSpan? VideoLength { get; set; }

        /// <summary>
        /// Tỷ lệ nén phần trăm. Trong trường hợp nén hai lần, tốc độ dự kiến được sử dụng trong Pass=1 và Pass=2 là khác nhau, do đó tốc độ mã hóa có sự khác biệt lớn
        /// Cần nhân với các hệ số khác nhau cho hai lần Pass trước và sau để đảm bảo thanh tiến trình chính xác.
        /// </summary>
        /// <remarks>
        /// NewBing thật đáng ghét, đã nói với tôi rằng trong 2Pass, hai lần Preset có thể khác nhau, tôi đã bị lừa.
        /// </remarks>
        public double PercentCompressionFactor { get; set; } = 1;

        /// <summary>
        /// Tiến độ cập nhật
        /// </summary>
        /// <param name="VideoDuration"></param>
        public void Update(TimeSpan VideoDuration)
        {
            if (VideoLength.HasValue)
            {
                //Tỷ lệ phần trăm = Tỷ lệ phần trăm tính toán * Hệ số nén + Tỷ lệ phần trăm cơ sở, là một sự thay đổi tuyến tính.
                Percent = VideoDuration.Ticks * 1.0 / VideoLength.Value.Ticks * PercentCompressionFactor + BasePercent;
                if (Percent >= 1)
                {
                    Percent = 1;
                }
                Duration = DateTime.Now - StartTime;
                RealDuration = Duration - PauseTime;
                var totalTime = Percent == 0 ? TimeSpan.Zero : RealDuration / Percent;
                FinishTime = StartTime + PauseTime + totalTime;
                LastTime = totalTime - RealDuration;
                LastTime = LastTime > TimeSpan.Zero ? LastTime : TimeSpan.Zero;
            }
            else
            {
                IsIndeterminate = true;
            }
        }
    }
}
