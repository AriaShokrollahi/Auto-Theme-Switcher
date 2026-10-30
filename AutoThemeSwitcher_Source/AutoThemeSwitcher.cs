// Auto Theme Switcher
// Switches Windows between Light and Dark mode (and optionally the wallpaper) at times you choose.
// UI: Windows 11 Fluent Design style (see Fluent.cs).
// Compatible with the compiler that ships with Windows (C# 5 / .NET Framework 4).

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
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
                // Another copy is already running: ask it to show its window.
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
    const string AppVersion = "1.1";
    const string AppKey = @"Software\AutoThemeSwitcher";
    const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "AutoThemeSwitcher";

    static readonly string WallDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        @"AutoThemeSwitcher\Wallpapers");

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, UIntPtr wParam, string lParam,
        uint flags, uint timeout, out UIntPtr result);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool SystemParametersInfo(uint action, uint param, string vparam, uint flags);

    [DllImport("user32.dll")]
    static extern bool DestroyIcon(IntPtr handle);

    // Settings (defaults: Light from 07:00, Dark from 19:00)
    TimeSpan lightStart = new TimeSpan(7, 0, 0);
    TimeSpan darkStart = new TimeSpan(19, 0, 0);
    bool enabled = true;
    bool configured;

    // Wallpaper settings
    bool wpEnabled;
    string lightWp = "", darkWp = "", lightWpName = "", darkWpName = "";
    int wpFit;   // 0 Fill, 1 Fit, 2 Stretch, 3 Center, 4 Tile

    bool? lastApplied;     // last mode the app applied (null = nothing yet)
    bool? appliedLight;    // theme the app's own window is currently painted with
    bool allowShow;
    bool exiting;

    NotifyIcon tray;
    System.Windows.Forms.Timer timer;
    FNavItem[] navs;
    FPage[] pages;
    FTimeBox tbLight, tbDark;
    FToggle tgEnabled, tgStartup, tgWallpaper;
    FSegmented segFit;
    FWallTile tileLight, tileDark;
    FText lblStatus1, lblStatus2;
    FButton btnSave, btnReset;
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

        // If the exe moved, refresh the autostart path
        if (IsStartupEnabled()) SetStartup(true);

        // First run (nothing saved yet): show the window
        allowShow = !startMinimized || !configured;

        IntPtr forceHandle = this.Handle;   // needed for BeginInvoke

        ListenForShowRequests();
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        timer = new System.Windows.Forms.Timer();
        timer.Interval = 20000;   // check the clock every 20 seconds
        timer.Tick += delegate { CheckTime(); };
        timer.Start();

        CheckTime();
        UpdateStatus();
    }

    // ---------------------------------------------------------------
    // Core logic
    // ---------------------------------------------------------------

    bool ShouldBeLight(TimeSpan now)
    {
        if (lightStart == darkStart) return true;
        if (lightStart < darkStart) return now >= lightStart && now < darkStart;
        return now >= lightStart || now < darkStart;   // the light range crosses midnight
    }

    void CheckTime() { CheckTime(false); }

    // Only acts when the target mode changes, so a manual theme change is left alone
    // until the next scheduled time. "force" is used right after saving settings.
    void CheckTime(bool force)
    {
        if (!enabled) return;

        bool light = ShouldBeLight(DateTime.Now.TimeOfDay);
        if (lastApplied.HasValue && lastApplied.Value == light && !force) return;

        bool firstRun = !lastApplied.HasValue;
        lastApplied = light;

        bool themeChanges = IsLightNow() != light;
        if (themeChanges) SetTheme(light);

        // At startup the wallpaper is only touched if the theme actually had to change.
        if (themeChanges || force || !firstRun) ApplyWallpaper(light);

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

        // Tell open apps the theme changed
        UIntPtr result;
        SendMessageTimeout((IntPtr)0xFFFF, 0x001A, UIntPtr.Zero, "ImmersiveColorSet", 0x2, 5000, out result);
    }

    static bool HasImage(string path)
    {
        return !string.IsNullOrEmpty(path) && File.Exists(path);
    }

    void ApplyWallpaper(bool light)
    {
        if (!wpEnabled) return;
        string path = light ? lightWp : darkWp;
        if (!HasImage(path)) return;

        try
        {
            string[] style = { "10", "6", "2", "0", "0" };   // Fill, Fit, Stretch, Center, Tile
            int fit = (wpFit >= 0 && wpFit < style.Length) ? wpFit : 0;
            using (RegistryKey k = Registry.CurrentUser.CreateSubKey(@"Control Panel\Desktop"))
            {
                k.SetValue("WallpaperStyle", style[fit], RegistryValueKind.String);
                k.SetValue("TileWallpaper", fit == 4 ? "1" : "0", RegistryValueKind.String);
            }
            SystemParametersInfo(0x0014, 0, path, 0x03);   // SPI_SETDESKWALLPAPER, update + broadcast
        }
        catch (Exception) { }
    }

    // The app's own window follows the Windows theme too
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
    // Saving and loading settings
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

            try { wpEnabled = Convert.ToInt32(k.GetValue("WallpaperEnabled", 0)) != 0; }
            catch (Exception) { wpEnabled = false; }
            try { wpFit = Convert.ToInt32(k.GetValue("WallpaperFit", 0)); }
            catch (Exception) { wpFit = 0; }
            if (wpFit < 0 || wpFit > 4) wpFit = 0;

            lightWp = (k.GetValue("LightWallpaper") as string) ?? "";
            darkWp = (k.GetValue("DarkWallpaper") as string) ?? "";
            lightWpName = (k.GetValue("LightWallpaperName") as string) ?? "";
            darkWpName = (k.GetValue("DarkWallpaperName") as string) ?? "";
        }
    }

    void SaveSettings()
    {
        lightStart = tbLight.Value;
        darkStart = tbDark.Value;
        enabled = tgEnabled.Checked;
        wpEnabled = tgWallpaper.Checked;
        wpFit = segFit.SelectedIndex;

        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppKey))
        {
            k.SetValue("LightStart", lightStart.ToString(@"hh\:mm"));
            k.SetValue("DarkStart", darkStart.ToString(@"hh\:mm"));
            k.SetValue("Enabled", enabled ? 1 : 0, RegistryValueKind.DWord);
            k.SetValue("WallpaperEnabled", wpEnabled ? 1 : 0, RegistryValueKind.DWord);
            k.SetValue("WallpaperFit", wpFit, RegistryValueKind.DWord);
            k.SetValue("LightWallpaper", lightWp ?? "");
            k.SetValue("DarkWallpaper", darkWp ?? "");
            k.SetValue("LightWallpaperName", lightWpName ?? "");
            k.SetValue("DarkWallpaperName", darkWpName ?? "");
        }
        configured = true;

        SetStartup(tgStartup.Checked);
        CleanWallpaperDir();

        lastApplied = null;   // apply the new settings right away
        CheckTime(true);
        UpdateStatus();

        tray.ShowBalloonTip(2000, "Auto Theme Switcher", "Settings saved.", ToolTipIcon.Info);
    }

    // Removes copied wallpaper files that are no longer selected
    void CleanWallpaperDir()
    {
        try
        {
            if (!Directory.Exists(WallDir)) return;
            foreach (string f in Directory.GetFiles(WallDir))
            {
                bool keep = string.Equals(f, lightWp, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(f, darkWp, StringComparison.OrdinalIgnoreCase);
                if (!keep)
                {
                    try { File.Delete(f); } catch (Exception) { }
                }
            }
        }
        catch (Exception) { }
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
    // User interface (Fluent)
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
        ClientSize = new Size(Fluent.S(820), Fluent.S(520));
        Icon = appIcon;

        // Sidebar
        string[] names = { "General", "Wallpaper", "About" };
        navs = new FNavItem[3];
        for (int i = 0; i < 3; i++)
        {
            int idx = i;
            navs[i] = new FNavItem(names[i]);
            Fluent.At(navs[i], 12, 12 + i * 44);
            navs[i].Click += delegate { ShowPage(idx); };
            Controls.Add(navs[i]);
        }

        // Pages (same area, one visible at a time)
        pages = new FPage[3];
        for (int i = 0; i < 3; i++)
        {
            pages[i] = new FPage();
            Fluent.Bounds(pages[i], 200, 0, 620, 520);
            Controls.Add(pages[i]);
        }

        BuildGeneralPage(pages[0]);
        BuildWallpaperPage(pages[1]);
        BuildAboutPage(pages[2]);

        // Footer buttons
        btnSave = new FButton("Save and apply");
        Fluent.Bounds(btnSave, 632, 460, 160, 36);
        btnSave.Click += delegate { SaveSettings(); };
        Controls.Add(btnSave);

        btnReset = new FButton("Reset");
        btnReset.Accent = false;
        Fluent.Bounds(btnReset, 522, 460, 100, 36);
        btnReset.Click += delegate { LoadSettings(); LoadToUi(); };
        Controls.Add(btnReset);

        btnSave.BringToFront();
        btnReset.BringToFront();

        LoadToUi();
        ShowPage(0);
    }

    void BuildGeneralPage(FPage page)
    {
        Fluent.Label(page, "General", Fluent.TitleFont, false, false, false, 12, 12, 580, 38);
        lblStatus1 = Fluent.Label(page, "", Fluent.BodyFont, true, false, false, 12, 50, 580, 22);

        FCard c1 = new FCard();
        Fluent.Bounds(c1, 12, 84, 580, 112);
        c1.DividerYs = new int[] { 56 };
        page.Controls.Add(c1);

        Fluent.Label(c1, "Switch to Light mode at", Fluent.BodyFont, false, true, false, 16, 1, 380, 54);
        tbLight = new FTimeBox();
        Fluent.At(tbLight, 436, 10);
        c1.Controls.Add(tbLight);

        Fluent.Label(c1, "Switch to Dark mode at", Fluent.BodyFont, false, true, false, 16, 57, 380, 54);
        tbDark = new FTimeBox();
        Fluent.At(tbDark, 436, 66);
        c1.Controls.Add(tbDark);

        FCard c2 = new FCard();
        Fluent.Bounds(c2, 12, 210, 580, 112);
        c2.DividerYs = new int[] { 56 };
        page.Controls.Add(c2);

        Fluent.Label(c2, "Enable automatic switching", Fluent.BodyFont, false, true, false, 16, 1, 480, 54);
        tgEnabled = new FToggle();
        Fluent.At(tgEnabled, 520, 17);
        c2.Controls.Add(tgEnabled);

        Fluent.Label(c2, "Start with Windows", Fluent.BodyFont, false, true, false, 16, 57, 480, 54);
        tgStartup = new FToggle();
        Fluent.At(tgStartup, 520, 73);
        c2.Controls.Add(tgStartup);

        Fluent.Label(page,
            "Closing this window keeps the app running in the system tray (next to the clock). " +
            "To quit completely, right-click the tray icon and choose Exit.",
            Fluent.SmallFont, true, false, true, 12, 336, 580, 40);
    }

    void BuildWallpaperPage(FPage page)
    {
        Fluent.Label(page, "Wallpaper", Fluent.TitleFont, false, false, false, 12, 12, 580, 38);
        lblStatus2 = Fluent.Label(page, "", Fluent.BodyFont, true, false, false, 12, 50, 580, 22);

        FCard c1 = new FCard();
        Fluent.Bounds(c1, 12, 84, 580, 72);
        page.Controls.Add(c1);

        Fluent.Label(c1, "Change wallpaper with the theme", Fluent.BodyFont, false, true, false, 16, 10, 480, 26);
        Fluent.Label(c1, "Applied at the same moment Windows switches", Fluent.SmallFont, true, true, false, 16, 36, 480, 22);
        tgWallpaper = new FToggle();
        Fluent.At(tgWallpaper, 520, 24);
        tgWallpaper.CheckedChanged += delegate { UpdateWallpaperUi(); };
        c1.Controls.Add(tgWallpaper);

        tileLight = new FWallTile("Light wallpaper", true);
        Fluent.At(tileLight, 12, 170);
        tileLight.Browse += delegate { PickWallpaper(true); };
        page.Controls.Add(tileLight);

        tileDark = new FWallTile("Dark wallpaper", false);
        Fluent.At(tileDark, 309, 170);
        tileDark.Browse += delegate { PickWallpaper(false); };
        page.Controls.Add(tileDark);

        FCard c2 = new FCard();
        Fluent.Bounds(c2, 12, 374, 580, 68);
        page.Controls.Add(c2);

        Fluent.Label(c2, "Picture fit", Fluent.BodyFont, false, true, false, 16, 1, 200, 66);
        segFit = new FSegmented(new string[] { "Fill", "Fit", "Stretch", "Center", "Tile" });
        Fluent.At(segFit, 234, 16);
        c2.Controls.Add(segFit);
    }

    void BuildAboutPage(FPage page)
    {
        Fluent.Label(page, "About", Fluent.TitleFont, false, false, false, 12, 12, 580, 38);
        Fluent.Label(page, "Auto Theme Switcher " + AppVersion, Fluent.BodyFont, true, false, false, 12, 50, 580, 22);

        FCard c = new FCard();
        Fluent.Bounds(c, 12, 84, 580, 168);
        c.DividerYs = new int[] { 56, 112 };
        page.Controls.Add(c);

        AboutRow(c, 0, "Version", AppVersion);
        AboutRow(c, 1, "Installed in", Path.GetDirectoryName(Application.ExecutablePath));
        AboutRow(c, 2, "Settings stored in", "Windows registry (current user only)");

        Fluent.Label(page,
            "Auto Theme Switcher switches Windows between Light and Dark mode at the times you choose, " +
            "and can change your wallpaper at the same time. It runs entirely on your PC: " +
            "no internet access and no data is collected.",
            Fluent.BodyFont, true, false, true, 12, 268, 580, 80);
    }

    void AboutRow(FCard card, int row, string label, string value)
    {
        int y = row * 56 + 1;
        Fluent.Label(card, label, Fluent.BodyFont, false, true, false, 16, y, 160, 54);
        FText v = Fluent.Label(card, value, Fluent.SmallFont, true, true, false, 190, y, 374, 54);
        v.AlignRight = true;
    }

    void ShowPage(int i)
    {
        for (int k = 0; k < pages.Length; k++)
        {
            pages[k].Visible = (k == i);
            navs[k].Selected = (k == i);
            navs[k].Invalidate();
        }
        btnSave.Visible = i < 2;
        btnReset.Visible = i < 2;
    }

    void LoadToUi()
    {
        tbLight.Value = lightStart;
        tbDark.Value = darkStart;
        tgEnabled.Checked = enabled;
        tgStartup.Checked = configured ? IsStartupEnabled() : true;
        tgWallpaper.Checked = wpEnabled;
        segFit.SelectedIndex = wpFit;
        RefreshTiles();
        UpdateWallpaperUi();
        UpdateStatus();
    }

    void RefreshTiles()
    {
        tileLight.SetImage(lightWp);
        tileLight.FileName = lightWpName;
        tileDark.SetImage(darkWp);
        tileDark.FileName = darkWpName;
        tileLight.Invalidate();
        tileDark.Invalidate();
    }

    void UpdateWallpaperUi()
    {
        bool on = tgWallpaper.Checked;
        tileLight.Enabled = on;
        tileDark.Enabled = on;
        segFit.Enabled = on;
        UpdateStatus();
    }

    void PickWallpaper(bool light)
    {
        using (OpenFileDialog dlg = new OpenFileDialog())
        {
            dlg.Title = light ? "Choose the Light wallpaper" : "Choose the Dark wallpaper";
            dlg.Filter = "Images (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";
            if (dlg.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                Directory.CreateDirectory(WallDir);
                string ext = Path.GetExtension(dlg.FileName).ToLowerInvariant();
                string dest = Path.Combine(WallDir, (light ? "light-" : "dark-") + DateTime.Now.Ticks + ext);
                File.Copy(dlg.FileName, dest, true);

                Bitmap test = Fluent.LoadThumb(dest, 16, 16);
                if (test == null)
                {
                    try { File.Delete(dest); } catch (Exception) { }
                    throw new InvalidOperationException("That file is not a valid image.");
                }
                test.Dispose();

                if (light) { lightWp = dest; lightWpName = Path.GetFileName(dlg.FileName); }
                else { darkWp = dest; darkWpName = Path.GetFileName(dlg.FileName); }
                RefreshTiles();
                UpdateStatus();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Could not use that image:\n" + ex.Message, "Auto Theme Switcher",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    string StatusText()
    {
        bool light = IsLightNow();
        string s = "Windows is in " + (light ? "Light" : "Dark") + " mode.";
        if (!enabled) return s + " Automatic switching is off.";

        bool schedLight = ShouldBeLight(DateTime.Now.TimeOfDay);
        TimeSpan next = schedLight ? darkStart : lightStart;
        return s + " Switching to " + (schedLight ? "Dark" : "Light") + " at " + next.ToString(@"hh\:mm") + ".";
    }

    void UpdateStatus()
    {
        if (lblStatus1 == null || tileLight == null) return;

        string s = StatusText();
        lblStatus1.Text = s;
        lblStatus2.Text = s;

        bool light = IsLightNow();
        tileLight.Active = tgWallpaper.Checked && light && HasImage(lightWp);
        tileDark.Active = tgWallpaper.Checked && !light && HasImage(darkWp);
        tileLight.Invalidate();
        tileDark.Invalidate();
    }

    // ---------------------------------------------------------------
    // Tray icon
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
        LoadSettings();
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

    // If the user opens the app again, the copy in the tray shows its window
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
    // Window behavior
    // ---------------------------------------------------------------

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Fluent.StyleTitleBar(Handle);
    }

    // With --minimized the window is never shown
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
    // Icon (half white, half black)
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
