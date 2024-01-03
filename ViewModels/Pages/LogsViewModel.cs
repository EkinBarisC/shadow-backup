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
        // Implement logic to parse a line from the log file
        // For example, split the line by a delimiter and extract timestamp and message
        // Return null if the line doesn't match the expected format

        // Example implementation (adjust according to your log format):
        var parts = line.Split(new[] { ' ' }, 3);
        if (parts.Length >= 3 && DateTime.TryParse(parts[0], out var timestamp))
        {
            return new LogEntry
            {
                Timestamp = timestamp,
                Message = parts[2]
            };
        }
        return null;
    }
}
