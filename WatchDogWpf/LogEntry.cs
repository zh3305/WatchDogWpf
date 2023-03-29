using System;

namespace WatchDogWpf;

public class LogEntry
{
    public DateTime Time { get; set; }
    public string Type { get; set; }
    public string Content { get; set; }
}