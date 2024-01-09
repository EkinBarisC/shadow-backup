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

        [RelayCommand]
        private void CreateScheduledTask()
        {
            BackupStore store = App.GetService<BackupStore>();

            string executablePath = Assembly.GetExecutingAssembly().Location;
            string arguments = $"-s \"{store.SelectedBackup.BackupName}\"";
            DateTime startTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, SelectedHour, SelectedMinute, 0);

            store.SelectedBackup.CreateBackupTask($"Backup_{store.SelectedBackup.BackupName}", executablePath, arguments, startTime, TimeSpan.FromMinutes(1));
        }

    }
}
