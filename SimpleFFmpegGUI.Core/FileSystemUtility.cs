using FzLib.IO;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SimpleFFmpegGUI
{
    public static class FileSystemUtility
    {
        private const string TempDirKey = "TempDir";

        /// <summary>
        /// Ký tự không hợp lệ trong tên tập tin.
        /// </summary>
        private static HashSet<char> invalidFileNameChars = Path.GetInvalidFileNameChars().ToHashSet();

        public static string GetSequence(string sampleFilePath)
        {
            string dir = Path.GetDirectoryName(sampleFilePath);
            string filename = Path.GetFileNameWithoutExtension(sampleFilePath);
            string ext = Path.GetExtension(sampleFilePath);
            var filesInDir = Directory.EnumerateFiles(dir, $"*{ext}").ToList();
            for (int i = filename.Length - 1; i >= 0; i--)
            {
                var thisChar = filename[i];
                var rightChar = i == filename.Length - 1 ? '\0' : filename[i + 1];
                if (thisChar is '0' or '1' && rightChar is < '0' or > '9') //Các ký tự hiện tại là 0 hoặc 1, bên phải không phải là số.
                {
                    int indexLength = 1;
                    for (int j = i - 1; j >= 0; j--)
                    {
                        if (filename[j] == '0')
                        {
                            indexLength++;
                        }
                    }
                    string leftPart = filename[..(i - indexLength + 1)];
                    string rightPart = filename[(i + 1)..];
                    int indexFrom = thisChar - '0';
                    string nextFileName = leftPart + (indexFrom + 1).ToString().PadLeft(indexLength, '0') + rightPart;
                    nextFileName = Path.Combine(dir, nextFileName + ext);
                    if (filesInDir.Contains(nextFileName))
                    {
                        if (indexLength == 1)
                        {
                            return Path.Combine(dir, $"{leftPart}%d{rightPart}{ext}");
                        }
                        return Path.Combine(dir, $"{leftPart}%0{indexLength}d{rightPart}{ext}");
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Nhận một tệp tạm thời có tên ngẫu nhiên. Tệp nhận được sẽ có đường dẫn: %TEMP%/<see cref="SimpleFFmpegGUI"/>/{type}/{tên ngẫu nhiên}
        /// </summary>
        /// <returns></returns>
        public static string GetTempFileName(string type)
        {
            return Path.Combine(GetTempDir(type,""), Guid.NewGuid().ToString());
        }

        /// <summary>
        /// Nhận thư mục tạm thời. Thư mục nhận được sẽ có đường dẫn: %TEMP%/<see cref="SimpleFFmpegGUI"/>/{type}/{tên ngẫu nhiên}
        /// </summary>
        /// <returns></returns>
        public static string GetTempDir(string type)
        {
            string str = Guid.NewGuid().ToString();
            return GetTempDir(type, str);
        }

        private static string GetTempDir(string type, string subName)
        {
            string path = Path.Combine(Path.GetTempPath(), nameof(SimpleFFmpegGUI), type, subName);
            Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// Tạo đường dẫn đầu ra.
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static string GenerateOutputPath(TaskInfo task)
        {
            string output = task.Output.Trim();
            var a = task.Arguments;
            if (string.IsNullOrEmpty(output))
            {
                if (task.Inputs.Count == 0)
                {
                    throw new Exception("Chưa chỉ định đường dẫn đầu ra，Và tệp đầu vào trống");
                }
                output = task.Inputs[0].FilePath;
            }

            //Xóa các ký tự không hợp lệ
            string dir = Path.GetDirectoryName(output);
            string filename = Path.GetFileName(output);
            if (filename.Any(p => invalidFileNameChars.Contains(p)))
            {
                foreach (var c in invalidFileNameChars)
                {
                    filename = filename.Replace(c.ToString(), "");
                }
                output = Path.Combine(dir, filename);
            }


            //Thay đổi phần mở rộng
            if (!string.IsNullOrEmpty(a?.Format))
            {
                VideoFormat format = VideoFormat.Formats.Where(p => p.Name == a.Format || p.Extension == a.Format).FirstOrDefault();
                if (format != null)
                {
                    string name = Path.GetFileNameWithoutExtension(output);
                    string extension = format.Extension;
                    output = Path.Combine(dir, name + "." + extension);
                }
            }

            //Nhận tên tệp không trùng lặp
            task.RealOutput = FileSystem.GetNoDuplicateFile(output);

            //Tạo thư mục
            if (!new FileInfo(task.RealOutput).Directory.Exists)
            {
                new FileInfo(task.RealOutput).Directory.Create();
            }
            return task.RealOutput;
        }
    }
}
