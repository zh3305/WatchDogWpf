using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Resources;
using WatchDogWpf.Config;
using Application = System.Windows.Application;
using DragEventArgs = System.Windows.DragEventArgs;

namespace WatchDogWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainWindowViewModel _model = new MainWindowViewModel();
        // private IConfigurationRoot configuration;
        private NotifyIcon notifyIcon;
        public MainWindow()
        {
            InitializeComponent();

            //加载配置文件
            Log.Information("加载配置文件");
            // SystemConfig.Instance.

            this.StateChanged += Window_StateChanged;
            this.Closing += OnClosing;

            InitNotifyIcon();

            // configuration = new ConfigurationBuilder()
            //     .SetBasePath(Directory.GetCurrentDirectory())
            //     .AddJsonFile("appsettings.json")
            //     .AddEnvironmentVariables()
            //     .Build();
            //加载配置
            this.DataContext = _model;

            // _model.Processes= new ObservableCollection<ProcessViewModel>()
            SystemConfig.Instance.ProcessConfigs ??= new List<ProcessConfig>();

            foreach (var processConfig in SystemConfig.Instance.ProcessConfigs)
            {
                _model.Processes.Add(new ProcessViewModel() { Config = processConfig });
            }
        }

        private void AutoStartItem_CheckedChanged(object sender, EventArgs e)
        {
            ToolStripMenuItem autoStartItem = (ToolStripMenuItem)sender;

            autoStartItem.Checked = SetAutoStartStatus(autoStartItem.Checked);
        }

        private System.Drawing.Icon ConvertToIcon(ImageSource imageSource)
        {
            var bitmap = new System.Drawing.Bitmap(GetBitmapFromImageSource(imageSource));
            return System.Drawing.Icon.FromHandle(bitmap.GetHicon());
        }

        private void exitMenuItem_Click(object sender, EventArgs e)
        {
            // 退出程序
            System.Windows.Application.Current.Shutdown();
        }

        private System.Drawing.Bitmap GetBitmapFromImageSource(ImageSource imageSource)
        {
            var bitmapSource = imageSource as BitmapSource;
            var bitmap = new System.Drawing.Bitmap(
                bitmapSource.PixelWidth,
                bitmapSource.PixelHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            var data = bitmap.LockBits(
                new System.Drawing.Rectangle(System.Drawing.Point.Empty, bitmap.Size),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            bitmapSource.CopyPixels(
                Int32Rect.Empty,
                data.Scan0,
                data.Height * data.Stride,
                data.Stride);
            bitmap.UnlockBits(data);
            return bitmap;
        }
        private void InitNotifyIcon()
        {
            // 创建 NotifyIcon 对象
            notifyIcon = new NotifyIcon();

            Uri uri = new Uri(@"Resources\watchDog.ico", UriKind.Relative);
            StreamResourceInfo info = Application.GetResourceStream(uri);

            // 设置图标
            notifyIcon.Icon = new Icon(info.Stream);

            // 设置提示文本
            notifyIcon.Text = "Watchdog";

            // 双击图标时还原窗口
            notifyIcon.DoubleClick += notifyIcon_DoubleClick;

            // 创建右键菜单
            ContextMenuStrip menuStrip = new ContextMenuStrip();

            // 创建一个勾选框菜单项
            ToolStripMenuItem autoStartItem = new ToolStripMenuItem("自动启动");
            autoStartItem.CheckOnClick = true;
            autoStartItem.Checked = SystemConfig.Instance.IsAutoStart; // 获取当前自启动状态
            autoStartItem.CheckedChanged += new EventHandler(AutoStartItem_CheckedChanged);

            // 将勾选框菜单项添加到右键菜单
            menuStrip.Items.Add(autoStartItem);

            // 创建启动菜单项
            ToolStripMenuItem startMenuItem = new ToolStripMenuItem();
            startMenuItem.Text = "启动";
            startMenuItem.Image =
                new Bitmap(Application.GetResourceStream(new Uri(@"Resources\启动.png", UriKind.Relative)).Stream);
            startMenuItem.Click += new EventHandler(startMenuItem_Click);
            menuStrip.Items.Add(startMenuItem);

            // 创建停止菜单项
            ToolStripMenuItem stopMenuItem = new ToolStripMenuItem();
            stopMenuItem.Text = "停止";
            stopMenuItem.Image =
                new Bitmap(Application.GetResourceStream(new Uri(@"Resources\停止.png", UriKind.Relative)).Stream);
            stopMenuItem.Enabled = false;
            stopMenuItem.Click += new EventHandler(stopMenuItem_Click);
            menuStrip.Items.Add(stopMenuItem);

            // 创建退出菜单项
            ToolStripMenuItem exitMenuItem = new ToolStripMenuItem();
            exitMenuItem.Text = "退出";
            exitMenuItem.Image =
                new Bitmap(Application.GetResourceStream(new Uri(@"Resources\退出.png", UriKind.Relative)).Stream);
            exitMenuItem.Click += new EventHandler(exitMenuItem_Click);

            menuStrip.Items.Add(exitMenuItem);
            // 设置右键菜单
            notifyIcon.ContextMenuStrip = menuStrip;

            // 显示状态栏图标
            notifyIcon.Visible = true;
        }

        private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (sender is ScrollViewer scrollViewer)
            {
                // 如果已经滚动到最底部，则自动滚动到最新的一条日志
                if (Math.Abs(scrollViewer.ScrollableHeight - scrollViewer.VerticalOffset) < 3)
                {
                    scrollViewer.ScrollToEnd();
                }
            }
        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            // 还原窗口
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        private void OnClosing(object? sender, CancelEventArgs e)
        {
            // 关闭状态栏图标
            if (!e.Cancel)
            {
                notifyIcon.Visible = false;
            }
        }
        private void ProcessConfigView_OnDrop(object sender, DragEventArgs e)
        {
            // 如果拖拽的是文件，获取文件路径并赋值给 AppPath 属性
            if (e.Data.GetDataPresent(System.Windows.DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(System.Windows.DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    // var processConfig = (ProcessConfig)   this.DataContext;
                    // processConfig. AppPath = files[0];
                    var model = (MainWindowViewModel)this.DataContext;
                    ProcessConfigView processConfigView = new ProcessConfigView();
                    ProcessConfig processConfig = new ProcessConfig();
                    processConfig.AppPath = files[0];
                    processConfigView.DataContext = processConfig;
                    if (processConfigView.ShowDialog() ?? false)
                    {
                        model.Processes.Add(new ProcessViewModel() { Config = processConfig });
                        SystemConfig.Instance.ProcessConfigs.Add(processConfig);
                        SystemConfig.Instance.Save();
                    }
                }
            }
        }

        //设置程序自启动
        private bool SetAutoStartStatus(bool autoStartEnabled)
        {
            try
            {
                // 获取当前程序的路径
                string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

                // 获取当前程序的名称
                // string exeName = System.IO.Path.GetFileName(appPath);
                string exeName = "看门狗";

                // 创建注册表项
                Microsoft.Win32.RegistryKey reg = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");
                if (autoStartEnabled)
                {
                    reg.SetValue(exeName, appPath);
                }
                else
                {
                    reg.DeleteValue(exeName);
                }
                reg.Close();
                SystemConfig.Instance.IsAutoStart = autoStartEnabled;
                return autoStartEnabled;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return !autoStartEnabled;
            }
        }
        private void SetNotifyIconImage(bool isRunning)
        {
            string iconFileName = isRunning ? "watchDog_green.ico" : "watchDog_read.ico";
            // string iconFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, iconFileName);

            Uri uri = new Uri(@"Resources\" + iconFileName, UriKind.Relative);
            StreamResourceInfo info = Application.GetResourceStream(uri);
            // if (File.Exists(iconFilePath))
            if (info != null)
            {
                // this.notifyIcon.Icon = new System.Drawing.Icon(iconFilePath);
                this.notifyIcon.Icon = new Icon(info.Stream);
            }
            else
            {
                System.Windows.MessageBox.Show($"Failed to load icon file '{iconFileName}'.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void startMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: 启动程序

            // 禁用启动菜单项
            ((ToolStripMenuItem)notifyIcon.ContextMenuStrip.Items[0]).Enabled = false;

            // 启用停止菜单项
            ((ToolStripMenuItem)notifyIcon.ContextMenuStrip.Items[1]).Enabled = true;

            // 更新图标
            SetNotifyIconImage(true);
        }

        private void stopMenuItem_Click(object sender, EventArgs e)
        {
            // TODO: 停止程序

            // 启用启动菜单项
            ((ToolStripMenuItem)notifyIcon.ContextMenuStrip.Items[0]).Enabled = true;

            // 禁用停止菜单项
            ((ToolStripMenuItem)notifyIcon.ContextMenuStrip.Items[1]).Enabled = false;

            // 更新图标
            SetNotifyIconImage(false);
        }
        private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = WindowState.Minimized;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            // 最小化到状态栏图标
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
        }
    }
}