﻿using Enterwell.Clients.Wpf.Notifications;
using FzLib;
using FzLib.WPF;
using Mapster;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.FzExtension;
using ModernWpf.FzExtension.CommonDialog;
using Newtonsoft.Json;
using SimpleFFmpegGUI.Manager;
using SimpleFFmpegGUI.Model;
using SimpleFFmpegGUI.WPF.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
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
    /// <summary>
    /// Interaction logic for PresetsPage.xaml
    /// </summary>
    public partial class PresetsPage : UserControl
    {
        public PresetsPage(PresetsPageViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = ViewModel;
            InitializeComponent();
            Loaded += (s, e) => ViewModel.FillPresets();
        }

        public PresetsPageViewModel ViewModel { get; set; }
        private async void DeleteAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (await CommonDialog.ShowYesNoDialogAsync("Xóa giá trị đặt trước", $"Xóa tất cả các cài đặt trước của tất cả các loại? "))
            {
                IsEnabled = false;
                try
                {
                    PresetManager.DeletePresets();
                    ViewModel.FillPresets();
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "Xóa không thành công");
                }
                finally
                {
                    IsEnabled = true;
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var preset = (sender as Button).DataContext as CodePreset;
            if (await CommonDialog.ShowYesNoDialogAsync("Xóa cài đặt trước", $"Bạn có muốn xóa không“{preset.Name}”？"))
            {
                ViewModel.DeletePreset(preset);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var path = new FileFilterCollection().Add("Tệp cấu hình", "json").CreateSaveFileDialog().SetParent(this.GetWindow()).SetDefault("Cài đặt trước hộp công cụ FFmpeg.json").GetFilePath();
            if (path != null)

            {
                var json = PresetManager.Export();
                File.WriteAllText(path, json, new UTF8Encoding());
                this.CreateMessage().QueueSuccess("Export thành công");
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            var path = new FileFilterCollection().Add("Tệp cấu hình", "json").CreateOpenFileDialog().SetParent(this.GetWindow()).GetFilePath();
            if (path != null)

            {
                try
                {
                    PresetManager.Import(File.ReadAllText(path, new UTF8Encoding()));
                    ViewModel.FillPresets();
                    this.CreateMessage().QueueSuccess("Nhập thành công, cài đặt trước cùng tên đã được cập nhật");
                }
                catch (Exception ex)
                {
                    await CommonDialog.ShowErrorDialogAsync(ex, "Import không thành công");
                }
            }
        }

        private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is CodePreset oldPreset)
            {
            }
            if (e.AddedItems.Count > 0 && e.AddedItems[0] is CodePreset preset)
            {

                grd.RowDefinitions[2].Height = new GridLength(48);
                lvw.IsHitTestVisible = false;
                lvw.ScrollIntoView(preset);
                grd.RowDefinitions[4].Height = new GridLength(1,GridUnitType.Star);
                argumentsPanel.Update(preset.Type, preset.Arguments);
            }
            else
            {
                grd.RowDefinitions[2].Height = new GridLength(1,GridUnitType.Star);
                lvw.IsHitTestVisible = true;
                grd.RowDefinitions[4].Height = new GridLength(0);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(ViewModel.SelectedPreset!=null); 
            try
            {
                ViewModel.SelectedPreset.Arguments = argumentsPanel.GetOutputArguments();
                PresetManager.UpdatePreset(ViewModel.SelectedPreset);
                ViewModel.SelectedPreset = null;
            }
            catch (Exception ex)
            {
                this.CreateMessage().QueueError("Không thể cập nhật cài đặt trước", ex);
            }
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.SelectedPreset = null;
        }

        private void SetDefaultButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.Assert(ViewModel.SelectedPreset != null);
            ViewModel.Presets.ForEach(p => p.Default = false);
            ViewModel.SelectedPreset.Default = true;
            PresetManager.SetDefaultPreset(ViewModel.SelectedPreset.Id);
        }
    }

    public class PresetsPageViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CodePreset> presets;

        private CodePreset selectedPreset;

        private TaskType type = TaskType.Code;

        public PresetsPageViewModel()
        {
        }
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<CodePreset> Presets
        {
            get => presets;
            set => this.SetValueAndNotify(ref presets, value, nameof(Presets));
        }

        public CodePreset SelectedPreset
        {
            get => selectedPreset;
            set => this.SetValueAndNotify(ref selectedPreset, value, nameof(SelectedPreset));
        }


        public IEnumerable TaskTypes => Enum.GetValues(typeof(TaskType));
        public TaskType Type
        {
            get => type;
            set
            {
                this.SetValueAndNotify(ref type, value, nameof(Type));
                FillPresets();
            }
        }
        public void DeletePreset(CodePreset preset)
        {
            PresetManager.DeletePreset(preset.Id);
            Presets.Remove(preset);
        }

        public void FillPresets()
        {
            Presets = new ObservableCollection<CodePreset>(PresetManager.GetPresets(Type));
        }

    }
}
