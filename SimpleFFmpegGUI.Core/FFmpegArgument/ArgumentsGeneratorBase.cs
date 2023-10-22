using SimpleFFmpegGUI.FFmpegLib;
using System.Collections.Generic;
using System.Linq;

namespace SimpleFFmpegGUI.FFmpegArgument
{
    public abstract class ArgumentsGeneratorBase
    {
        /// <summary>
        /// Tập hợp các cặp tham số đơn
        /// </summary>
        protected List<FFmpegArgumentItem> arguments = new List<FFmpegArgumentItem>();

        /// <summary>
        /// Tham số bổ sung
        /// </summary>
        /// <returns></returns>
        public virtual IEnumerable<FFmpegArgumentItem> ExtraArguments()
        {
            return Enumerable.Empty<FFmpegArgumentItem>();
        }

        /// <summary>
        /// Sắp<see cref="arguments"/>kết nối các tham số trong chuỗi
        /// </summary>
        /// <returns></returns>
        public virtual string GetArguments()
        {
            var args = arguments.Concat(ExtraArguments()).Where(p => p != null).ToList();
            while (args.Any(p => p.Other != null))
            {
                var hasOthers = args.Where(p => p.Other != null).ToList();
                foreach (var item in hasOthers)
                {
                    args.Add(item.Other);
                    item.Other = null;
                }
            }

            List<string> list = new List<string>();
            var groups = args.GroupBy(p => p.Parent).ToList();
            foreach (var group in groups)
            {
                if (group.Key == null)
                {
                    foreach (var arg in group)
                    {
                        list.Add($"-{arg.Key} {arg.Value}");
                    }
                }
                else
                {
                    List<string> subList = new List<string>();
                    foreach (var arg in group)
                    {
                        subList.Add($"{arg.Key}={arg.Value}");
                    }
                    list.Add($"-{group.Key} {string.Join(group.First().Seprator, subList)}");
                }
            }

            return string.Join(' ', list);
        }
    }
}
