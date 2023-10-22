using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Manager
{
    /// <summary>
    /// Tiến trình FFmpeg
    /// </summary>
    public class FFmpegProcess
    {
        private readonly Process process = new Process();

        private ProcessPriorityClass priority = default;

        private bool started = false;

        private TaskCompletionSource<bool> tcs;

        private FFmpegProcess()
        {
            Priority = ConfigManager.DefaultProcessPriority;
        }
        public FFmpegProcess(string argument) : this()
        {
            process.StartInfo = new ProcessStartInfo()
            {
                FileName = "ffmpeg",
                Arguments = argument,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                StandardErrorEncoding = System.Text.Encoding.UTF8,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += Process_OutputDataReceived;
            process.ErrorDataReceived += Process_OutputDataReceived;
        }

        public event EventHandler<FFmpegOutputEventArgs> Output;

        /// <summary>
        /// CPU đã dùng
        /// </summary>
        public double CpuUsage => process.HasExited ? process.TotalProcessorTime.TotalSeconds / Environment.ProcessorCount / RunningTime.TotalSeconds : throw new Exception("进程还未结束");

        /// <summary>
        /// ID Tiến trình
        /// </summary>
            public int Id => !started ? throw new Exception("Tiến trình chưa bắt đầu chạy") : process.Id;

        public int Priority
        {
            get
            {
                if (started && process.HasExited)
                {
                    throw new Exception("Tiến trình này đã EXIT!");
                }

                return priority switch
                {
                    ProcessPriorityClass.RealTime => 5,
                    ProcessPriorityClass.High => 4,
                    ProcessPriorityClass.AboveNormal => 3,
                    ProcessPriorityClass.Normal => 2,
                    ProcessPriorityClass.BelowNormal => 1,
                    ProcessPriorityClass.Idle => 0,
                    _ => throw new InvalidEnumArgumentException()
                };
            }
            set
            {
                if (started && process.HasExited)
                {
                    throw new Exception("Tiến trình này đã EXIT!");
                }
                priority = value switch
                {
                    5 => ProcessPriorityClass.RealTime,
                    4 => ProcessPriorityClass.High,
                    3 => ProcessPriorityClass.AboveNormal,
                    2 => ProcessPriorityClass.Normal,
                    1 => ProcessPriorityClass.BelowNormal,
                    0 => ProcessPriorityClass.Idle,
                    _ => ProcessPriorityClass.Normal,
                };
                if (started)
                {
                    process.PriorityClass = priority;
                }
            }
        }
        /// <summary>
        /// Thời gian trôi qua
        /// </summary>
        public TimeSpan RunningTime => process.HasExited ? process.ExitTime - process.StartTime : throw new Exception("Tiến trình vẫn chưa kết thúc");
        /// <summary>
        /// Bắt đầu tiến trình
        /// </summary>
        /// <param name="workingDir"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Task StartAsync(string workingDir, CancellationToken? cancellationToken)
        {
            if (started)
            {
                throw new Exception("Nó đã bắt đầu chạy, không thể chạy lại");
            }
            started = true;

            if (!string.IsNullOrEmpty(workingDir))
            {
                //2Pass tạo các tệp tạm thời có cùng tên tệp và xung đột nếu nhiều FFmpegs được chạy cùng nhau, do đó cần đặt một thư mục làm việc riêng biệt
                process.StartInfo.WorkingDirectory = workingDir;
            }
            tcs = new TaskCompletionSource<bool>();
            bool exit = false;
            cancellationToken?.Register(() =>
            {
                if (!exit)
                {
                    exit = true;
                    process.Kill();
                }
            });
            process.Start();
            process.PriorityClass = priority;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.Exited += async (s, e) =>
             {
                 exit = true;
                 try
                 {
                     await Task.Delay(1000);
                     if (process.ExitCode == 0)
                     {
                         tcs.SetResult(true);
                     }
                     else if (exit)
                     {
                         tcs.SetException(new TaskCanceledException("Tiến trình bị hủy!"));
                     }
                     else
                     {
                         tcs.SetException(new Exception($"Thoát tiến trình：" + process.ExitCode));
                     }
                     await Task.Delay(10000);
                     process.Dispose();
                 }
                 catch (Exception ex)
                 {
                     tcs.SetException(new Exception($"Đã xảy ra lỗi với trình xử lý quy trình：" + ex.Message, ex));
                 }
             };
            return tcs.Task;
        }

        public Task WaitForExitAsync()
        {
            if (tcs == null)
            {
                throw new Exception("Tiến trình vẫn chưa bắt đầu");
            }
            return tcs.Task;
        }

        /// <summary>
        /// Các sự kiện đầu ra của quy trình được nhận
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Process_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }
            Output?.Invoke(this, new FFmpegOutputEventArgs(e.Data));
        }
    }
}
