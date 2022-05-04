﻿using FzLib;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SimpleFFmpegGUI.WPF.Model
{
    public class PerformanceTestLine
    {
        public const int CodecsCount = 3;
        public const int SizesCount = 4;
        public PerformanceTestLine()
        {
            Sizes = new PerformanceTestItem[CodecsCount]
            {
                new PerformanceTestItem(){IsChecked=true },
                new PerformanceTestItem(){IsChecked=true },
                new PerformanceTestItem(){IsChecked=true },
            };
        }
        public string Header { get; set; }
        public PerformanceTestItem[] Sizes { get; }
    }
    public class PerformanceTestItem:INotifyPropertyChanged
    {
        public bool IsChecked { get; set; }
        private double score;

        public event PropertyChangedEventHandler PropertyChanged;

        public double Score
        {
            get => score;
            set => this.SetValueAndNotify(ref score, value, nameof(Score));
        }

    }
}
