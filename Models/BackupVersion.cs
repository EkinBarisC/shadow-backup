using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    public class BackupVersion
    {
        public int Version { get; set; }
        public string BackupZipFilePath { get; set; }
        public DateTime DateCreated { get; set; }
        public BackupSetting BackupSetting { get; set; }

    }
}
