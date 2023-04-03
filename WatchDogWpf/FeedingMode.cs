using System.ComponentModel;
using System.Xml.Linq;

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
/*
命名管道（Named Pipes）通讯

// 创建命名管道服务器
NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyNamedPipe");
// 等待客户端连接
pipeServer.WaitForConnection();
// 获取管道读取器
StreamReader reader = new StreamReader(pipeServer);
// 从管道中读取数据
string message = reader.ReadLine();
// 关闭管道
pipeServer.Disconnect();
pipeServer.Close();
// 输出读取到的数据
Console.WriteLine("Received message: " + message);

客户端:
// 连接命名管道服务器
NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "MyNamedPipe", PipeDirection.Out);
// 等待服务器连接
pipeClient.Connect();
// 获取管道写入器
StreamWriter writer = new StreamWriter(pipeClient);
// 向管道中写入数据
writer.WriteLine("Hello, named pipe!");
// 刷新管道并关闭
writer.Flush();
pipeClient.Close();


共享内存

// 创建共享内存
using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("MySharedMemory", 1024))
{
    // 获取共享内存中的写入器
    using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
    {
        // 将数据写入共享内存
        byte[] data = new byte[1024];
        accessor.WriteArray(0, data, 0, data.Length);
    }
}

// 打开共享内存
using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("MySharedMemory"))
{
    // 获取共享内存中的读取器
    using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
    {
        // 从共享内存中读取数据
        byte[] data = new byte[1024];
        accessor.ReadArray(0, data, 0, data.Length);
    }
}
}
*/