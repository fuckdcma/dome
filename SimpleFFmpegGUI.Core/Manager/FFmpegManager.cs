using FFMpegCore;
using FzLib;
using FzLib.IO;
using FzLib.Program;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.FFmpegLib;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Task = System.Threading.Tasks.Task;
using static SimpleFFmpegGUI.FileSystemUtility;
using System.Diagnostics;
using System.Threading.Tasks;
using TaskStatus = SimpleFFmpegGUI.Model.TaskStatus;

namespace SimpleFFmpegGUI.Manager
{
    /// <summary>
    /// Quản lý nhiệm vụ FFmpeg đơn lẻ
    /// </summary>
    public class FFmpegManager : INotifyPropertyChanged
    {
        /// <summary>
        /// Dấu hiệu nhận dạng lỗi trong biểu thức chính quy
        /// </summary>
        private static readonly Regex[] ErrorMessageRegexs = new[]
        {
            new Regex("Error.*",RegexOptions.Compiled),
            new Regex(@"\[.*\] *Unable.*",RegexOptions.Compiled),
            new Regex(@".*Invalid.*",RegexOptions.Compiled|RegexOptions.IgnoreCase),
            new Regex(@"Could find no file.*",RegexOptions.Compiled|RegexOptions.IgnoreCase),
            new Regex(@".* error",RegexOptions.Compiled|RegexOptions.IgnoreCase),
        };

        /// <summary>
        /// Biểu thức chính quy để nhận dạng PSNR
        /// </summary>
        private static readonly Regex rPSNR = new Regex(@"PSNR (([yuvaverageminmax]+:[0-9\. ]+)+)", RegexOptions.Compiled);

        /// <summary>
        /// Biểu thức chính quy để nhận dạng SSIM
        /// </summary>
        private static readonly Regex rSSIM = new Regex(@"SSIM ([YUVAll]+:[0-9\.\(\) ]+)+", RegexOptions.Compiled);

        /// <summary>
        /// Biểu thức chính quy để nhận dạng VMAF
        /// </summary>
        private static readonly Regex rVMAF = new Regex(@"VMAF score: [0-9\.]+", RegexOptions.Compiled);


        /// <summary>
        /// Task hiện tại
        /// </summary>
        private readonly TaskInfo task;

        /// <summary>
        /// Token nguồn để hủy task
        /// </summary>
        private CancellationTokenSource cancel;

        /// <summary>
        /// Trạng thái nhiệm vụ đã bắt đầu chạy hoặc đã hoàn thành
        /// </summary>
        private bool hasRun = false;

        /// <summary>
        /// Kết quả đầu ra cuối cùng
        /// </summary>
        private string lastOutput;

        /// <summary>
        /// Log
        /// </summary>
        private Logger logger = new Logger();

        /// <summary>
        /// Trạng thái nhiệm vụ đã tạm dừng hay chưa
        /// </summary>
        private bool paused;

        /// <summary>
        /// Khi tạm dừng, thời gian bắt đầu tạm dừng
        /// </summary>
        private DateTime pauseStartTime;
        private FFmpegProcess process;

        public FFmpegManager(TaskInfo task)
        {
            this.task = task;
        }

        /// <summary>
        /// Sự kiện đầu ra quy trình
        /// </summary>
        public event EventHandler<FFmpegOutputEventArgs> FFmpegOutput;

        public event EventHandler<ProcessChangedEventArgs> ProcessChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sự kiện thay đổi trạng thái task
        /// </summary>
        public event EventHandler StatusChanged;

        /// <summary>
        /// Sử dụng để giữ lại tiến trình FFmpeg tương ứng với phiên bản sau khi nhiệm vụ kết thúc
        /// </summary>
        public FFmpegProcess LastProcess { get; private set; }

        /// <summary>
        /// Trạng thái nhiệm vụ có đang tạm dừng hay không?
        /// </summary>
        public bool Paused
        {
            get => paused;
            set => this.SetValueAndNotify(ref paused, value, nameof(Paused));
        }

