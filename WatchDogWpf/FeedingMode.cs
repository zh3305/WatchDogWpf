using System.ComponentModel;
using System.Xml.Linq;

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
/*
�����ܵ���Named Pipes��ͨѶ

// ���������ܵ�������
NamedPipeServerStream pipeServer = new NamedPipeServerStream("MyNamedPipe");
// �ȴ��ͻ�������
pipeServer.WaitForConnection();
// ��ȡ�ܵ���ȡ��
StreamReader reader = new StreamReader(pipeServer);
// �ӹܵ��ж�ȡ����
string message = reader.ReadLine();
// �رչܵ�
pipeServer.Disconnect();
pipeServer.Close();
// �����ȡ��������
Console.WriteLine("Received message: " + message);

�ͻ���:
// ���������ܵ�������
NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "MyNamedPipe", PipeDirection.Out);
// �ȴ�����������
pipeClient.Connect();
// ��ȡ�ܵ�д����
StreamWriter writer = new StreamWriter(pipeClient);
// ��ܵ���д������
writer.WriteLine("Hello, named pipe!");
// ˢ�¹ܵ����ر�
writer.Flush();
pipeClient.Close();


�����ڴ�

// ���������ڴ�
using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("MySharedMemory", 1024))
{
    // ��ȡ�����ڴ��е�д����
    using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
    {
        // ������д�빲���ڴ�
        byte[] data = new byte[1024];
        accessor.WriteArray(0, data, 0, data.Length);
    }
}

// �򿪹����ڴ�
using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting("MySharedMemory"))
{
    // ��ȡ�����ڴ��еĶ�ȡ��
    using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
    {
        // �ӹ����ڴ��ж�ȡ����
        byte[] data = new byte[1024];
        accessor.ReadArray(0, data, 0, data.Length);
    }
}
}
*/