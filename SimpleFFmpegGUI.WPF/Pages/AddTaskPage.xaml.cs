using Enterwell.Clients.Wpf.Notifications;
using FFMpegCore.Exceptions;
using FzLib;
using FzLib.Collection;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json;
using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WPF.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SimpleFFmpegGUI.WPF.Pages
{
    public class AddTaskPageViewModel : INotifyPropertyChanged
    {
        public AddTaskPageViewModel(QueueManager queue)
        {
            Queue = queue;
        }

        public IEnumerable TaskTypes => Enum.GetValues(typeof(TaskType));
        private TaskType type;

        public event PropertyChangedEventHandler PropertyChanged;

        public TaskType Type
        {
            get => type;
            set
            {
                this.SetValueAndNotify(ref type, value, nameof(Type));
                CanAddFile = value is TaskType.Code or TaskType.Concat;
            }
        }

        public QueueManager Queue { get; }

        private bool allowChangeType = true;

        public bool AllowChangeType
        {
            get => allowChangeType;
            set => this.SetValueAndNotify(ref allowChangeType, value, nameof(AllowChangeType));
        }

        private bool canAddFile;

        public bool CanAddFile
        {
            get => canAddFile;
            set => this.SetValueAndNotify(ref canAddFile, value, nameof(CanAddFile));
        }
    }

    /// <summary>
    /// Interaction logic for AddTaskPage.xaml
    /// </summary>
    public partial class AddTaskPage : UserControl
    {
        public AddTaskPageViewModel ViewModel { get; set; }

        public AddTaskPage(AddTaskPageViewModel viewModel)
        {
            ViewModel = viewModel;
            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            DataContext = ViewModel;
            InitializeComponent();
            presetsPanel.CodeArgumentsViewModel = argumentsPanel.ViewModel;
        }

        private bool canInitializeType = true;

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ViewModel.Type):
                    fileIOPanel.Update(ViewModel.Type);
                    argumentsPanel.Update(ViewModel.Type);
                    presetsPanel.Update(ViewModel.Type);
                    break;

                default:
                    break;
            }
        }

        private async void AddToQueueButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.Type is TaskType.Code)
                {
                    FFmpegManager.TestOutputArguments(argumentsPanel.GetOutputArguments());
                }
            }
            catch (FFmpegArgumentException ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex.Message, null, title: "Lỗi tham số");
                return;
            }

            IsEnabled = false;
            try
            {
                List<InputArguments> inputs = fileIOPanel.GetInputs();
                OutputArguments args = argumentsPanel.GetOutputArguments();
                switch (ViewModel.Type)
                {
                    case TaskType.Code://Bạn cần thêm tệp đầu vào vào tác vụ riêng biệt
                        foreach (var input in inputs)
                        {
                            TaskInfo task = null;
                            await Task.Run(() => task = TaskManager.AddTask(TaskType.Code, new List<InputArguments>() { input }, fileIOPanel.GetOutput(input), args));
                            Dispatcher.Invoke(() => App.ServiceProvider.GetService<TasksAndStatuses>().Tasks.Insert(0, UITaskInfo.FromTask(task)));
                        }
                        this.CreateMessage().QueueSuccess($"Đã thêm {inputs.Count} hàng đợi nhiệm vụ");
                        break;
                    case TaskType.Custom or TaskType.Compare://Không có đầu ra tệp nào tồn tại
                        {
                            TaskInfo task = null;
                            await Task.Run(() => task = TaskManager.AddTask(ViewModel.Type, inputs, null, args));
                            App.ServiceProvider.GetService<TasksAndStatuses>().Tasks.Insert(0, UITaskInfo.FromTask(task));
                            this.CreateMessage().QueueSuccess("Xếp hàng");
                        }
                        break;
                    default:
                        {
                            TaskInfo task = null;
                            await Task.Run(() => task = TaskManager.AddTask(ViewModel.Type, inputs, fileIOPanel.GetOutput(inputs[0]), args));
                            App.ServiceProvider.GetService<TasksAndStatuses>().Tasks.Insert(0, UITaskInfo.FromTask(task));
                            this.CreateMessage().QueueSuccess("Xếp hàng");
                        }
                        break;
                }
                if (Config.Instance.ClearFilesAfterAddTask)
                {
                    fileIOPanel.Reset();
                }

                if (Config.Instance.StartQueueAfterAddTask)
                {
                    await Task.Run(() => App.ServiceProvider.GetService<QueueManager>().StartQueue());
                    this.CreateMessage().QueueSuccess("Hàng đợi bắt đầu");
                }
                SaveAsLastOutputArguments(args);
            }
            catch (Exception ex)
            {
                this.CreateMessage().QueueError("Không thể tham gia hàng đợi", ex);
            }
            finally
            {
                IsEnabled = true;
            }
        }

        public void SetAsClone(TaskInfo task)
        {
            canInitializeType = false;
            //ViewModel.AllowChangeType = false;
            ViewModel.Type = task.Type;
            fileIOPanel.Update(task.Type, task.Inputs, task.Output);
            argumentsPanel.Update(task.Type, task.Arguments);
        }

        public void SetFiles(IEnumerable<string> files, TaskType type)
        {
            canInitializeType = false;
            ViewModel.Type = type;
            fileIOPanel.Update(type, files.Select(p => new InputArguments() { FilePath = p }).ToList(), null);
        }

        private async void SaveToPresetButton_Click(object sender, RoutedEventArgs e)
        {
            await presetsPanel.SaveToPresetAsync();
        }

        private void BrowseAndAddInputButton_Click(object sender, RoutedEventArgs e)
        {
            fileIOPanel.BrowseAndAddInput();
        }

        private void AddInputButton_Click(object sender, RoutedEventArgs e)
        {
            fileIOPanel.AddInput();
        }

        private void CommandBar_MouseEnter(object sender, MouseEventArgs e)
        {
            (sender as FrameworkElement).Focus();
        }

        private async void AddToRemoteHostButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ViewModel.Type is TaskType.Code)
                {
                    FFmpegManager.TestOutputArguments(argumentsPanel.GetOutputArguments());
                }
            }
            catch (FFmpegArgumentException ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex.Message, null, title: "Lỗi tham số");
                return;
            }

            var items = Config.Instance.RemoteHosts.Select(p =>
            new SelectDialogItem(p.Name, p.Address));
            var index = await CommonDialog.ShowSelectItemDialogAsync("Đảm bảo rằng bạn có một tệp có cùng tên trong thư mục đầu vào của máy chủ từ xa", items);
            if (index < 0)
            {
                return;
            }
            IsEnabled = false;
            try
            {
                var host = Config.Instance.RemoteHosts[index];
                List<InputArguments> inputs = fileIOPanel.GetInputs().Adapt<List<InputArguments>>();
                foreach (var i in inputs)
                {
                    //Đường dẫn tuyệt đối, chỉ để lại tên tệp
                    if (i.FilePath.Contains(':'))
                    {
                        i.FilePath = System.IO.Path.GetFileName(i.FilePath);
                    }
                    i.FilePath = ":" + i.FilePath;
                }
                string output = fileIOPanel.GetOutputFileName();
                OutputArguments args = argumentsPanel.GetOutputArguments();
                var data = new
                {
                    Inputs = inputs,
                    Output = output,
                    Argument = args,
                    Start = Config.Instance.StartQueueAfterAddTask
                };
                await PostAsync(host, "Task/Add/" + ViewModel.Type.ToString(), data);

                if (Config.Instance.ClearFilesAfterAddTask)
                {
                    fileIOPanel.Reset();
                }
                SaveAsLastOutputArguments(args);
                this.CreateMessage().QueueSuccess("Đã tham gia vào máy chủ từ xa" + host.Name);
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "Không thể tham gia máy chủ từ xa");
            }
            finally
            {
                IsEnabled = true;
            }
        }

        public static async Task PostAsync(RemoteHost host, string subUrl, object data)
        {
            HttpClient client = new HttpClient();
            string str = JsonConvert.SerializeObject(data);
            var content = new StringContent(str, Encoding.UTF8, "application/json");
            string url = host.Address.TrimEnd('/') + "/" + subUrl.TrimStart('/');
            if (!string.IsNullOrEmpty(host.Token))
            {
                client.DefaultRequestHeaders.Add("Authorization", host.Token);
            }
            var response = await client.PostAsync(url, content);

            var responseString = await response.Content.ReadAsStringAsync();
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                if (string.IsNullOrWhiteSpace(responseString))
                {
                    throw new HttpRequestException($"{response.StatusCode}");
                }
                else
                {
                    throw new HttpRequestException($"{response.StatusCode}：{responseString}");
                }
            }
        }

        private void SaveAsLastOutputArguments(OutputArguments arguments)
        {
            if (!Config.Instance.RememberLastArguments)
            {
                return;
            }
            Config.Instance.LastOutputArguments.AddOrSetValue(ViewModel.Type, arguments);
            Config.Instance.Save();
        }

        private void ClearFilesButton_Click(object sender, RoutedEventArgs e)
        {
            fileIOPanel.Reset();
        }

        private async void FFmpegArgsButton_Click(object sender, RoutedEventArgs e)
        {
            this.GetWindow().Activate();
            try
            {
                OutputArguments args = argumentsPanel.GetOutputArguments();
                await CommonDialog.ShowOkDialogAsync("Thông số đầu ra", FFmpegManager.TestOutputArguments(args));
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "Không lấy được tham số");
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (canInitializeType)
            {
                ViewModel.Type = TaskType.Code;
                canInitializeType = false;
            }
        }
    }
}
