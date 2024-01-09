using Back_It_Up.Models;
using Back_It_Up.Stores;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.ViewModels.UserControls
{
    public partial class ScheduleViewModel : ObservableObject
    {
        [ObservableProperty]
        private DateTime startDate;

        [ObservableProperty]
        private DateTime startTime;

        [ObservableProperty]
        private int frequency;

        [ObservableProperty]
        private string frequencyType;

        [ObservableProperty]
        private int _selectedHour;

        [ObservableProperty]
        private int _selectedMinute;

        [ObservableProperty]
        private ObservableCollection<int> _hours = new ObservableCollection<int>(Enumerable.Range(0, 24));

        [ObservableProperty]
        private ObservableCollection<int> _minutes = new ObservableCollection<int>(Enumerable.Range(0, 60));

        public ScheduleViewModel()
        {
            // get backup store
            BackupStore store = App.GetService<BackupStore>();
            //store.SelectedBackup.CreateBackupTask()
        }

    }
}
