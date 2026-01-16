using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Drawing;
using System.Text.Json;

namespace shortcut
{
    public class HotkeySettings
    {
        public Keys ToggleKey { get; set; }
        public uint ToggleModifiers { get; set; }
        public Keys ExitKey { get; set; }
        public uint ExitModifiers { get; set; }
    }

    public class HotkeyConfig
    {
        public string? Toggle { get; set; }
        public string? Exit { get; set; }
    }

    public class AppConfig
    {
        public HotkeyConfig Hotkeys { get; set; } = new HotkeyConfig();
    }

    public static class HotkeyConfigLoader
    {
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;
        private const uint MOD_SHIFT = 0x0004;
        private const uint MOD_WIN = 0x0008;

        public static HotkeySettings Load(string path)
        {
            if (!File.Exists(path))
                return GetDefaultSettings();

            try
            {
                string json = File.ReadAllText(path);
                AppConfig? config = JsonSerializer.Deserialize<AppConfig>(json);
                if (config == null || config.Hotkeys == null)
                    return GetDefaultSettings();

                return Parse(config.Hotkeys);
            }
            catch
            {
                return GetDefaultSettings();
            }
        }

        private static HotkeySettings GetDefaultSettings()
        {
            var toggle = ParseHotkeyString("F7");
            var exit = ParseHotkeyString("F8");

            return new HotkeySettings
            {
                ToggleKey = toggle.key,
                ToggleModifiers = toggle.modifiers,
                ExitKey = exit.key,
                ExitModifiers = exit.modifiers
            };
        }

        private static HotkeySettings Parse(HotkeyConfig config)
        {
            string toggleString = string.IsNullOrWhiteSpace(config.Toggle) ? "F7" : config.Toggle;
            string exitString = string.IsNullOrWhiteSpace(config.Exit) ? "F8" : config.Exit;

            var toggle = ParseHotkeyString(toggleString);
            var exit = ParseHotkeyString(exitString);

            return new HotkeySettings
            {
                ToggleKey = toggle.key,
                ToggleModifiers = toggle.modifiers,
                ExitKey = exit.key,
                ExitModifiers = exit.modifiers
            };
        }

        private static (Keys key, uint modifiers) ParseHotkeyString(string input)
        {
            string[] parts = input.Split('+', StringSplitOptions.RemoveEmptyEntries);
            uint modifiers = 0;
            Keys key = Keys.None;

            foreach (string raw in parts)
            {
                string part = raw.Trim();
                if (part.Length == 0)
                    continue;

                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                    part.Equals("Control", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= MOD_CONTROL;
                }
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= MOD_ALT;
                }
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= MOD_SHIFT;
                }
                else if (part.Equals("Win", StringComparison.OrdinalIgnoreCase) ||
                         part.Equals("Windows", StringComparison.OrdinalIgnoreCase))
                {
                    modifiers |= MOD_WIN;
                }
                else
                {
                    if (Enum.TryParse(part, true, out Keys parsed))
                        key = parsed;
                }
            }

            if (key == Keys.None)
                key = Keys.F7;

            return (key, modifiers);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            HotkeySettings settings = HotkeyConfigLoader.Load("appsettings.json");
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(settings));
        }
    }

    public class MainForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int HOTKEY_ID = 1;
        private const int HOTKEY_EXIT_ID = 2;

        private readonly HotkeySettings hotkeySettings;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly NotifyIcon notifyIcon;
        private readonly ContextMenuStrip menu;

        public MainForm(HotkeySettings settings)
        {
            hotkeySettings = settings;
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Opacity = 0;
            FormBorderStyle = FormBorderStyle.FixedToolWindow;

            menu = new ContextMenuStrip();
            menu.Items.Add("退出", null, (_, __) => Application.Exit());

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Icon = SystemIcons.Application,
                ContextMenuStrip = menu
            };

            UpdateTray();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RegisterHotKey(Handle, HOTKEY_ID, hotkeySettings.ToggleModifiers, (uint)hotkeySettings.ToggleKey);
            RegisterHotKey(Handle, HOTKEY_EXIT_ID, hotkeySettings.ExitModifiers, (uint)hotkeySettings.ExitKey);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnregisterHotKey(Handle, HOTKEY_ID);
            UnregisterHotKey(Handle, HOTKEY_EXIT_ID);
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleProxy();
            }
            else if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_EXIT_ID)
            {
                Application.Exit();
            }

            base.WndProc(ref m);
        }

        private void ToggleProxy()
        {
            bool enabled = IsProxyEnabled();
            bool newStatus = !enabled;
            SetProxyEnabled(newStatus);
            ShowNotification(newStatus);
            UpdateTray();
        }

        private static bool IsProxyEnabled()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            const string valueName = "ProxyEnable";

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, false))
            {
                if (key == null)
                    return false;

                object? value = key.GetValue(valueName);
                if (value == null)
                    return false;

                int dword = value is int i ? i : Convert.ToInt32(value);
                return dword != 0;
            }
        }

        private static void SetProxyEnabled(bool enabled)
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Internet Settings";
            const string valueName = "ProxyEnable";

            using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(keyPath, true))
            {
                if (key == null)
                    return;

                int dword = enabled ? 1 : 0;
                key.SetValue(valueName, dword, RegistryValueKind.DWord);
            }
        }

        private void ShowNotification(bool enabled)
        {
            string title = "系统代理状态";
            string text = enabled ? "代理已启用 (F7 切换)" : "代理已禁用 (F7 切换)";
            notifyIcon.BalloonTipTitle = title;
            notifyIcon.BalloonTipText = text;
            notifyIcon.ShowBalloonTip(3000);
        }

        private void UpdateTray()
        {
            bool enabled = IsProxyEnabled();
            notifyIcon.Text = enabled ? "代理: 已启用" : "代理: 已禁用";
            notifyIcon.Icon = enabled ? SystemIcons.Shield : SystemIcons.Application;
        }
    }
}
