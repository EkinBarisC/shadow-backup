using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    class BackupItemSetting
    {
        public string SelectedBackupMethod { get; set; }
        public string SelectedBackupScheme { get; set; }
        public string SelectedCleaningOption { get; set; }
        public int DaysToKeepBackups { get; set; }
        public int IncrementalBackupVersionCount { get; set; }

    }
}