        /// <summary>
        /// Tiến trình FFmpeg
        /// </summary>
        public FFmpegProcess Process
        {
            get => process;
            set
            {
                var old = process;
                process = value;
                ProcessChanged?.Invoke(this, new ProcessChangedEventArgs(old, value));
            }
        }
        /// <summary>
        /// Các thuộc tính liên quan đến tiến độ
        /// </summary>
        public ProgressDto Progress { get; private set; }

        /// <summary>
        /// FFmpeg Task
        /// </summary>
        public TaskInfo Task => task;
        /// <summary>
        /// Kiểm tra tính hợp lệ của tham số đầu ra
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        public static string TestOutputArguments(OutputArguments arguments)
        {
            return ArgumentsGenerator.GetOutputArguments(arguments, arguments.Video?.TwoPass == true ? 2 : 0);
        }

        /// <summary>
        /// Hủy task
        /// </summary>
        public void Cancel()
        {
            logger.Info(task, "Hủy Task hiện tại");
            task.Status = TaskStatus.Cancel;
            cancel.Cancel();
        }

        /// <summary>
        /// Hủy task
        /// </summary>
        public async Task CancelAsync()
        {
            logger.Info(task, "Hủy nhiệm vụ hiện tại");
            task.Status = TaskStatus.Cancel;
            cancel.Cancel();
            try
            {
                await Process.WaitForExitAsync();
            }
            catch (TaskCanceledException)
            {

            }
        }

        /// <summary>
        /// Nhận thông tin lỗi
        /// </summary>
        /// <returns></returns>
        public string GetErrorMessage()
        {
            var logs = LogManager.GetLogs('O', Task.Id, DateTime.Now.AddSeconds(-5));
            var log = logs.List
                .Where(p => ErrorMessageRegexs.Any(q => q.IsMatch(p.Message)))
                .OrderByDescending(p => p.Time).FirstOrDefault();
            return log?.Message;
        }
        /// <summary>
        /// Nhận trạng thái hiện tại
        /// </summary>
        /// <returns></returns>
        public StatusDto GetStatus()
        {
            if (Process == null)
            {
                return new StatusDto(task);
            }
            return new StatusDto(task, Progress, lastOutput, paused);
        }

        /// <summary>
        /// Tiếp tục sau khi tạm dừng
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        public void Resume()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Chức năng tạm dừng và tiếp tục chỉ được hỗ trợ trên Windows");
            }
            if (Process == null)
            {
                throw new Exception("Tiến trình chưa được khởi động, không thể tạm dừng hoặc tiếp tục");
            }
            Paused = false;
            Progress.PauseTime += DateTime.Now - pauseStartTime;
            logger.Info(task, "Tiếp tục hàng đợ");
            ProcessExtension.ResumeProcess(Process.Id);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Thực hiện task
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        /// <exception cref="NotSupportedException"></exception>
        public async Task RunAsync()
        {
            if (hasRun)
            {
                throw new Exception("Một phiên chỉ có thể chạy một lần");
            }
            hasRun = true;
            cancel = new CancellationTokenSource();
            try
            {
                logger.Info(task, "Bắt đầu");
                await (task.Type switch
                {
                    TaskType.Code => RunCodeProcessAsync(cancel.Token),
                    TaskType.Combine => RunCombineProcessAsync(cancel.Token),
                    TaskType.Compare => RunCompareProcessAsync(cancel.Token),
                    TaskType.Custom => RunCustomProcessAsync(cancel.Token),
                    TaskType.Concat => RunConcatProcessAsync(cancel.Token),
                    _ => throw new NotSupportedException("Loại Task không được hỗ trợ：" + task.Type),
                });


                if (task.RealOutput != null && File.Exists(task.RealOutput) && task.Arguments.SyncModifiedTime)
                {
                    try
                    {
                        File.SetLastWriteTime(task.RealOutput, File.GetLastWriteTime(task.Inputs[^1].FilePath));
                    }
                    catch (Exception ex)
                    {
                        logger.Error(task, "Thay đổi thời gian sửa đổi của tệp đầu ra thất bại：" + ex.Message);
                    }
                }

                logger.Info(task, "Task Done!");
            }
            finally
            {
                Progress = null;
                logger.Dispose();
            }
        }

