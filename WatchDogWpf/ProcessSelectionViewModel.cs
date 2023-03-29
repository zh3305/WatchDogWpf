using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace WatchDogWpf;

public partial class ProcessSelectionViewModel : ObservableObject
{
    private readonly Process[] _allProcesses;
    private string _processName;

    [NotifyCanExecuteChangedFor(nameof(SelectCommand))] [ObservableProperty]
    private Process _selectedProcess;

    public ProcessSelectionViewModel()
    {
        _allProcesses = Process.GetProcesses()
            .Where(p => p.SessionId != 0 && p.MainWindowHandle != IntPtr.Zero && !p.HasExited)
            .ToArray();
        Processes = new ObservableCollection<Process>(_allProcesses);
    }

    public bool? DialogResult { get; set; }

    public ObservableCollection<Process> Processes { get; }

    public string ProcessName
    {
        get => _processName;
        set
        {
            _processName = value;
            FilterProcesses();
        }
    }
    private bool CanSelectProcess => SelectedProcess != null;

    [RelayCommand]
    private void Cancel()
    {
        DialogResult = false;
        Application.Current.Windows.OfType<ProcessSelectionWindow>().FirstOrDefault()?.Close();
    }

    private void FilterProcesses()
    {
        Processes.Clear();

        foreach (Process process in _allProcesses)
        {
            if (string.IsNullOrEmpty(_processName) || process.ProcessName.ToLower().Contains(_processName.ToLower()))
            {
                Processes.Add((process));
            }
        }
    }
    [RelayCommand(CanExecute = nameof(CanSelectProcess))]
    private void Select()
    {
        if (SelectedProcess != null)
        {
            DialogResult = true;
            Application.Current.Windows.OfType<ProcessSelectionWindow>().FirstOrDefault()?.Close();
        }
    }
}