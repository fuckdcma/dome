using FFMpegCore;
using Mapster;
using Newtonsoft.Json.Linq;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.Model.MediaInfo;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Manager
{
    public static class MediaInfoManager
    {
        public static async Task<MediaInfoGeneral> GetMediaInfoAsync(string path)
        {
            MediaInfoGeneral mediaInfo = null;
            await Task.Run(() =>
            {
                var mediaInfoJSON = GetMediaInfoProcessOutput(path);
                mediaInfo = ParseMediaInfoJSON(mediaInfoJSON);
                mediaInfo.Raw = mediaInfoJSON.ToString(Newtonsoft.Json.Formatting.Indented);
                foreach (var video in mediaInfo.Videos)
                {
                    if (!string.IsNullOrEmpty(video.Encoded_Library_Settings))
                    {
                        video.EncodingSettings = ParseEncodingSettings(video.Encoded_Library_Settings);
                    }
                }

            });
            return mediaInfo;
        }

        public static async Task<string> GetSnapshotAsync(string path, TimeSpan time, string scale,string format="bmp")
        {
            string tempPath = $"{FileSystemUtility.GetTempFileName("snapshot")}.{format}";
            FFmpegProcess process = new FFmpegProcess($"-ss {time.TotalSeconds:0.000}  -i \"{path}\" -vframes 1 -vf scale={scale} {tempPath}");
            await process.StartAsync(null, null);
            return tempPath;
        }

        public static TimeSpan GetVideoDurationByFFprobe(string path)
        {
            return FFProbe.Analyse(path).Duration;
        }

        public static async Task<TimeSpan> GetVideoDurationByFFprobeAsync(string path)
        {
            return (await FFProbe.AnalyseAsync(path)).Duration;
        }

        private static JObject GetMediaInfoProcessOutput(string path)
        {
            string tmpFile = FileSystemUtility.GetTempFileName("mediainfo");
            var p = Process.Start(new ProcessStartInfo
            {
                FileName = "MediaInfo",
                Arguments = $"--output=JSON --BOM --LogFile=\"{tmpFile}\" \"{path}\"",
                CreateNoWindow = true,
            });
            p.WaitForExit();
            string output = System.IO.File.ReadAllText(tmpFile);
            return JObject.Parse(output);
        }

        /// <summary>
        /// Phân tích cú pháp cài đặt mã hóa (được tạo bởi NewBing)
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static List<MediaInfoItem> ParseEncodingSettings(string input)
        {
            List<MediaInfoItem> settings = new List<MediaInfoItem>(); // Tạo danh sách trống để lưu trữ các mục cài đặt mã hóa
            var parts = input.Split('/').Select(p => p.Trim()); // Sử dụng "/" để tách chuỗi đầu vào thành một mảng chuỗi
            foreach (string part in parts) // Lặp qua từng chuỗi trong mảng
            {
                MediaInfoItem setting = new MediaInfoItem(); // Tạo đối tượng mục cài đặt mã hóa mới
                if (part.Contains('=')) // Kiểm tra xem chuỗi có chứa "=" không
                {
                    string[] pair = part.Split('='); // Sử dụng "=" để chia chuỗi thành hai phần
                    setting.Name = pair[0]; // Gán phần đầu tiên cho thuộc tính name của mục cài đặt mã hóa
                    string value = pair[1]; // Lấy phần thứ hai làm chuỗi
                    if (int.TryParse(value, out int intValue)) // Cố gắng phân tích cú pháp giá trị thành một số nguyên
                    {
                        setting.Value = intValue; // Gán giá trị số nguyên cho thuộc tính giá trị của mục cài đặt mã hóa
                    }
                    else if (double.TryParse(value, out double doubleValue)) // Cố gắng phân tích giá trị dưới dạng số dấu phẩy động có độ chính xác kép
                    {
                        setting.Value = doubleValue; // Gán số dấu phẩy động có độ chính xác kép cho thuộc tính giá trị của mục nhập cài đặt mã hóa
                    }
                    else // Nếu giá trị không phải là một số
                    {
                        setting.Value = value; // Gán một giá trị chuỗi cho thuộc tính giá trị của mục cài đặt mã hóa
                    }
                }
                else // Nếu chuỗi không chứa "="
                {
                    setting.Name = part; // Gán toàn bộ chuỗi cho thuộc tính name của mục cài đặt mã hóa
                    setting.Value = true; // Gán đúng cho thuộc tính giá trị của mục cài đặt mã hóa
                }
                settings.Add(setting); // Thêm đối tượng thiết đặt mã hóa vào danh sách
            }
            return settings; // Trả về danh sách các mục thiết đặt mã hóa
        }

        private static MediaInfoGeneral ParseMediaInfoJSON(JObject json)
        {
            MediaInfoGeneral info = null;
            var tracks = json["media"]["track"] as JArray;
            foreach (JObject track in tracks)
            {
                if (track["@type"].Value<string>() == "General")
                {
                    info = track.ToObject<MediaInfoGeneral>();
                }
                else if (track["@type"].Value<string>() == "Video")
                {
                    Debug.Assert(info != null);
                    info.Videos.Add(track.ToObject<MediaInfoVideo>());
                    info.Videos[^1].Index = info.Videos.Count;
                }
                else if (track["@type"].Value<string>() == "Audio")
                {
                    Debug.Assert(info != null);
                    info.Audios.Add(track.ToObject<MediaInfoAudio>());
                    info.Audios[^1].Index = info.Audios.Count;
                }
                else if (track["@type"].Value<string>() == "Text")
                {
                    Debug.Assert(info != null);
                    info.Texts.Add(track.ToObject<MediaInfoText>());
                    info.Texts[^1].Index = info.Texts.Count;
                }
            }
            return info;
        }

        public static VideoCodeArguments ConvertToVideoArguments(MediaInfoGeneral mediaInfo)
        {
            VideoCodeArguments arguments = new VideoCodeArguments();

            var tracks = JObject.Parse(mediaInfo.Raw)["media"]["track"] as JArray;
            if (mediaInfo.Videos.Count == 0)
            {
                throw new Exception("Tệp nguồn không chứa video");
            }
            var video = mediaInfo.Videos[0];

            arguments.Code = video.Format switch
            {
                "AVC" => VideoCodec.X264.Name,
                "HEVC" => VideoCodec.X265.Name,
                _ => throw new Exception("Chỉ hỗ trợ H264 hoặc H265")
            };

            if (video.EncodingSettings != null && video.EncodingSettings.Count > 0)
            {
                var settings = video.EncodingSettings.ToDictionary(p => p.Name, p => p.Value);
                try
                {
                    if (settings["rc"].Equals("crf"))
                    {
                        if (settings.ContainsKey("crf"))
                        {
                            arguments.Crf = Convert.ToInt32(settings["crf"]);
                        }
                    }
                    else if (settings["rc"].Equals("abr"))
                    {
                        arguments.AverageBitrate = Convert.ToDouble(settings["bitrate"]) / 1000;
                        if (Convert.ToDouble(settings["stats-read"]) > 0)
                        {
                            arguments.TwoPass = true;
                        }
                    }
                    if (settings.ContainsKey("vbv-maxrate"))
                    {
                        arguments.MaxBitrate = Convert.ToDouble(settings["vbv-maxrate"]) / 1000;
                        arguments.MaxBitrateBuffer = Convert.ToDouble(settings["vbv-bufsize"]) / 1000 / arguments.MaxBitrate;
                    }
                }
                catch (Exception ex)
                {
                    throw;
                }

                int preset = 0;

                if (arguments.Code == VideoCodec.X264.Name)
                {
                    if (settings["cabac"].Equals(0))
                    {
                        preset = 8;
                    }
                    else if (settings["subme"].Equals(1))
                    {
                        preset = 7;
                    }
                    else if (settings["subme"].Equals(2))
                    {
                        preset = 6;
                    }
                    else if (settings["subme"].Equals(4))
                    {
                        preset = 5;
                    }
                    else if (settings["subme"].Equals(6))
                    {
                        preset = 4;
                    }
                    else if (settings["subme"].Equals(7))
                    {
                        preset = 3;
                    }
                    else if (settings["subme"].Equals(8))
                    {
                        preset = 2;
                    }
                    else if (settings["subme"].Equals(9))
                    {
                        preset = 1;
                    }
                }
                else if (arguments.Code == VideoCodec.X265.Name)
                {
                    if (settings.ContainsKey("no-signhide"))
                    {
                        preset = 8;
                    }
                    else if (settings.ContainsKey("no-sao"))
                    {
                        preset = 7;
                    }
                    else if (settings["subme"].Equals(1))
                    {
                        preset = 6;
                    }
                    else if (settings["ref"].Equals(2))
                    {
                        preset = 5;
                    }
                    else if (settings["max-merge"].Equals(2))
                    {
                        preset = 4;
                    }
                    else if (settings["ref"].Equals(3))
                    {
                        preset = 3;
                    }
                    else if (settings["ref"].Equals(4))
                    {
                        preset = 2;
                    }
                    else if (settings["max-merge"].Equals(4))
                    {
                        preset = 1;
                    }
                }
                arguments.Preset = preset;
            }
            else
            {
                throw new Exception("Video nguồn không cung cấp thông tin cài đặt mã hóa và không thể chuyển đổi thành thông số đầu ra");
            }

            return arguments;
        }
    }
}
