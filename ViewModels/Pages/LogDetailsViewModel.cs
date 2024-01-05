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


        public LogDetailsViewModel()
        {
            BackupStore store = App.GetService<BackupStore>();
            LogEntry = store.CurrentLogEntry;
        }

    }
}
