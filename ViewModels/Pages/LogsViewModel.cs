using Back_It_Up;
using Back_It_Up.Models;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Pages;
using Back_It_Up.Views.Pages;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;
using System.IO;

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

    private async void LoadLogs()
    {
        // Initialize the collection
        Logs = new ObservableCollection<LogEntry>();
        string logFilePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackItUp", "logs.txt");

        // Read the log file and parse each line
        var logLines = new List<string>();
        using (var fileStream = new FileStream(logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var reader = new StreamReader(fileStream))
        {
            while (!reader.EndOfStream)
            {
                var line = await reader.ReadLineAsync();
                logLines.Add(line);
            }
        }

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
        // Split the line by spaces, but limit the number of parts to handle fixed format parts
        var parts = line.Split(new[] { ' ' }, 5);

        if (parts.Length >= 5)
        {
            var dateTimePart = $"{parts[0]} {parts[1]} {parts[2]}";
            if (DateTimeOffset.TryParse(dateTimePart, out var timestamp))
            {
                var logLevel = parts[3].Trim('[', ']');
                // The message is everything after the 4th space
                var message = parts[4];

                return new LogEntry
                {
                    Timestamp = timestamp,
                    LogLevel = logLevel,
                    Message = message
                };
            }
        }

        return null;
    }


}
