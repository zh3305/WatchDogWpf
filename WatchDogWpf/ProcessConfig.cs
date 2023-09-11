using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EnumsNET;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace WatchDogWpf;

//����������
public sealed partial class ProcessConfig : ObservableValidator
{
    [Required(ErrorMessage = "����·������Ϊ��")]
    private string _appPath = "";
    public string AppPath
    {
        get => _appPath;
        set
        {
            if (_appPath!=value)
            {
                OnPropertyChanging(nameof(AppPath));
                //�ж��Ƿ��ǿ�ݷ�ʽ
                if (Path.GetExtension(value)?.Equals(".lnk", StringComparison.OrdinalIgnoreCase) ==
                    true)
                {
                    Type shellObjectType = Type.GetTypeFromProgID("WScript.Shell");
                    dynamic windowsShell = Activator.CreateInstance(shellObjectType);
                    var shortcut = windowsShell.CreateShortcut(value);
                    var file = shortcut.TargetPath;

                    _appPath = file;
                    // Release the COM objects
                    shortcut = null;
                    windowsShell = null;
                }
                else
                {
                    _appPath = value;
                }
                OnPropertyChanged(nameof(AppPath));
            }
        }
    }

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