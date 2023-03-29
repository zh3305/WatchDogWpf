using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows.Documents;
using System.Xml;

namespace WatchDogWpf.Config;

//public class config: PropertHelper
public sealed class SystemConfig : DictionaryBasedConfig
{    
    // 定义心跳检测周期，单位为毫秒
    public int WATCHDOG_INTERVAL{ get; set; } = 1000;

    internal static string Path { get; set; } = "Config.json";
    public List<ProcessConfig> ProcessConfigs { get; set; } = new List<ProcessConfig>();
    //是否自启动
    public bool IsAutoStart { get; set; } = false;

    public static SystemConfig Instance { set; get; }
    public int MissCount { get; set; } = 5;


    static SystemConfig()
    {
        if (Instance == null)
        {
            if (!File.Exists(Path))
            {
                Instance = new SystemConfig();
                Instance.Save();
            }
            else
            {
                try
                {
                    var readAllText = File.ReadAllText("Config.json");
                    Instance = JsonSerializer.Deserialize<SystemConfig>(readAllText) ?? new SystemConfig();
                }
                catch (Exception ex)
                {
                    // logger.Error($"配置文件存在,但未能正确的读取... ", ex);
                    Instance = new SystemConfig();
                }
            }
        }
    }


    public void Save()
    {
        File.WriteAllText(Path, JsonSerializer.Serialize(Instance));
    }
}