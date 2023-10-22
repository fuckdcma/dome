using System;

namespace SimpleFFmpegGUI.FFmpegLib
{
    public enum VideoFormatType
    {
        Video,
        Audio,
        Image
    }
    /// <summary>
    /// Định dạng tệp video
    /// </summary>
    public class VideoFormat
    {
        public static readonly VideoFormat[] Formats = new[]
        {
            new VideoFormat("mp4","mp4",main:true),
            new VideoFormat("matroska","mkv",main:true),
            new VideoFormat("mov","mov",main:true),
            new VideoFormat("avi","avi",main:true),
            new VideoFormat("webm","webm",main:true),
            new VideoFormat("mp3","mp3",VideoFormatType.Audio,true),
            new VideoFormat("adts","aac",VideoFormatType.Audio,true),
            new VideoFormat("image2","jpg",VideoFormatType.Image,true),
            new VideoFormat("image2","bmp",VideoFormatType.Image,true),
            new VideoFormat("mpegts","ts"),
            new VideoFormat("ogv","ogv",VideoFormatType.Audio),
            new VideoFormat("ac3","ac3",VideoFormatType.Audio),
            new VideoFormat("wav","wav",VideoFormatType.Audio),
            new VideoFormat("mp2","mp2",VideoFormatType.Audio),
            new VideoFormat("vob","vob"),
            new VideoFormat("av1","av1",main:true),
            new VideoFormat("image2","png",VideoFormatType.Image),
        };

        public VideoFormat()
        {
        }

        public VideoFormat(string name, string extension, VideoFormatType type = VideoFormatType.Video, bool main = false)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Extension = extension ?? throw new ArgumentNullException(nameof(extension));
            Type = type;
            Main = main;
        }

        /// <summary>
        /// Tên vùng chứa
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Phần mở rộng định dạng
        /// </summary>
        public string Extension { get; set; }

        /// <summary>
        /// Loại định dạng
        /// </summary>
        public VideoFormatType Type { get; set; }

        /// <summary>
        /// Chỉ có âm thanh
        /// </summary>
        public bool AudioOnly =>Type==VideoFormatType.Audio;

        /// <summary>
        /// Có phải là hình ảnh không?
        /// </summary>
        public bool ImageOnly=>Type==VideoFormatType.Image;

        /// <summary>
        /// Có phải là định dạng chính không?
        /// </summary>
        public bool Main { get; set; }
    }
}
