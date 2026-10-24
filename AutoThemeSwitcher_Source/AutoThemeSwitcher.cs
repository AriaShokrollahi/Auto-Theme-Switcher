// Auto Theme Switcher
// Switches Windows between Light and Dark mode at times you choose.
// UI: Windows 11 Fluent Design style (see Fluent.cs).
// Compatible with the compiler that ships with Windows (C# 5 / .NET Framework 4).

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        Fluent.InitDpi();   // sharp text on high-DPI screens

        bool createdNew;
        using (Mutex mutex = new Mutex(true, "Local\\AutoThemeSwitcher_Mutex", out createdNew))
        {
            if (!createdNew)
            {
                // نسخه دیگری از برنامه در حال اجراست؛ از آن می‌خواهیم پنجره را نشان بدهد
                try { EventWaitHandle.OpenExisting("Local\\AutoThemeSwitcher_Show").Set(); }
                catch (Exception) { }
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool minimized = Array.IndexOf(args, "--minimized") >= 0;
            Application.Run(new MainForm(minimized));
        }
    }
}

class MainForm : Form
{
    const string AppKey = @"Software\AutoThemeSwitcher";
    const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "AutoThemeSwitcher";

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam,
        uint flags, uint timeout, out UIntPtr result);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);

    // تنظیمات (مقدار پیش‌فرض: روشن از ۷ صبح، تاریک از ۷ عصر)
    TimeSpan lightStart = new TimeSpan(7, 0, 0);
    TimeSpan darkStart = new TimeSpan(19, 0, 0);
    bool enabled = true;
    bool configured;

    bool? lastApplied;     // آخرین حالتی که برنامه اعمال کرده (null = هنوز هیچ)
    bool? appliedLight;    // تمی که ظاهر خود برنامه الان با آن نقاشی شده
    bool allowShow;
    bool exiting;

    NotifyIcon tray;
    System.Windows.Forms.Timer timer;
    FTimeBox tbLight, tbDark;
    FToggle tgEnabled, tgStartup;
    FText lblStatus;
    Icon appIcon;
    EventWaitHandle showEvent;

    public MainForm(bool startMinimized)
    {
        LoadSettings();

        appliedLight = IsLightNow();
        Fluent.Update(appliedLight.Value);

        appIcon = MakeIcon();
        BuildUi();
        BuildTray();

        // اگر مسیر برنامه عوض شده باشد، مسیر اجرای خودکار را به‌روز می‌کنیم
        if (IsStartupEnabled()) SetStartup(true);

        // بار اول (هنوز تنظیماتی ذخیره نشده) پنجره را نشان بده
        allowShow = !startMinimized || !configured;

        IntPtr forceHandle = this.Handle;   // برای BeginInvoke لازم است

        ListenForShowRequests();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 20000;   // هر ۲۰ ثانیه ساعت را چک می‌کند
        timer.Tick += delegate { CheckTime(); };
        timer.Start();

        CheckTime();
        UpdateStatus();
    }

    // ---------------------------------------------------------------
    // منطق اصلی
    // ---------------------------------------------------------------

    bool ShouldBeLight(TimeSpan now)
    {
        if (lightStart == darkStart) return true;
        if (lightStart < darkStart) return now >= lightStart && now < darkStart;
        return now >= lightStart || now < darkStart;   // بازه روشن از نیمه‌شب رد می‌شود
    }

    // فقط وقتی «حالت مطلوب» عوض شود تم را تغییر می‌دهد؛
    // پس اگر کاربر دستی تم را عوض کند تا زمان بعدی تغییر، دست نمی‌زند.
    void CheckTime()
    {
        if (!enabled) return;

        bool light = ShouldBeLight(DateTime.Now.TimeOfDay);
        if (lastApplied.HasValue && lastApplied.Value == light) return;

        lastApplied = light;
        if (IsLightNow() != light) SetTheme(light);
        ApplyTheme();
        UpdateStatus();
    }

    static bool IsLightNow()
    {
        return Fluent.SystemIsLight();
    }

    static void SetTheme(bool light)
    {
        int v = light ? 1 : 0;
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(PersonalizeKey))
        {
            k.SetValue("AppsUseLightTheme", v, RegistryValueKind.DWord);
            k.SetValue("SystemUsesLightTheme", v, RegistryValueKind.DWord);
        }

        // به برنامه‌های باز خبر می‌دهیم که تم عوض شده
        UIntPtr result;
        SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "ImmersiveColorSet", 0x2, 5000, out result);
    }

    // ظاهر خود برنامه هم با تم ویندوز عوض می‌شود
    void ApplyTheme()
    {
        bool light = IsLightNow();
        if (appliedLight.HasValue && appliedLight.Value == light) return;
        appliedLight = light;

        Fluent.Update(light);
        BackColor = Fluent.Window;
        Fluent.StyleTitleBar(Handle);
        Invalidate(true);
    }

    void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        try { BeginInvoke((MethodInvoker)delegate { ApplyTheme(); UpdateStatus(); }); }
        catch (Exception) { }
    }

    // ---------------------------------------------------------------
    // ذخیره و بارگذاری تنظیمات
    // ---------------------------------------------------------------

    void LoadSettings()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(AppKey))
        {
            if (k == null) { configured = false; return; }
            configured = true;

            TimeSpan ts;
            if (TimeSpan.TryParse(k.GetValue("LightStart") as string, out ts)) lightStart = ts;
            if (TimeSpan.TryParse(k.GetValue("DarkStart") as string, out ts)) darkStart = ts;
            try { enabled = Convert.ToInt32(k.GetValue("Enabled", 1)) != 0; }
            catch (Exception) { enabled = true; }
        }
    }

    void SaveSettings()
    {
        lightStart = tbLight.Value;
        darkStart = tbDark.Value;
        enabled = tgEnabled.Checked;

        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppKey))
        {
            k.SetValue("LightStart", lightStart.ToString(@"hh\:mm"));
            k.SetValue("DarkStart", darkStart.ToString(@"hh\:mm"));
            k.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
        }
        configured = true;

        SetStartup(tgStartup.Checked);

        lastApplied = null;   // تا بلافاصله طبق تنظیمات جدید اعمال شود
        CheckTime();
        UpdateStatus();

        tray.ShowBalloonTip(2000, "Auto Theme Switcher", "Settings saved.", ToolTipIcon.Info);
    }

    static bool IsStartupEnabled()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
        {
            return k != null && k.GetValue(RunName) != null;
        }
    }

    static void SetStartup(bool on)
    {
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (on) k.SetValue(RunName, "\"" + Application.ExecutablePath + "\" --minimized");
            else k.DeleteValue(RunName, false);
        }
    }

    // ---------------------------------------------------------------
    // رابط کاربری (Fluent)
    // ---------------------------------------------------------------

    void BuildUi()
    {
        Text = "Auto Theme Switcher";
        Font = Fluent.BodyFont;
        BackColor = Fluent.Window;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Fluent.S(440), Fluent.S(470));
        Icon = appIcon;

        Fluent.Label(this, "Auto Theme Switcher", Fluent.TitleFont, false, false, false, 24, 20, 392, 38);
        lblStatus = Fluent.Label(this, "", Fluent.BodyFont, true, false, false, 24, 58, 392, 24);

        // Card 1: times
        FCard card1 = new FCard();
        Fluent.Bounds(card1, 24, 96, 392, 112);
        card1.DividerYs = new int[] { 56 };
        Controls.Add(card1);

        Fluent.Label(card1, "Switch to Light mode at", Fluent.BodyFont, false, true, false, 16, 1, 220, 54);
        tbLight = new FTimeBox();
        Fluent.At(tbLight, 248, 10);
        card1.Controls.Add(tbLight);

        Fluent.Label(card1, "Switch to Dark mode at", Fluent.BodyFont, false, true, false, 16, 57, 220, 54);
        tbDark = new FTimeBox();
        Fluent.At(tbDark, 248, 66);
        card1.Controls.Add(tbDark);

        // Card 2: options
        FCard card2 = new FCard();
        Fluent.Bounds(card2, 24, 224, 392, 112);
        card2.DividerYs = new int[] { 56 };
        Controls.Add(card2);

        Fluent.Label(card2, "Enable automatic switching", Fluent.BodyFont, false, true, false, 16, 1, 300, 54);
        tgEnabled = new FToggle();
        Fluent.At(tgEnabled, 332, 17);
        card2.Controls.Add(tgEnabled);

        Fluent.Label(card2, "Start with Windows", Fluent.BodyFont, false, true, false, 16, 57, 300, 54);
        tgStartup = new FToggle();
        Fluent.At(tgStartup, 332, 73);
        card2.Controls.Add(tgStartup);

        Fluent.Label(this,
            "Closing this window keeps the app running in the system tray (next to the clock). " +
            "To quit completely, right-click the tray icon and choose Exit.",
            Fluent.SmallFont, true, false, true, 24, 348, 392, 44);

        FButton btnSave = new FButton("Save and apply");
        Fluent.Bounds(btnSave, 256, 412, 160, 36);
        btnSave.Click += delegate { SaveSettings(); };
        Controls.Add(btnSave);

        LoadToUi();
    }

    void LoadToUi()
    {
        tbLight.Value = lightStart;
        tbDark.Value = darkStart;
        tgEnabled.Checked = enabled;
        tgStartup.Checked = configured ? IsStartupEnabled() : true;
        UpdateStatus();
    }

    void UpdateStatus()
    {
        if (lblStatus == null) return;
        lblStatus.Text = "Windows is currently in " + (IsLightNow() ? "Light" : "Dark") + " mode";
    }

    // ---------------------------------------------------------------
    // آیکون کنار ساعت (Tray)
    // ---------------------------------------------------------------

    void BuildTray()
    {
        ContextMenuStrip menu = new ContextMenuStrip();
        menu.Items.Add("Open settings", null, delegate { ShowSettings(); });
        menu.Items.Add("Exit", null, delegate { ExitApp(); });

        tray = new NotifyIcon();
        tray.Icon = appIcon;
        tray.Text = "Auto Theme Switcher";
        tray.ContextMenuStrip = menu;
        tray.Visible = true;
        tray.DoubleClick += delegate { ShowSettings(); };
    }

    void ShowSettings()
    {
        allowShow = true;
        LoadToUi();
        Show();
        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        Activate();
    }

    void ExitApp()
    {
        exiting = true;
        tray.Visible = false;
        Application.Exit();
    }

    // اگر کاربر برنامه را دوباره باز کند، نسخه‌ای که در tray است پنجره‌اش را نشان می‌دهد
    void ListenForShowRequests()
    {
        showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, "Local\\AutoThemeSwitcher_Show");
        Thread th = new Thread(delegate ()
        {
            while (showEvent.WaitOne())
            {
                try { BeginInvoke((MethodInvoker)delegate { ShowSettings(); }); }
                catch (Exception) { }
            }
        });
        th.IsBackground = true;
        th.Start();
    }

    // ---------------------------------------------------------------
    // رفتار پنجره
    // ---------------------------------------------------------------

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Fluent.StyleTitleBar(Handle);
    }

    // وقتی با --minimized اجرا می‌شود پنجره اصلاً نمایش داده نمی‌شود
    protected override void SetVisibleCore(bool value)
    {
        base.SetVisibleCore(allowShow && value);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!exiting && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnFormClosing(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        if (tray != null) tray.Visible = false;
        base.OnFormClosed(e);
    }

    // ---------------------------------------------------------------
    // ساخت آیکون (نیمه سفید، نیمه سیاه)
    // ---------------------------------------------------------------

    static Icon MakeIcon()
    {
        using (Bitmap bmp = new Bitmap(32, 32))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            Rectangle r = new Rectangle(2, 2, 27, 27);
            g.FillPie(Brushes.White, r, 90, 180);
            g.FillPie(Brushes.Black, r, 270, 180);
            using (Pen pen = new Pen(Color.Gray, 2f)) g.DrawEllipse(pen, r);

            IntPtr h = bmp.GetHicon();
            Icon tmp = Icon.FromHandle(h);
            Icon icon = (Icon)tmp.Clone();
            tmp.Dispose();
            DestroyIcon(h);
            return icon;
        }
    }
}
