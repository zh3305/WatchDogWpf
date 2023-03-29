using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using WatchDogWpf.Config;

namespace WatchDogWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(@"Logs/.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information, retainedFileCountLimit: 7)
                // .WriteTo.Async(c => 
                //     c.File($"Logs/{DateTime.Now:yyyy-MM-dd}logs.txt", restrictedToMinimumLevel: LogEventLevel.Information)
                //     )
                .WriteTo.Async(c => c.Console(theme: AnsiConsoleTheme.Literate))
                .CreateLogger();

            Log.Information("程序启动 {0}",nameof(WatchDogWpf));
            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            Log.Logger.Information("程序退出 {0}", nameof(WatchDogWpf));
            SystemConfig.Instance.Save();
#if !NET481
            await Log.CloseAndFlushAsync();
#endif
        }
    }
}
