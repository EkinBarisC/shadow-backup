using Back_It_Up.Models;
using Back_It_Up.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.ViewModels.Pages
{
    public partial class LogDetailsViewModel : ObservableObject
    {
        [ObservableProperty]
        private LogEntry logEntry;

        private readonly INavigationService _navigationService;

        public LogDetailsViewModel(INavigationService navigationService)
        {

            _navigationService = navigationService;
            LoadLogEntry();
        }

        public void LoadLogEntry()
        {
            BackupStore store = App.GetService<BackupStore>();
            LogEntry = store.CurrentLogEntry;
        }

        [RelayCommand]
        public void GoBack()
        {
            _navigationService.GoBack();
        }

    }
}
