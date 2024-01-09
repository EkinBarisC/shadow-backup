using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.Views.Pages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.ViewModels.UserControls
{
    public partial class ScheduleViewModel : ObservableObject
    {
        [ObservableProperty]
        private List<string> frequencyTypeItems = new List<string> { "Minutes", "Days", "Weeks", "Months", "Years" };

        [ObservableProperty]
        private DateTime selectedDate = DateTime.Now;

        [ObservableProperty]
        private DateTime startTime;

        [ObservableProperty]
        private int frequency;

        [ObservableProperty]
        private string frequencyType = "Weeks";

        [ObservableProperty]
        private int selectedHour;

        [ObservableProperty]
        private int selectedMinute;

        [ObservableProperty]
        private ObservableCollection<int> hours = new ObservableCollection<int>(Enumerable.Range(0, 24));

        [ObservableProperty]
        private ObservableCollection<int> minutes = new ObservableCollection<int>(Enumerable.Range(0, 60));

        [RelayCommand]
        private void CreateScheduledTask()
        {
            BackupStore store = App.GetService<BackupStore>();

            DateTime startTime = new DateTime(SelectedDate.Year, SelectedDate.Month, SelectedDate.Day, SelectedHour, SelectedMinute, 0);

            store.SelectedBackup.CreateBackupTask($"Backup_{store.SelectedBackup.BackupName}", startTime, Frequency, FrequencyType.ToString());
        }

    }
}
