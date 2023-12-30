using Back_It_Up.Models;
using Back_It_Up.Stores;
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
        private BackupSetting backupSetting;

        public OptionsViewModel()
        {
            BackupStore store = App.GetService<BackupStore>();
            BackupSetting = store.SelectedBackup.BackupSetting;
        }


    }
}
