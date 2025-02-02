using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using LiveCharts.Wpf;
using Serilog;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Timers;
using System.Windows;
using LiveCharts;
using WatchDogWpf.Config;
using System.Drawing;
using System.Diagnostics.PerformanceData;
using System.Windows.Media; // 使用WPF的Color

namespace WatchDogWpf;

public partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    // [ObservableProperty] private ObservableCollection<LogEntry> _logEntries;
    [ObservableProperty] private ObservableCollection<string> _logEntries = new ObservableCollection<string>();

    [ObservableProperty] private ObservableCollection<ProcessViewModel> _processes;

    [ObservableProperty] private ProcessViewModel _selectedProcess;
    private readonly Timer _watchdogTimer;
    private readonly Timer _performanceTimer;
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    private const int MAX_POINTS = 30; // 显示最近30个数据点

    public SeriesCollection CpuSeries { get; set; }
    public SeriesCollection MemorySeries { get; set; }


    public MainWindowViewModel()
    {
        Processes = new ObservableCollection<ProcessViewModel>();
        // LogEntries = new ObservableCollection<LogEntry>();

        // 创建定时器
        _watchdogTimer = new Timer(SystemConfig.Instance.WATCHDOG_INTERVAL);
        _watchdogTimer.Elapsed += OnWatchdogTimer;
        _watchdogTimer.AutoReset = false;
        _watchdogTimer.Enabled = true;

        // 初始化性能计数器
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "% Committed Bytes In Use");

        // 初始化图表数据
        CpuSeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "CPU",
                Values = new ChartValues<double>(),
                PointGeometry = null,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 65, 105, 225)), // 半透明的蓝色
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(65, 105, 225))
            }
        };

        MemorySeries = new SeriesCollection
        {
            new LineSeries
            {
                Title = "Memory",
                Values = new ChartValues<double>(),
                PointGeometry = null,
                Fill = new SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 50, 205, 50)), // 半透明的绿色
                Stroke = new SolidColorBrush(System.Windows.Media.Color.FromRgb(50, 205, 50))
            }
        };

        // 创建定时器，每秒更新一次
        _performanceTimer = new Timer(1000);
        _performanceTimer.Elapsed += OnPerformanceTimer;
        _performanceTimer.AutoReset = true;
        _performanceTimer.Start();

    }

    [RelayCommand]
    private void Add()
    {
        ProcessConfigView processConfigView = new ProcessConfigView();
        ProcessConfig processConfig = new ProcessConfig();
        processConfigView.DataContext = processConfig;
        if (processConfigView.ShowDialog() ?? false)
        {
            Processes.Add(new ProcessViewModel() { Config = processConfig });
            SystemConfig.Instance.ProcessConfigs.Add(processConfig);
            SystemConfig.Instance.Save();
        }
    }

    private bool CheckSelectProcess()
    {
        if (SelectedProcess == null)
        {
            MessageBox.Show("请选择一个进程");
            return true;
        }

        return false;
    }

    [RelayCommand]
    private void Delete()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        SystemConfig.Instance.ProcessConfigs.Remove(SelectedProcess.Config);
        SystemConfig.Instance.Save();
        Processes.Remove(SelectedProcess);
    }

    [RelayCommand]
    private void Disable()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        SelectedProcess.Config.IsEnable = false;
        SystemConfig.Instance.Save();
    }

    [RelayCommand]
    private void Edit()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        ProcessConfigView processConfigView = new ProcessConfigView();
        ProcessConfig processConfig = new ProcessConfig();
        var config = SelectedProcess.Config.Clone();
        processConfigView.DataContext = config;
        if (processConfigView.ShowDialog() ?? false)
        {
            config.CopyTo(SelectedProcess.Config);
            SystemConfig.Instance.Save();
        }
    }

    [RelayCommand]
    private void Enable()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        SelectedProcess.Config.IsEnable = true;
        SystemConfig.Instance.Save();
    }

    private void Kill(ProcessViewModel processViewModel)
    {
        //重启进程
        processViewModel.Process.Kill();
        processViewModel.Process = null;
        processViewModel.MissCount = 0;
        processViewModel.StartTime = null;
        OutLog($"进程:{processViewModel.Config.Title} 已结束", "结束进程");
    }

    private void OnWatchdogTimer(object sender, ElapsedEventArgs e)
    {
        foreach (ProcessViewModel processViewModel in Processes)
        {
            try
            {
                if (processViewModel.Process == null || processViewModel.Process.HasExited)
                {
                    //查找进程
                    var processName = Path.GetFileNameWithoutExtension(processViewModel.Config.Title);
                    Process[] processes = Process.GetProcessesByName(processName);
                    if (processes.Length > 0)
                    {
                        processViewModel.Process = processes[0];
                        processViewModel.StartTime = processViewModel.Process.StartTime;
                    }
                    else
                    {
                        //启动进程
                        if (!processViewModel.Config.IsEnable || !Start(processViewModel))
                        {
                            //进程启动失败!
                            processViewModel.StartTime = null;
                            processViewModel.Duration = new TimeSpan();
                            continue;
                        }
                    }
                }

                //刷新Process 数据,不重新获取不会刷新
                processViewModel.Process = Process.GetProcessById(processViewModel.Process.Id);

                // processViewModel.Process.StandardOutput
                if (processViewModel.Config.IsEnable)
                {
                    switch (processViewModel.Config.FeedingMode)
                    {
                        case FeedingMode.SharedMemory:
                            break;

                        case FeedingMode.NamedPipe:
                            break;

                        case FeedingMode.WebApi:
                            break;
                        // case FeedingMode.ProcessMemory:
                        //     // 读取被监控程序的心跳消息
                        //     byte[] buffer = new byte[1];
                        //     WinApi.ReadProcessMemory(processViewModel.ProcessHandle, processViewModel.HeartbeatAddress, buffer, 1,
                        //         out int bytesRead);
                        //     break;
                        case FeedingMode.Responding:
                            if (processViewModel.Process.Responding)
                            {
                                processViewModel.MissCount = 0;
                            }
                            else
                            {
                                processViewModel.MissCount++;
                            }

                            break;

                        case FeedingMode.None:
                        default:
                            break;
                    }

                    if (processViewModel.MissCount > SystemConfig.Instance.MissCount)
                    {
                        OutLog($"进程:{processViewModel.Config.Title} 心跳检测超时!", "心跳检测");
                        Kill(processViewModel);
                        continue;
                    }

                    // 更新心跳时间
                    processViewModel.LastHeartbeatTime = e.SignalTime;
                }

                // 更新运行时长
                processViewModel.Duration = e.SignalTime - processViewModel.StartTime.Value;

                // 更新 CPU 和内存占用率
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                           $"SELECT * FROM Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess={processViewModel.Process.Id}"))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    if (collection.Count > 0)
                    {
                        ManagementObject obj = collection.OfType<ManagementObject>().FirstOrDefault();
                        if (obj != null)
                        {
                            //任务管理器的 CPU 占用率计算会比较复杂，需要考虑许多因素。而我们这里的简单计算方式只是取当前进程在 CPU 上消耗的时间片比例，可能与任务管理器的计算方式不同，因此存在差距。
                            processViewModel.Cpu = double.Parse(obj["PercentProcessorTime"].ToString());
                            processViewModel.Memory = double.Parse(obj["WorkingSetPrivate"].ToString()) / 1024 / 1024;
                        }
                    }
                }

                //Cpu 占用过高自动重启
                // if (processViewModel.Cpu > SystemConfig.Instance.CpuThreshold)
                // {
                //     OutLog($"进程:{processViewModel.Config.Title} CPU 占用过高,重启进程", "CPU 占用过高");
                //     Kill(processViewModel);
                //     continue;
                // }
            }
            catch
            {
                // 忽略访问被拒绝的进程
            }
        }

        // 启动下一个定时器
        _watchdogTimer.Start();
    }

    [RelayCommand]
    private void OpenDirectory()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        var configAppPath = SelectedProcess.Config.AppPath;
        if (string.IsNullOrEmpty(configAppPath))
        {
            MessageBox.Show("请先配置程序路径");
            return;
        }

        //打开程序所在目录
        var appPath = Path.GetDirectoryName(configAppPath);
        Process.Start("explorer.exe", appPath);
    }

    private void OutLog(string content, string type = "主进程", Exception exception=null)
    {
        Application.Current.Dispatcher.Invoke(new Action(() =>
        {
            //如果大于3000行则清空
            if (LogEntries.Count > 3000)
            {
                LogEntries.Clear();
            }

            LogEntries.Add($"{DateTime.Now:HH:mm:ss.fff}	 [{type}]	{content}");
            Log.Information(exception,"[{type}]	{content}", type, content);
        }));
    }

    [RelayCommand]
    private void Start()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        Start(SelectedProcess);
    }

    private bool Start(ProcessViewModel processView)
    {
        processView.StartCount++;
        if (!File.Exists(processView.Config.AppPath))
        {
            OutLog($"路径不存在:{processView.Config.AppPath}", "启动进程");
            return false;
        }

        // 创建进程启动信息
        var startInfo = new ProcessStartInfo();
        startInfo.UseShellExecute = false;
        startInfo.FileName = processView.Config.AppPath;
        startInfo.WorkingDirectory= Path.GetDirectoryName(processView.Config.AppPath);
        // 设置自定义环境变量
        startInfo.EnvironmentVariables["FeedingMode"] = processView.Config.FeedingMode.AsString(EnumFormat.Name);
        startInfo.EnvironmentVariables["FeedingInterval"] = SystemConfig.Instance.WATCHDOG_INTERVAL.ToString();//processView.Config.FeedingInterval.ToString()
        // 启动进程
        if (!string.IsNullOrEmpty(processView.Config.Arguments))
        {
            startInfo.Arguments = processView.Config.Arguments;
        }

        try
        {

            var processViewProcess = Process.Start(startInfo);
            if (processViewProcess != null)
            {
                processView.Process = processViewProcess;
                processView.StartTime = processView.Process.StartTime;
                processView.Config.Title = processView.Process.ProcessName;
                OutLog($"进程:{processView.Config.Title} 已启动", "启动进程");
                return true;
            }

        }
        catch (Exception e)
        {
            OutLog($"进程:{processView.Config.Title} 启动失败!"+e.Message, "启动进程",e);
        }
        return false;
    }

    [RelayCommand]
    private void Stop()
    {
        if (CheckSelectProcess())
        {
            return;
        }

        Kill(SelectedProcess);
    }

    private void OnPerformanceTimer(object sender, ElapsedEventArgs e)
    {
        try
        {
            // 获取CPU和内存使用率
            float cpuUsage = _cpuCounter.NextValue();
            float memoryUsage = _memoryCounter.NextValue();

            // 在UI线程更新图表
            Application.Current.Dispatcher.Invoke(() =>
            {
                var cpuValues = (ChartValues<double>)CpuSeries[0].Values;
                var memValues = (ChartValues<double>)MemorySeries[0].Values;

                // 添加新数据点
                cpuValues.Add(Math.Round(cpuUsage, 1));
                memValues.Add(Math.Round(memoryUsage, 1));

                // 保持固定数量的数据点
                if (cpuValues.Count > MAX_POINTS)
                {
                    cpuValues.RemoveAt(0);
                }
                if (memValues.Count > MAX_POINTS)
                {
                    memValues.RemoveAt(0);
                }
            });
        }
        catch (Exception ex)
        {
            OutLog($"性能监控异常: {ex.Message}", "性能监控", ex);
        }
    }

    // 在析构函数中释放资源
    ~MainWindowViewModel()
    {
        _watchdogTimer?.Dispose();
        _performanceTimer?.Dispose();
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
    }
}