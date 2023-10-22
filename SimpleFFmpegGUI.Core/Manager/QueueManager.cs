using SimpleFFmpegGUI.FFmpegArgument;
using SimpleFFmpegGUI.Model;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using Tasks = System.Threading.Tasks;

namespace SimpleFFmpegGUI.Manager
{
    public class QueueManager
    {
        private Logger logger = new Logger();
        private bool cancelQueue = false;

        /// <summary>
        /// ID kế hoạch hàng đợi được sử dụng để xác định xem đó có phải là gói hợp lệ hay không
        /// </summary>
        private int currentScheduleID = 0;

        private bool running = false;
        private DateTime? scheduleTime = null;
        private List<FFmpegManager> taskProcessManagers = new List<FFmpegManager>();

        public QueueManager()
        {
        }

        /// <summary>
        /// Thay đổi task
        /// </summary>
        public event NotifyCollectionChangedEventHandler TaskManagersChanged;

        /// <summary>
        /// Hàng đợi Task
        /// </summary>
        public FFmpegManager MainQueueManager => Managers.FirstOrDefault(p => p.Task == MainQueueTask);

        /// <summary>
        /// Tác vụ cho hàng đợi chính
        /// </summary>
        public TaskInfo MainQueueTask { get; private set; }

        /// <summary>
        /// Tất cả task
        /// </summary>
        public IReadOnlyList<FFmpegManager> Managers => taskProcessManagers.AsReadOnly();

        /// <summary>
        /// Quản lý hiệu suất năng lượng
        /// </summary>
        public PowerManager PowerManager { get; } = new PowerManager();

        /// <summary>
        /// Nhiệm vụ độc lập
        /// </summary>
        public IEnumerable<TaskInfo> StandaloneTasks => Managers.Where(p => p.Task != MainQueueTask).Select(p => p.Task);

        /// <summary>
        /// Tất cả nhiệm vụ
        /// </summary>
        public IEnumerable<TaskInfo> Tasks => Managers.Select(p => p.Task);

        /// <summary>
        /// Hủy hàng đợi chính
        /// </summary>
        public void Cancel()
        {
            CheckMainQueueProcessingTaskManager();
            cancelQueue = true;

            MainQueueManager.Cancel();
        }

        /// <summary>
        /// Hủy hàng đợi chính
        /// </summary>
        public Task CancelAsync()
        {
            CheckMainQueueProcessingTaskManager();
            cancelQueue = true;

            return MainQueueManager.CancelAsync();
        }

        /// <summary>
        /// Hủy hàng đợi đã lên lịch
        /// </summary>
        public void CancelQueueSchedule()
        {
            currentScheduleID++;
            scheduleTime = null;
        }

        public DateTime? GetQueueScheduleTime()
        {
            return scheduleTime;
        }

        public void ResumeMainQueue()
        {
            CheckMainQueueProcessingTaskManager();
            MainQueueManager.Resume();
        }

        /// <summary>
        /// Lập kế hoạch cho một nhiệm vụ để bắt đầu hàng đợi tại một số thời điểm trong tương lai
        /// </summary>
        /// <param name="time"></param>
        /// <exception cref="ArgumentException"></exception>
        public async void ScheduleQueue(DateTime time)
        {
            if (time <= DateTime.Now)
            {
                throw new ArgumentException("Thời gian dự kiến sớm hơn thời gian hiện tại");
            }
            currentScheduleID++;
            scheduleTime = time;
            int id = currentScheduleID;
            await Task.Delay(time - DateTime.Now);
            if (id == currentScheduleID)
            {
                StartQueue();
            }
        }

        /// <summary>
        /// Bắt đầu hàng đợi
        /// </summary>
        public async void StartQueue()
        {
            if (running)
            {
                logger.Warn("Hàng đợi đang chạy và hàng đợi bắt đầu không thành công");
                return;
            }
            running = true;
            scheduleTime = null;
            logger.Info("Bắt đầu hàng đợi");
            using FFmpegDbContext db = FFmpegDbContext.GetNew();
            List<TaskInfo> tasks;
            while (!cancelQueue && GetQueueTasks(db).Any())
            {
                tasks = GetQueueTasks(db).OrderBy(p => p.CreateTime).ToList();

                var task = tasks[0];

                await ProcessTaskAsync(db, task, true);
            }
            running = false;
            bool cancelManually = cancelQueue;
            cancelQueue = false;
            logger.Info("Hàng đợi hoàn tất");
            if (!cancelManually && PowerManager.ShutdownAfterQueueFinished)
            {
                PowerManager.Shutdown();
            }
        }

