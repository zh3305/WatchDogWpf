using System.ComponentModel;

namespace WatchDogWpf;

/// <summary>
/// 喂狗方式
/// </summary>
public enum FeedingMode
{
    [Description("不喂狗")] None = 0,
    [Description("共享内存")] SharedMemory = 1,
    [Description("命名管道")] NamedPipe = 2,
    [Description("WebApi")] WebApi = 3,
    [Description("检测Ui未响应")] Responding = 4,
    [Description("心跳文件")] HeartbeatFile
}