﻿using Mapster;
using SimpleFFmpegGUI.Dto;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.Manager
{
    /// <summary>
    /// Quản lý nguồn điện máy tính, tắt điều khiển
    /// </summary>
    public class PowerManager
    {
        private static readonly string abortShutdownCommand = "-a";
        private static readonly string shutdownCommand = $"-s -t 180 -c \"{FzLib.Program.App.ProgramName}\"";
        private bool shutdownAfterQueueFinished = false;

        public bool ShutdownAfterQueueFinished
        {
            get { return shutdownAfterQueueFinished; }
            set
            {
                shutdownAfterQueueFinished = value;
                using Logger logger = new Logger();
                logger.Info("Nhận lệnh tự động tắt khi hàng đợi kết thúc：" + value.ToString());
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416: Xác minh tính tương thích của nền tảng", Justification = "<Đình chỉ>")]
        public static async Task<CpuCoreUsageDto[]> GetCpuUsageAsync(TimeSpan sampleSpan=default,Task task=default)
        {
            if(sampleSpan ==default && task==default)
            {
                throw new ArgumentException("Cung cấp ít nhất một thông số");
            }

            PerformanceCounter cpuCounter;

            cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");

            var pc = new PerformanceCounter("Processor Information", "% Processor Time");
            var cat = new PerformanceCounterCategory("Processor Information");

            var usages = new SortedDictionary<int, CpuCoreUsage>();
            var instances = cat.GetInstanceNames();
            foreach (var s in instances)
            {
                pc.InstanceName = s;
                string[] parts = s.Split(",");
                if (parts.Length == 1)
                {
                    usages.Add(-1, new CpuCoreUsage()
                    {
                        CpuIndex = -1,
                        CoreIndex = -1,
                        Sample = pc.NextSample()
                    });
                    continue;
                }
                if (parts[^1] == "_Total")
                {
                    continue;
                }
                CpuCoreUsage cpuCoreUsage = new CpuCoreUsage()
                {
                    CpuIndex = int.Parse(parts[0]),
                    CoreIndex = int.Parse(parts[1]),
                    Sample = pc.NextSample()
                };
                usages.Add(cpuCoreUsage.CpuIndex * 1000 + cpuCoreUsage.CoreIndex, cpuCoreUsage);
            }
            if (sampleSpan != default)
            {
                await Task.Delay(sampleSpan);
            }
            else
            {
                await task;
            }

            foreach (var s in instances)
            {
                pc.InstanceName = s;
                string[] parts = s.Split(",");

                if (parts.Length == 1)
                {
                    usages[-1].Usage = CalculateCpuUsage(usages[-1].Sample, pc.NextSample());
                    continue;
                }
                if (parts[^1] == "_Total")
                {
                    continue;
                }
                int cpuIndex = int.Parse(parts[0]);
                int coreIndex = int.Parse(parts[1]);
                usages[cpuIndex * 1000 + coreIndex].Usage
                    = CalculateCpuUsage(usages[cpuIndex * 1000 + coreIndex].Sample, pc.NextSample());
            }
            return usages.Values.Select(p => p.Adapt<CpuCoreUsageDto>()).ToArray();
        }

        public void AbortShutdown()
        {
            using Logger logger = new Logger();
            logger.Warn("Lệnh dừng tắt máy đã được nhận");
            Shutdown(false);
        }

        public void Shutdown()
        {
            using Logger logger = new Logger();
            logger.Warn("Một lệnh tắt máy đã được nhận");
            Shutdown(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416: Xác minh tính tương thích của nền tảng", Justification = "<Đình chỉ>")]
        private static double CalculateCpuUsage(CounterSample oldSample, CounterSample newSample)
        {
            double difference = newSample.RawValue - oldSample.RawValue;
            double timeInterval = newSample.TimeStamp100nSec - oldSample.TimeStamp100nSec;
            if (timeInterval != 0)
            {
                var value = 1 - difference / timeInterval;
                return value <= 0 ? 0 : value;
            }
            return 0;
        }

        private void Shutdown(bool shutdown)
        {
            using Process process = new Process();
            process.StartInfo.FileName = "shutdown";
            process.StartInfo.Arguments = shutdown ? shutdownCommand : abortShutdownCommand;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
            process.Close();
        }

        [DebuggerDisplay("{CpuIndex} - {CoreIndex} : {Usage}")]
        public class CpuCoreUsage
        {
            public int CoreIndex { get; set; }
            public int CpuIndex { get; set; }
            public CounterSample Sample { get; set; }
            public double Usage { get; set; }
        }
    }
}
