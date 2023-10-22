namespace SimpleFFmpegGUI.Model
{
    public class StreamMapInfo
    {
        /// <summary>
        /// Nhập số thứ tự của tệp
        /// </summary>
        public int InputIndex { get; set; }
        /// <summary>
        /// Kênh được chỉ định
        /// </summary>
        public StreamChannel Channel { get; set; }
        /// <summary>
        /// Trong tệp được chỉ định (và kênh), số thứ tự của luồng đã chọn
        /// </summary>
        public int? StreamIndex { get; set; }
    }
}
