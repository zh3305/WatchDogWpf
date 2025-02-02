﻿using System;
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
using System.Threading;

namespace WatchDogWpf
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private static Mutex _mutex = null;
        private const string MutexName = "WatchDogWpf_SingleInstance_Mutex";

        protected override void OnStartup(StartupEventArgs e)
        {
            bool createdNew;
            _mutex = new Mutex(true, MutexName, out createdNew);

            if (!createdNew)
            {
                // 程序已经在运行
                MessageBox.Show("程序已经在运行中", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                _mutex = null;
                Current.Shutdown();
                return;
            }

            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug()
#else
                .MinimumLevel.Information()
#endif
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.File(AppDomain.CurrentDomain.BaseDirectory+@"Logs/.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Information, retainedFileCountLimit: 7)
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
#if !NET481&&!NET48
            await Log.CloseAndFlushAsync();
#endif
            if (_mutex != null)
            {
                _mutex.ReleaseMutex();
                _mutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}
