using Back_It_Up.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.ViewModels.UserControls
{
    public partial class OptionsViewModel : ObservableObject
    {

        [ObservableProperty]
        private BackupSetting backupSetting = new BackupSetting();

        public OptionsViewModel()
        {

        }

        [RelayCommand]
        public void Save()
        {
            Console.WriteLine("ekin");
        }

    }
}