        /// <summary>
        /// Bắt đầu một nhiệm vụ độc lập
        /// </summary>
        /// <param name="id"></param>
        /// <exception cref="Exception"></exception>
        public async void StartStandalone(int id)
        {
            using FFmpegDbContext db = FFmpegDbContext.GetNew();
            var task = db.Tasks.Find(id);
            if (task == null)
            {
                throw new Exception("Không thể tìm thấy tác vụ có ID " + id + " ");
            }
            if (task.Status != TaskStatus.Queue)
            {
                throw new Exception("Trạng thái của tác vụ không chính xác và không thể bắt đầu tác vụ");
            }
            if (Tasks.Any(p => p.Id == task.Id))
            {
                throw new Exception("Tác vụ đang được tiến hành, nhưng trạng thái không phải là Đang xử lý");
            }
            logger.Info(task, "Bắt đầu một nhiệm vụ độc lập");
            await ProcessTaskAsync(db, task, false);
            logger.Info(task, "Hoàn thành nhiệm vụ độc lập");
        }

        /// <summary>
        /// Tạm dừng nhiệm vụ chính
        /// </summary>
        public void SuspendMainQueue()
        {
            CheckMainQueueProcessingTaskManager();
            MainQueueManager.Suspend();
        }

        private void AddManager(TaskInfo task, FFmpegManager ffmpegManager, bool main)
        {
            taskProcessManagers.Add(ffmpegManager);
            if (main)
            {
                MainQueueTask = task;
            }
            TaskManagersChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, ffmpegManager));
        }

        private void CheckMainQueueProcessingTaskManager()
        {
            if (!Managers.Any(p => p.Task == MainQueueTask))
            {
                throw new Exception("Hàng đợi chính không chạy hoặc tác vụ hiện tại đang được chuẩn bị");
            }
        }

        private IQueryable<TaskInfo> GetQueueTasks(FFmpegDbContext db)
        {
            return db.Tasks
                .Where(p => p.Status == TaskStatus.Queue)
                .Where(p => !p.IsDeleted);
        }

        private async Task ProcessTaskAsync(FFmpegDbContext db, TaskInfo task, bool main)
        {
            FFmpegManager ffmpegManager = new FFmpegManager(task);

            task.Status = TaskStatus.Processing;
            task.StartTime = DateTime.Now;
            task.Message = "";
            task.FFmpegArguments = "";
            db.Update(task);
            db.SaveChanges();
            AddManager(task, ffmpegManager, main);
            try
            {
                await ffmpegManager.RunAsync();
                task.Status = TaskStatus.Done;
            }
            catch (Exception ex)
            {
                if (task.Status != TaskStatus.Cancel)
                {
                    logger.Error(task, "Chạy lỗi：" + ex.ToString());
                    task.Status = TaskStatus.Error;
                    task.Message = ex is FFmpegArgumentException ? 
                        ex.Message : ffmpegManager.GetErrorMessage() ?? "Đối với các lỗi đang chạy, vui lòng kiểm tra nhật ký";
                }
                else
                {
                    logger.Warn(task, "Nhiệm vụ bị hủy");
                }
            }
            finally
            {
                task.FinishTime = DateTime.Now;
            }
            db.Update(task);
            db.SaveChanges();
            RemoveManager(task, ffmpegManager, main);
        }

        private void RemoveManager(TaskInfo task, FFmpegManager ffmpegManager, bool main)
        {
            if (!taskProcessManagers.Remove(ffmpegManager))
            {
                throw new Exception("Người quản lý không có trong bộ sưu tập của người quản lý");
            }
            if (main)
            {
                MainQueueTask = null;
            }
            TaskManagersChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, ffmpegManager));
        }
    }
}
