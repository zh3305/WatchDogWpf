using System.ComponentModel;

namespace WatchDogWpf;

/// <summary>
/// ι����ʽ
/// </summary>
public enum FeedingMode
{
    [Description("��ι��")] None = 0,
    [Description("�����ڴ�")] SharedMemory = 1,
    [Description("�����ܵ�")] NamedPipe = 2,
    [Description("WebApi")] WebApi = 3,
    [Description("���Uiδ��Ӧ")] Responding = 4,
    [Description("�����ļ�")] HeartbeatFile
}