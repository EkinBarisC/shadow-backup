using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Serilog;


namespace Back_It_Up.Models
{


    public static class Logger
    {
        private static ILogger log;

        static Logger()
        {
            // Configure Serilog
            log = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console()
                .WriteTo.File("C:\\Users\\User\\Documents\\backup_log.txt", rollingInterval: RollingInterval.Month)
                .CreateLogger();
        }

        public static void Information(string message)
        {
            log.Information(message);
        }

        public static void Error(string message, Exception ex)
        {
            log.Error(ex, message);
        }

        // Add more methods as needed for Debug, Warning, etc.
    }


}
