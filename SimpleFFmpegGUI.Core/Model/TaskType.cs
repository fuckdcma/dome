namespace SimpleFFmpegGUI.Model
{
    public enum TaskType
    {
        [NameDescription("Chuyển mã", "mã hóa lại video")]
        Code = 0,

        [NameDescription("Hợp nhất AV", "Hợp nhất video và âm thanh thành một tệp")]
        Combine = 1,

        [NameDescription("So sánh video", "So sánh tính nhất quán giữa hai video")]
        Compare = 2,

        [NameDescription("Thông số tùy chỉnh", "Thông số tùy chỉnh đầy đủ")]
        Custom = 3,

        [NameDescription("Ghép video", "Kết nối nhiều video từ đầu đến cuối để tạo một video")]
        Concat = 4,

        //[NameDescription("Frame to video","biến một loạt hình ảnh thành video")]
        //Frame2Video=5,
    }
}
