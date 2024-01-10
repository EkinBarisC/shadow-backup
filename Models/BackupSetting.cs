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
        private BackupScheme selectedBackupScheme;

        [ObservableProperty]
        private CleaningOption selectedCleaningOption;

        [ObservableProperty]
        private int? daysToKeepBackups = 30;

        [ObservableProperty]
        private int? fullBackupFrequency = 7;
    }

    public enum BackupMethod
    {
        Full, Incremental
    }

    public enum BackupScheme
    {
        NoScheme,
        ContinuousDataProtection,
        PeriodicFullBackup
    }

    public enum CleaningOption
    {
        KeepAllBackups,
        CleanUpOldBackups,
    }

    public enum BackupStatus
    {
        NotStarted,
        InProgress,
        Complete,
        Failed,
        RestoreComplete,
        Loaded
    }

}
