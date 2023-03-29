using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.Reflection;
using WatchDogWpf.Config;

namespace WatchDogWpf;

public partial class ProcessViewModel : ObservableObject
{
    [ObservableProperty]
    private ProcessConfig _config;

    private double _cpu;

    [ObservableProperty]
    private TimeSpan _duration;

    private IntPtr _heartbeatAddress;

    [ObservableProperty]
    private DateTime _lastHeartbeatTime;

    private double _memory;

    //错过喂狗次数
    [ObservableProperty]
    private int _missCount = 0;

    private Process _process;

    [ObservableProperty]
    private int _startCount = 0;
    [ObservableProperty]
    private DateTime? _startTime = null;

    public ProcessViewModel(Process process)
    {
        _process = process;
        _startTime = process.StartTime;
        // 获取心跳函数的地址
        _heartbeatAddress = GetHeartbeatAddress();
    }

    public ProcessViewModel()
    {
    }

    public double Cpu
    {
        get => _cpu;
        set
        {
            _cpu = value;
            OnPropertyChanged();
        }
    }

    public IntPtr HeartbeatAddress
    {
        get => _heartbeatAddress;
        set
        {
            _heartbeatAddress = value;
            OnPropertyChanged();
        }
    }

    public double Memory
    {
        get => _memory;
        set
        {
            _memory = value;
            OnPropertyChanged();
        }
    }

    // public string Title => _process.MainWindowTitle;
    //
    // public string Path => _process.MainModule.FileName;
    public Process Process
    {
        get => _process;
        set => _process = value;
    }

    public IntPtr ProcessHandle => Process?.Handle ?? IntPtr.Zero;
    private IntPtr GetHeartbeatAddress()
    {
        // 获取当前进程中的所有Module
        var modules = Process.GetCurrentProcess().Modules;

        // 获取心跳函数的MethodInfo
        var module = modules[0];
        var moduleAssembly = Assembly.LoadFrom(module.FileName);
        var moduleType = moduleAssembly.GetType("MyProgram.Program");
        var heartbeatMethod = moduleType.GetMethod("Heartbeat");

        if (heartbeatMethod == null)
        {
            throw new InvalidOperationException("Heartbeat method not found.");
        }

        // 获取心跳函数的地址
        var heartbeatAddress = heartbeatMethod.MethodHandle.GetFunctionPointer();

        return heartbeatAddress;
    }

    [RelayCommand]
    private void ToggleEnable()
    {
        Config.IsEnable = !Config.IsEnable;
        SystemConfig.Instance.Save();
    }
}