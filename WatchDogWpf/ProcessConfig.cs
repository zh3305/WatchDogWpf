using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Forms;

namespace WatchDogWpf;

//进程配置类
public sealed partial class ProcessConfig : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "程序路径不能为空")]
    private string _appPath = "";

    //运行参数
    [ObservableProperty]
    private string _arguments = "";

    //喂狗地址
    [ObservableProperty]
    private string _feedingAddress = "";

    [Required]
    //喂狗方式
    [ObservableProperty]
    private FeedingMode _feedingMode = FeedingMode.None;

    [ObservableProperty] private bool _isEnable = true;

    [ObservableProperty]
    [Required(ErrorMessage = "进程名称不能为空")]
    private string _title = "";

    public ProcessConfig()
    {
        Id = new Guid();
    }

    public IEnumerable<KeyValuePair<FeedingMode, string>> FeedingModes => Enums.GetValues<FeedingMode>()
        .Select(value => new KeyValuePair<FeedingMode, string>(value, value.AsString(EnumFormat.Description)));

    public Guid Id { get; private set; }
    //喂狗间隔
    // public int FeedingInterval { get; set; } = 5000;

    public bool Validate()
    {
        ValidateAllProperties();
        return !HasErrors;
    }

    [RelayCommand]
    private void ChooseFile()
    {
        var dialog = new OpenFileDialog();
        dialog.Filter = "Executable Files|*.exe";
        if (dialog.ShowDialog() == DialogResult.OK)
        {
            AppPath = dialog.FileName;
        }
    }
}