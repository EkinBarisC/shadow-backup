using Back_It_Up;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Pages;
using Back_It_Up.Views.Pages;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;

public partial class LogsViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<LogEntry> logs;

    private readonly INavigationService _navigationService;

    public LogsViewModel(INavigationService navigationService)
    {
        LoadLogs();
        _navigationService = navigationService;
    }

    private void LoadLogs()
    {
        // Initialize the collection
        Logs = new ObservableCollection<LogEntry>();

        // Read the log file and parse each line
        string logFilePath = "C:\\Users\\User\\Documents\\backup_log202401.txt"; // Adjust the path as necessary
        var logLines = System.IO.File.ReadAllLines(logFilePath);
        foreach (var line in logLines)
        {
            var logEntry = ParseLogLine(line);
            if (logEntry != null)
            {
                Logs.Add(logEntry);
            }
        }
    }

    [RelayCommand]
    private void OpenLogDetails(LogEntry logEntry)
    {
        // Navigate to LogDetailsPage with the selected log entry
        BackupStore store = App.GetService<BackupStore>();
        store.CurrentLogEntry = logEntry;
        _navigationService.Navigate(typeof(LogDetailsPage));
    }


    private LogEntry ParseLogLine(string line)
    {
        // Split the line by spaces but limit the number of parts to 6 
        // (date, time, timezone, log level, and message)
        var parts = line.Split(new[] { ' ' }, 6);

        if (parts.Length >= 6)
        {
            var dateTimePart = $"{parts[0]} {parts[1]} {parts[2]}";
            if (DateTimeOffset.TryParse(dateTimePart, out var timestamp))
            {
                var logLevel = parts[3].Trim('[', ']');
                var message = parts[5];

                return new LogEntry
                {
                    Timestamp = timestamp.UtcDateTime,
                    LogLevel = logLevel,
                    Message = message
                };
            }
        }

        return null;
    }

}
