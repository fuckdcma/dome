using CommandLine;
using CommandLine.Text;
using FzLib.Program.Runtime;
using JKang.IpcServiceFramework.Hosting;
using log4net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config", Watch = true)]

namespace SimpleFFmpegGUI
{
    public class Program
    {
        private const string DefaultPipeName = "ffpipe";

        public static ILog AppLog { get; private set; }

        public static IHostBuilder CreateHostBuilder(string pipeName)
        {
            try
            {
                FFmpegDbContext.Migrate();
            }
            catch (Exception ex)
            {
                AppLog.Error(ex);
                Console.WriteLine("Di chuyển cơ sở dữ liệu không thành công：" + ex);
                Console.WriteLine("Chấm dứt chương trình");
                Console.ReadKey();
                Environment.Exit(-1);
                return null;
            }

            return Host.CreateDefaultBuilder()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton<IPipeService, PipeService>();
                    })
                    .ConfigureIpcHost(builder =>
                    {
                        builder.AddNamedPipeEndpoint<IPipeService>(p =>
                        {
                            p.PipeName = pipeName;
                            p.IncludeFailureDetailsInResponse = true;
                        });
                    })
                    .ConfigureLogging(builder =>
                    {
                        builder.AddConsole();
                        builder.SetMinimumLevel(LogLevel.Information);
                    });
        }

        public static void Main(string[] args)
        {
            InitializeLogs();
#if !DEBUG
            UnhandledExceptionCatcher catcher = new UnhandledExceptionCatcher();
            catcher.RegisterTaskCatcher();
            catcher.RegisterThreadsCatcher();
            catcher.UnhandledExceptionCatched += UnhandledException_UnhandledExceptionCatched;
#endif
            string pipeName = DefaultPipeName;
            Parser.Default.ParseArguments<Options>(args)
                     .WithParsed(o =>
                     {
                         if (o.PipeName != null)
                         {
                             Console.WriteLine($"Tên pipe được đặt thành： {o.PipeName}");
                             pipeName = o.PipeName;
                         }
                         else
                         {
                             Console.WriteLine($"Tên pipe không được đặt và mặc định là： {DefaultPipeName}");
                         }
                         if (o.RegisterStartup)
                         {
                             if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                             {
                                 throw new PlatformNotSupportedException("Khởi động chỉ được hỗ trợ cho Windows");
                             }
                             if (FzLib.Program.Startup.IsRegistryKeyExist() != FzLib.IO.ShortcutStatus.Exist)
                             {
                                 List<string> args = new List<string>();
                                 if (o.WorkingDirectoryHere)
                                 {
                                     args.Add("-d");
                                 }
                                 if (o.PipeName != null)
                                 {
                                     args.Add("-p");
                                     args.Add(o.PipeName);
                                 }
                                 FzLib.Program.Startup.CreateRegistryKey(string.Join(' ', args));
                                 Console.WriteLine("Đã đăng ký để tự khởi động, tham số là" + string.Join(' ', args));
                             }
                             else
                             {
                                 Console.WriteLine("Nó đã là một khởi động tự khởi động, không cần đăng ký");
                             }
                             Console.WriteLine("Nhấn phím bất kỳ để thoát");
                             Console.ReadKey();
                             Environment.Exit(0);
                         }
                         if (o.UnregistereStartup)
                         {
                             if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                             {
                                 throw new PlatformNotSupportedException("Tự động khởi động chỉ hỗ trợ Windows");
                             }
                             if (FzLib.Program.Startup.IsRegistryKeyExist() == FzLib.IO.ShortcutStatus.Exist)
                             {
                                 FzLib.Program.Startup.DeleteRegistryKey();
                                 Console.WriteLine("Khởi động bị hủy");
                             }
                             else
                             {
                                 Console.WriteLine("Khởi động chưa đăng ký, không cần hủy");
                             }
                             Console.WriteLine("Nhấn phím bất kỳ để thoát");
                             Console.ReadKey();
                             Environment.Exit(0);
                         }
                         if (o.WorkingDirectoryHere)
                         {
                             FzLib.Program.App.SetWorkingDirectoryToAppPath();
                             Console.WriteLine("Thư mục làm việc được đặt thành thư mục chương trình：" + FzLib.Program.App.ProgramDirectoryPath);
                         }
                     });
            ConsoleLogger.StartListen();
            CreateHostBuilder(pipeName).Build().Run();
        }

        private static void InitializeLogs()
        {
            //Nhật ký cục bộ
            AppLog = LogManager.GetLogger(typeof(Program));
            AppLog.Info("Chương trình bắt đầu");

            //Nhật ký cơ sở dữ liệu
            Logger.Log += Logger_Log;
            Logger.LogSaveFailed += Logger_LogSaveFailed;
            void Logger_Log(object sender, LogEventArgs e)
            {
                switch (e.Log.Type)
                {
                    case 'E': AppLog.Error(e.Log.Message); break;
                    case 'W': AppLog.Warn(e.Log.Message); break;
                    case 'I': AppLog.Info(e.Log.Message); break;
                }
            }
            void Logger_LogSaveFailed(object sender, ExceptionEventArgs e)
            {
                AppLog.Error(e.Exception.Message, e.Exception);
            }
        }

        private static void UnhandledException_UnhandledExceptionCatched(object sender, FzLib.Program.Runtime.UnhandledExceptionEventArgs e)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"Chương trình ngừng chạy với lỗi không xác định：\r\n {e.Exception}");
            AppLog.Error(e.Exception);
            Console.WriteLine("Nhấn phím bất kỳ để thoát");
            Console.ReadKey();
        }
    }
}