        /// <summary>
        /// Tạm dừng task
        /// </summary>
        /// <exception cref="PlatformNotSupportedException"></exception>
        /// <exception cref="Exception"></exception>
        public void Suspend()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("Chức năng tạm dừng và tiếp tục chỉ được hỗ trợ trên Windows");
            }
            if (Process == null)
            {
                throw new Exception("Tiến trình chưa được khởi động hoặc Task này không cho phép tạm dừng");
            }
            Paused = true;
            logger.Info(task, "Tạm dừng hàng đợi");
            pauseStartTime = DateTime.Now;
            ProcessExtension.SuspendProcess(Process.Id);
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Get độ dài của video
        /// </summary>
        /// <param name="arg"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private static TimeSpan? GetVideoDuration(InputArguments arg)
        {
            var path = arg.FilePath;
            TimeSpan realLength;
            try
            {
                realLength = MediaInfoManager.GetVideoDurationByFFprobe(path);
            }
            catch (Exception)
            {
                return null;
            }
            if (arg == null || arg.From == null && arg.To == null && arg.Duration == null)
            {
                return realLength;
            }

            TimeSpan start = TimeSpan.Zero;
            if (arg.From.HasValue)
            {
                start = arg.From.Value;
            }
            if (arg.To == null && arg.Duration == null)
            {
                if (realLength <= arg.From.Value)
                {
                    throw new FFmpegArgumentException("Thời gian bắt đầu sau thời gian kết thúc của video");
                }
                return realLength - arg.From.Value;
            }
            else if (arg.Duration.HasValue)
            {
                TimeSpan endTime = (arg.From.HasValue ? arg.From.Value : TimeSpan.Zero) + arg.Duration.Value;
                if (endTime > realLength)
                {
                    throw new FFmpegArgumentException("Thời gian bắt đầu sau thời gian kết thúc của video có nghĩa là thời gian bắt đầu sau khi video đã kết thúc.");
                }
                return arg.Duration.Value;
            }
            else if (arg.To.HasValue)
            {
                if (arg.To.Value > realLength)
                {
                    throw new FFmpegArgumentException("Thời gian bắt đầu sau thời gian kết thúc của video có nghĩa là thời gian bắt đầu sau khi video đã kết thúc.");
                }
                return arg.To.Value - start;
            }

            throw new Exception("Tình huống không xác định");
        }

        /// <summary>
        /// lấy thông tin tiến độ
        /// </summary>
        /// <param name="onlyCalcFirstVideoDuration"></param>
        /// <returns></returns>
        private ProgressDto GetProgress(bool onlyCalcFirstVideoDuration = false)
        {
            var p = new ProgressDto();
            if (task.Inputs.Count == 1 || onlyCalcFirstVideoDuration)
            {
                p.VideoLength = GetVideoDuration(task.Inputs[0]);
            }
            else
            {
                var durations = task.Inputs.Select(p => GetVideoDuration(p));
                p.VideoLength = durations.All(p => p.HasValue) ? TimeSpan.FromTicks(durations.Select(p => p.Value.Ticks).Sum()) : null;
            }
            p.StartTime = DateTime.Now;
            return p;
        }

        /// <summary>
        /// Đầu ra quá trình của FFmpeg
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Output(object sender, FFmpegOutputEventArgs e)
        {
            lastOutput = e.Data;
            logger.Output(task, e.Data);
            FFmpegOutput?.Invoke(this, new FFmpegOutputEventArgs(e.Data));
            StatusChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Thực hiện task
        /// </summary>
        /// <param name="arguments"></param>
        /// <param name="desc"></param>
        /// <param name="cancellationToken"></param>
        /// <param name="workingDir"></param>
        /// <returns></returns>
        private async Task RunAsync(string arguments, string desc, CancellationToken cancellationToken, string workingDir = null)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                logger.Info(task, "Yêu cầu hủy trước khi khởi động quá trình");
                return;
            }
            logger.Info(task, "Tham số của FFmpeg：" + arguments);
            task.FFmpegArguments = string.IsNullOrEmpty(task.FFmpegArguments) ? arguments : task.FFmpegArguments + ";" + arguments;
            if (Progress != null)
            {
                Progress.Name = desc;
            }

            Process = new FFmpegProcess(arguments);
            Process.Output += Output;
            try
            {
                await Process.StartAsync(workingDir, cancellationToken);
            }
            finally
            {
                LastProcess = Process;
                Process = null;
            }
        }

        /// <summary>
        /// Thực hiện nhiệm vụ mã hóa
        /// </summary>
        /// <param name="tempDir"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task RunCodeProcessAsync(CancellationToken cancellationToken)
        {
            if (task.Inputs.Count != 1)
            {
                throw new ArgumentException("Mã hóa thông thường, tệp đầu vào phải là 1");
            }
            //Xử lý tên chuỗi hình ảnh
            if (task.Inputs.Count == 1 && task.Inputs[0].Image2)
            {
                string seq = GetSequence(task.Inputs[0].FilePath);
                if (seq != null)
                {
                    task.Inputs[0].FilePath = seq;
                }
            }
            GenerateOutputPath(task);
            string message;
            if (task.Arguments.Video == null || !task.Arguments.Video.TwoPass)
            {
                Progress = GetProgress();
                message = $"Chuyển mã：{Path.GetFileName(task.Inputs[0].FilePath)}";
                string arg = ArgumentsGenerator.GetArguments(task, 0);
                await RunAsync(arg, message, cancellationToken);
            }
            else
            {
                string tempDirectory = GetTempDir("2pass");
                Directory.CreateDirectory(tempDirectory);

                VideoArgumentsGenerator vag = new VideoArgumentsGenerator();
                vag.Codec(task.Arguments.Video.Code);

                Progress = GetProgress();
                Progress.VideoLength *= 2;

                message = $"Chuyển mã（Pass=1）：{Path.GetFileName(task.Inputs[0].FilePath)}";
                string arg = ArgumentsGenerator.GetArguments(task, 1, RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "NUL" : "/dev/null");
                await RunAsync(arg, message, cancellationToken, tempDirectory);

                Progress.BasePercent = 0.5;
                message = $"Chuyển mã（Pass=2）：{Path.GetFileName(task.Inputs[0].FilePath)}";
                arg = ArgumentsGenerator.GetArguments(task, 2);
                await RunAsync(arg, message, cancellationToken, tempDirectory);
            }

        }

        /// <summary>
        /// Thực hiện tác vụ hợp nhất âm thanh-video
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        private async Task RunCombineProcessAsync(CancellationToken cancellationToken)
        {
            if (task.Inputs.Count != 2)
            {
                throw new ArgumentException("Để kết hợp các thao tác âm thanh và video, tệp đầu vào phải là hai");
            }
            var video = FFProbe.Analyse(task.Inputs[0].FilePath);
            var audio = FFProbe.Analyse(task.Inputs[1].FilePath);
            if (video.VideoStreams.Count == 0)
            {
                throw new ArgumentException("Stream 1 không chứa video");
            }
            if (audio.AudioStreams.Count == 0)
            {
                throw new ArgumentException("Stream 2 không chứa âm thanh");
            }

            Progress = GetProgress(true);
            GenerateOutputPath(task);

            var outputArgs = ArgumentsGenerator.GetOutputArguments(v => v.Copy(), a => a.Copy(),
                s => (video.AudioStreams.Count != 0 || audio.VideoStreams.Count != 0) ? s.Map(0, StreamChannel.Video, 0).Map(0, StreamChannel.Audio, 0) : s);
            string arg = ArgumentsGenerator.GetArguments(task.Inputs, outputArgs, task.RealOutput);

            await RunAsync(arg, "Hợp nhất âm thanh và video", cancellationToken);
        }

        /// <summary>
        /// Thực hiện các tác vụ so sánh video
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="FFmpegArgumentException"></exception>
        /// <exception cref="Exception"></exception>
        private async Task RunCompareProcessAsync(CancellationToken cancellationToken)
        {
            if (task.Inputs.Count != 2)
            {
                throw new FFmpegArgumentException("Để so sánh video, Pải có 2 stream");
            }
            var v1 = FFProbe.Analyse(task.Inputs[0].FilePath);
            var v2 = FFProbe.Analyse(task.Inputs[1].FilePath);
            if (v1.VideoStreams.Count == 0)
            {
                throw new FFmpegArgumentException("Stream 1 không chứa video");
            }
            if (v2.VideoStreams.Count == 0)
            {
                throw new FFmpegArgumentException("Stream 2 không chứa video");
            }
            Progress = GetProgress(true);
            string argument = "-lavfi \"ssim;[0:v][1:v]psnr\" -f null -";
            string vmafModel = Directory.EnumerateFiles(App.ProgramDirectoryPath, "vmaf*.json").FirstOrDefault();
            if (vmafModel != null)
            {
                vmafModel = Path.GetFileName(vmafModel);
                argument = @$"-lavfi ""ssim;[0:v][1:v]psnr;[0:v]setpts=PTS-STARTPTS[reference]; [1:v]setpts=PTS-STARTPTS[distorted]; [distorted][reference]libvmaf=model=version=vmaf_v0.6.1:n_threads={Environment.ProcessorCount}""  -f null -";
            }
            var arg = ArgumentsGenerator.GetArguments(task.Inputs, argument);
            FFmpegOutput += CheckOutput;
            string ssim = null;
            string psnr = null;
            string vmaf = null;
            try
            {
                await RunAsync(arg, $"So sánh {Path.GetFileName(task.Inputs[0].FilePath)} Và {Path.GetFileName(task.Inputs[1].FilePath)}", cancellationToken);
                if (ssim == null || psnr == null)
                {
                    throw new Exception("Video so sánh không thành công, không có kết quả so sánh nào được ghi");
                }
                task.Message = ssim + Environment.NewLine + psnr + (vmaf == null ? "" : (Environment.NewLine + vmaf));
            }
            finally
            {
                FFmpegOutput -= CheckOutput;
            }

            void CheckOutput(object sender, FFmpegOutputEventArgs e)
            {
                if (!e.Data.StartsWith('['))
                {
                    return;
                }
                if (rSSIM.IsMatch(e.Data))
                {
                    var match = rSSIM.Match(e.Data);
                    ssim = match.Value;
                    logger.Info(task, "So sánh kết quả（SSIM）：" + match.Value);
                }
                if (rPSNR.IsMatch(e.Data))
                {
                    var match = rPSNR.Match(e.Data);
                    psnr = match.Value;
                    logger.Info(task, "So sánh kết quả（PSNR）：" + match.Value);
                }
                if (rVMAF.IsMatch(e.Data))
                {
                    var match = rVMAF.Match(e.Data);
                    vmaf = match.Value;
                    logger.Info(task, "So sánh kết quả（VMAF）：" + match.Value);
                }
            }
        }

        /// <summary>
        /// Thực hiện các tác vụ ghép video
        /// </summary>
        /// <param name="tempDir"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="NotSupportedException"></exception>
        private async Task RunConcatProcessAsync(CancellationToken cancellationToken)
        {
            if (task.Inputs.Count < 2)
            {
                throw new ArgumentException("Để ghép video, các tệp đầu vào phải từ 2 trở lên");
            }
            string message = $"Nối：{task.Inputs.Count} tập tin";

            string tempPath = GetTempFileName("concat") + ".txt";
            using (var stream = File.CreateText(tempPath))
            {
                foreach (var file in task.Inputs)
                {
                    stream.WriteLine($"file '{file.FilePath}'");
                }
            }
            Progress = GetProgress();
            GenerateOutputPath(task);
            var input = new InputArguments()
            {
                FilePath = tempPath,
                Format = "concat",
                Extra = "-safe 0"
            };

            string arg = ArgumentsGenerator.GetArguments(new InputArguments[] { input }, "-c copy", task.RealOutput);

            await RunAsync(arg, message, cancellationToken);
        }

        /// <summary>
        /// Thực hiện các tác vụ tùy chỉnh
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task RunCustomProcessAsync(CancellationToken cancellationToken)
        {
            await RunAsync(task.Arguments.Extra, null, cancellationToken);
        }
    }
}
