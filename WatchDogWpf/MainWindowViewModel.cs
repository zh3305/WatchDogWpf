using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
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
using WatchDogWpf.Config;

namespace WatchDogWpf;

public partial class MainWindowViewModel : ObservableObject, INotifyPropertyChanged
{
    // [ObservableProperty] private ObservableCollection<LogEntry> _logEntries;
    [ObservableProperty] private ObservableCollection<string> _logEntries = new ObservableCollection<string>();

    [ObservableProperty] private ObservableCollection<ProcessViewModel> _processes;

    [ObservableProperty] private ProcessViewModel _selectedProcess;
    private readonly Timer _watchdogTimer;

    public MainWindowViewModel()
    {
        Processes = new ObservableCollection<ProcessViewModel>();
        // LogEntries = new ObservableCollection<LogEntry>();

        // ������ʱ��
        _watchdogTimer = new Timer(SystemConfig.Instance.WATCHDOG_INTERVAL);
        _watchdogTimer.Elapsed += OnWatchdogTimer;
        _watchdogTimer.AutoReset = false;
        _watchdogTimer.Enabled = true;
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
            MessageBox.Show("��ѡ��һ������");
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
        //��������
        processViewModel.Process.Kill();
        processViewModel.Process = null;
        processViewModel.MissCount = 0;
        processViewModel.StartTime = null;
        OutLog($"����:{processViewModel.Config.Title} �ѽ���", "��������");
    }

    private void OnWatchdogTimer(object sender, ElapsedEventArgs e)
    {
        foreach (ProcessViewModel processViewModel in Processes)
        {
            try
            {
                if (processViewModel.Process == null || processViewModel.Process.HasExited)
                {
                    //���ҽ���
                    Process[] processes = Process.GetProcessesByName(processViewModel.Config.Title);
                    if (processes.Length > 0)
                    {
                        processViewModel.Process = processes[0];
                        processViewModel.StartTime = processViewModel.Process.StartTime;
                    }
                    else
                    {
                        //��������
                        if (!processViewModel.Config.IsEnable || !Start(processViewModel))
                        {
                            //��������ʧ��!
                            processViewModel.StartTime = null;
                            processViewModel.Duration = new TimeSpan();
                            continue;
                        }
                    }
                }

                //ˢ��Process ����,�����»�ȡ����ˢ��
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
                        //     // ��ȡ����س����������Ϣ
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
                        OutLog($"����:{processViewModel.Config.Title} ������ⳬʱ!", "�������");
                        Kill(processViewModel);
                        continue;
                    }

                    // ��������ʱ��
                    processViewModel.LastHeartbeatTime = e.SignalTime;
                }

                // ��������ʱ��
                processViewModel.Duration = e.SignalTime - processViewModel.StartTime.Value;

                // ���� CPU ���ڴ�ռ����
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                           $"SELECT * FROM Win32_PerfFormattedData_PerfProc_Process WHERE IDProcess={processViewModel.Process.Id}"))
                {
                    ManagementObjectCollection collection = searcher.Get();
                    if (collection.Count > 0)
                    {
                        ManagementObject obj = collection.OfType<ManagementObject>().FirstOrDefault();
                        if (obj != null)
                        {
                            //����������� CPU ռ���ʼ����Ƚϸ��ӣ���Ҫ����������ء�����������ļ򵥼��㷽ʽֻ��ȡ��ǰ������ CPU �����ĵ�ʱ��Ƭ����������������������ļ��㷽ʽ��ͬ����˴��ڲ�ࡣ
                            processViewModel.Cpu = double.Parse(obj["PercentProcessorTime"].ToString());
                            processViewModel.Memory = double.Parse(obj["WorkingSetPrivate"].ToString()) / 1024 / 1024;
                        }
                    }
                }

                //Cpu ռ�ù����Զ�����
                // if (processViewModel.Cpu > SystemConfig.Instance.CpuThreshold)
                // {
                //     OutLog($"����:{processViewModel.Config.Title} CPU ռ�ù���,��������", "CPU ռ�ù���");
                //     Kill(processViewModel);
                //     continue;
                // }
            }
            catch
            {
                // ���Է��ʱ��ܾ��Ľ���
            }
        }

        // ������һ����ʱ��
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
            MessageBox.Show("�������ó���·��");
            return;
        }

        //�򿪳�������Ŀ¼
        var appPath = Path.GetDirectoryName(configAppPath);
        Process.Start("explorer.exe", appPath);
    }

    private void OutLog(string content, string type = "������", Exception exception=null)
    {
        Application.Current.Dispatcher.Invoke(new Action(() =>
        {
            //�������3000�������
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
            OutLog($"·��������:{processView.Config.AppPath}", "��������");
            return false;
        }

        // ��������������Ϣ
        var startInfo = new ProcessStartInfo();
        startInfo.UseShellExecute = false;
        startInfo.FileName = processView.Config.AppPath;
        startInfo.WorkingDirectory= Path.GetDirectoryName(processView.Config.AppPath);
        // �����Զ��廷������
        startInfo.EnvironmentVariables["FeedingMode"] = processView.Config.FeedingMode.AsString(EnumFormat.Name);
        startInfo.EnvironmentVariables["FeedingInterval"] = SystemConfig.Instance.WATCHDOG_INTERVAL.ToString();//processView.Config.FeedingInterval.ToString()
        // ��������
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
                OutLog($"����:{processView.Config.Title} ������", "��������");
                return true;
            }

        }
        catch (Exception e)
        {
            OutLog($"����:{processView.Config.Title} ����ʧ��!"+e.Message, "��������",e);
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
}