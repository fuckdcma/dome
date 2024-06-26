﻿using FzLib;
using FzLib.Collection;
using Mapster;
using SimpleFFmpegGUI.Dto;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System;
using System.Windows.Shell;

namespace SimpleFFmpegGUI.WPF.Model
{
    public class TasksAndStatuses : TaskCollectionBase
    {
        private List<UITaskInfo> processingTasks;

        public TasksAndStatuses(QueueManager queue)
        {
            Refresh();
            Queue = queue;
            queue.TaskManagersChanged += Queue_TaskManagersChanged;
        }

        public List<UITaskInfo> ProcessingTasks
        {
            get => processingTasks;
            private set => this.SetValueAndNotify(ref processingTasks, value, nameof(ProcessingTasks));
        }

        public QueueManager Queue { get; }

        public ObservableCollection<StatusDto> Statuses { get; } = new ObservableCollection<StatusDto>();

        public void NotifyTaskReseted(UITaskInfo task)
        {
            if (!Tasks.Any(p => p.Id == task.Id))
            {
                Tasks.Add(task);
            }
        }

        public override void Refresh()
        {
            var tasks = TaskManager.GetCurrentTasks(App.AppStartTime);
            Tasks = new ObservableCollection<UITaskInfo>(tasks.Adapt<List<UITaskInfo>>());
        }

        private static void GetMainWindowAnd(Action<MainWindow> action)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var mainWindow = App.ServiceProvider.GetService<MainWindow>();
                Debug.Assert(mainWindow != null);
                action(mainWindow);
            });
        }

        private void Manager_StatusChanged(object sender, EventArgs e)
        {
            var manager = sender as FFmpegManager;
            var newStatus = manager.GetStatus();

            var task = Tasks.FirstOrDefault(p => p.Id == newStatus.Task.Id);
            Debug.Assert(task != null);
            Debug.Assert(task.ProcessStatus != null);

            task.ProcessStatus = newStatus;
            if (manager == Queue.MainQueueManager || Queue.Managers.Count == 1)//Hàng đợi chính hoặc chỉ một tác vụ, hiển thị tiến trình trên thanh trạng thái
            {
                GetMainWindowAnd(mainWindow =>
                {
                    if (newStatus.HasDetail)
                    {
                        mainWindow.TaskbarItemInfo.ProgressState =
                        manager.Paused ? TaskbarItemProgressState.Paused : TaskbarItemProgressState.Normal;
                        mainWindow.TaskbarItemInfo.ProgressValue = newStatus.Progress.Percent;
                    }
                    else
                    {
                        mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Indeterminate;
                    }
                });
            }
        }

        /// <summary>
        /// Hàng đợi tác vụ thay đổi
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Queue_TaskManagersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add)//Thêm nhiệm vụ mới
            {
                var manager = e.NewItems[0] as FFmpegManager;
                var unstartStatus = new StatusDto(manager.Task); //Đặt StatusDto làm trình giữ chỗ trước tiên, vì Trạng thái chưa được tạo
                var task = Tasks.FirstOrDefault(p => p.Id == manager.Task.Id);//Tìm UITaskInfo tương ứng
                Debug.Assert(task != null);
                task.UpdateSelf(); //Cập nhật UITaskInfo với thực thể TaskInfo
                task.ProcessStatus = unstartStatus;
                task.ProcessManager = manager;
                if (manager == Queue.MainQueueManager)
                {
                    Statuses.Insert(0, unstartStatus);
                }
                else
                {
                    Statuses.Add(unstartStatus);
                }
                manager.StatusChanged += Manager_StatusChanged;
            }
            else
            {
                var manager = e.OldItems[0] as FFmpegManager;
                var status = Statuses.FirstOrDefault(p => p.Task.Id == manager.Task.Id);
                Debug.Assert(status != null);

                var task = Tasks.FirstOrDefault(p => p.Id == manager.Task.Id);
                Debug.Assert(task != null);
                task.ProcessManager = null;
                task.ProcessStatus = null;
                task.UpdateSelf();

                Statuses.Remove(status);
                manager.StatusChanged -= Manager_StatusChanged;
                GetMainWindowAnd(async mainWindow =>
                {
                    if (task.Status is TaskStatus.Error or TaskStatus.Cancel)
                    {
                        mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.Error;
                        await System.Threading.Tasks.Task.Delay(1000);
                    }
                    mainWindow.TaskbarItemInfo.ProgressState = TaskbarItemProgressState.None;
                    Debug.Write("finish");
                });
            }
            ProcessingTasks = Tasks.Where(p => p.ProcessStatus != null).ToList();
        }
    }
}
