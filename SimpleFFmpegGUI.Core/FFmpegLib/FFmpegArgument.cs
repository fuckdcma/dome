namespace SimpleFFmpegGUI.FFmpegLib
{
    public class FFmpegArgumentItem
    {
        public FFmpegArgumentItem(string key)
        {
            Key = key;
        }

        public FFmpegArgumentItem(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public FFmpegArgumentItem(string key, string value, string parent,char seprator) : this(key, value)
        {
            Parent = parent;
            Seprator = seprator;
        }

        /// <summary>
        /// Tên đối số，Tức là nội dung sau dấu "-"
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// Giá trị tham số
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Nếu tham số đó là tham số con của một tham số khác, thì thuộc tính đó là khóa của tham số cha
        /// </summary>
        public string Parent { get; set; }

        /// <summary>
        /// Nếu tham số đó là tham số con của một tham số khác, thì thuộc tính đó là bộ phân tách để phân chia các tham số con của tham số cha
        /// </summary>
        public char Seprator { get; }

        /// <summary>
        /// Dùng để nối nhiều tham số
        /// </summary>
        public FFmpegArgumentItem Other { get; set; }
    }

}
