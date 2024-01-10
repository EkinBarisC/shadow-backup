using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    public class LogEntry
    {
        public DateTimeOffset Timestamp { get; set; }
        public string LogLevel { get; set; }
        public string Message { get; set; } // backup completed, restore completed, started
        public string FormattedTimestamp
        {
            get
            {
                // Convert the DateTimeOffset to local time and format it
                return Timestamp.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
    }

}
