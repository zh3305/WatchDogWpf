using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Windows.Forms;

namespace WatchDogWpf;

//����������
public sealed partial class ProcessConfig : ObservableValidator
{
    [ObservableProperty]
    [Required(ErrorMessage = "����·������Ϊ��")]
    private string _appPath = "";

    //���в���
    [ObservableProperty]
    private string _arguments = "";

    //ι����ַ
    [ObservableProperty]
    private string _feedingAddress = "";

    [Required]
    //ι����ʽ
    [ObservableProperty]
    private FeedingMode _feedingMode = FeedingMode.None;

    [ObservableProperty] private bool _isEnable = true;

    [ObservableProperty]
    [Required(ErrorMessage = "�������Ʋ���Ϊ��")]
    private string _title = "";

    public ProcessConfig()
    {
        Id = new Guid();
    }

    public IEnumerable<KeyValuePair<FeedingMode, string>> FeedingModes => Enums.GetValues<FeedingMode>()
        .Select(value => new KeyValuePair<FeedingMode, string>(value, value.AsString(EnumFormat.Description)));

    public Guid Id { get; private set; }
    //ι�����
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