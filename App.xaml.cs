

using Back_It_Up.Services;
using Back_It_Up.Stores;
using Back_It_Up.ViewModels.Pages;
using Back_It_Up.ViewModels.Windows;
using Back_It_Up.Views.Pages;
using Back_It_Up.Views.Windows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System.IO;
using System.Reflection;
using System.Windows.Threading;

namespace Back_It_Up
{

    public partial class App
    {

        private static readonly IHost _host = Host
            .CreateDefaultBuilder()
            .ConfigureAppConfiguration(c => { c.SetBasePath(Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)); })
            .ConfigureServices((context, services) =>
            {
                services.AddHostedService<ApplicationHostService>();

                services.AddSingleton<MainWindow>();
                services.AddSingleton<MainWindowViewModel>();
                services.AddSingleton<INavigationService, NavigationService>();
                services.AddSingleton<ISnackbarService, SnackbarService>();
                services.AddSingleton<IContentDialogService, ContentDialogService>();

                services.AddSingleton<DashboardPage>();
                services.AddSingleton<DashboardViewModel>();
                services.AddSingleton<SettingsPage>();
                services.AddSingleton<SettingsViewModel>();
                services.AddSingleton<BackupPage>();
                services.AddSingleton<BackupViewModel>();
                services.AddSingleton<SourceExplorerPage>();
                services.AddSingleton<SourceExplorerViewModel>();
                services.AddSingleton<DestinationExplorerPage>();
                services.AddSingleton<DestinationExplorerViewModel>();
                services.AddSingleton<RestorePage>();
                services.AddSingleton<RestoreViewModel>();
                services.AddSingleton<LogsPage>();
                services.AddSingleton<LogsViewModel>();
                services.AddSingleton<LogDetailsPage>();
                services.AddSingleton<LogDetailsViewModel>();

                services.AddSingleton<BackupStore>();
            }).Build();


        public static T GetService<T>()
            where T : class
        {
            return _host.Services.GetService(typeof(T)) as T;
        }


        private async void OnStartup(object sender, StartupEventArgs e)
        {
            string logFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BackItUp", "logs.txt");

            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(logFilePath, shared: true)
            .CreateLogger();

            if (e.Args.Length > 0 && e.Args[0] == "-s")
            {
                string backupName = e.Args.Length > 1 ? e.Args[1] : string.Empty;
                BackupStore store = GetService<BackupStore>();
                await store.SelectedBackup.PerformScheduledBackup(backupName);

                await _host.StopAsync();
                _host.Dispose();
                Current.Shutdown();
                return;
            }

            _host.Start();
            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();
        }


        private async void OnExit(object sender, ExitEventArgs e)
        {
            Log.CloseAndFlush();
            await _host.StopAsync();
            _host.Dispose();
        }

        private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        }
    }
}
