using Back_It_Up.Models;
using Microsoft.Extensions.Logging.Abstractions;
using System.Collections.ObjectModel;

public partial class LogsViewModel : ObservableObject
{
    private ObservableCollection<LogEntry> _logs;

    public ObservableCollection<LogEntry> Logs
    {
        get => _logs;
        set => SetProperty(ref _logs, value);
    }

    public LogsViewModel()
    {
        LoadLogs();
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
