using System.Windows;

namespace WatchDogWpf;

public partial class ProcessConfigView : Window
{
    public ProcessConfigView()
    {
        InitializeComponent();
    }

    private void Cancel_OnClick(object sender, RoutedEventArgs e)
    {
        this.DialogResult = false;
        this.Close();
    }

    private void Save_OnClick(object sender, RoutedEventArgs e)
    {
        var processConfig = (ProcessConfig)   this.DataContext;
        if (!processConfig.Validate())
        {
            MessageBox.Show(string.Join("\r\n", processConfig.GetErrors()));
            return;
        }
        this.DialogResult = true;
        this.Close();
    }

    private void ProcessConfigView_OnDrop(object sender, DragEventArgs e)
    {
        // 如果拖拽的是文件，获取文件路径并赋值给 AppPath 属性
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            { 
                var processConfig = (ProcessConfig)   this.DataContext;
                processConfig. AppPath = files[0];
            }
        }
    }

    private void SelectProcessButton_Click(object sender, RoutedEventArgs e)
    {
        var processSelectionWindow = new ProcessSelectionWindow();
        var processViewModel = new ProcessSelectionViewModel();
        processSelectionWindow.DataContext = processViewModel;
        processSelectionWindow.ShowDialog();
        if (processViewModel.DialogResult == true)
        {
            var selectedProcess = processViewModel.SelectedProcess;
            if (selectedProcess != null)
            {
                // 更新界面上的信息
                var processConfig = (ProcessConfig)   this.DataContext;
                processConfig.AppPath= selectedProcess.MainModule.FileName;
                processConfig.Title = selectedProcess.ProcessName;

            }
        }
    }
}