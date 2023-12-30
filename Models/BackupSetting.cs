using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    public partial class BackupSetting : ObservableObject
    {
        [ObservableProperty]
        private BackupMethod selectedBackupMethod;

        [ObservableProperty]
        private string selectedBackupScheme;

        [ObservableProperty]
        private string selectedCleaningOption;

        [ObservableProperty]
        private int daysToKeepBackups;

        [ObservableProperty]
        private int incrementalBackupVersionCount;
    }

    public enum BackupMethod
    {
        Full, Incremental
    }

}
