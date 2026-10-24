// Installer / Uninstaller for Auto Theme Switcher
// One file that both installs the app and (when named Uninstall.exe) removes it.
// UI: Windows 11 Fluent Design style (see Fluent.cs).
// Compatible with the compiler that ships with Windows (C# 5 / .NET Framework 4).

using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

static class SetupProgram
{
    [STAThread]
    static void Main(string[] args)
    {
        Fluent.InitDpi();   // sharp text on high-DPI screens

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);

        bool isUninstallExe = string.Equals(Path.GetFileName(Application.ExecutablePath),
                                            "Uninstall.exe", StringComparison.OrdinalIgnoreCase);

        if (args.Length > 0 && args[0] == "/uninstall2") { Installer.UninstallStage2(); return; }
        if ((args.Length > 0 && args[0] == "/uninstall") || isUninstallExe) { Installer.UninstallStage1(); return; }

        Application.Run(new InstallForm());
    }
}

static class Installer
{
    public const string AppName = "Auto Theme Switcher";
    public const string Version = "1.0";
    const string ProcessName = "AutoThemeSwitcher";

    const string UninstallRegKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\AutoThemeSwitcher";
    const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    const string RunName = "AutoThemeSwitcher";
    const string AppSettingsKey = @"Software\AutoThemeSwitcher";

    public static readonly string InstallDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        @"Programs\AutoThemeSwitcher");

    public static string AppExe { get { return Path.Combine(InstallDir, "AutoThemeSwitcher.exe"); } }
    public static string UninstallExe { get { return Path.Combine(InstallDir, "Uninstall.exe"); } }

    static string StartMenuLnk
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Programs), "Auto Theme Switcher.lnk"); }
    }

    static string DesktopLnk
    {
        get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Auto Theme Switcher.lnk"); }
    }

    public static bool IsInstalled()
    {
        return File.Exists(AppExe);
    }

    public static bool IsStartupEnabled()
    {
        using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey))
        {
            return k != null && k.GetValue(RunName) != null;
        }
    }

    // ---------------------------------------------------------------
    // نصب
    // ---------------------------------------------------------------

    public static void Install(bool desktopShortcut, bool runAtStartup)
    {
        KillApp();
        Directory.CreateDirectory(InstallDir);

        // 1) فایل برنامه (داخل همین نصب‌کننده جاسازی شده)
        using (Stream s = Assembly.GetExecutingAssembly().GetManifestResourceStream("AppPayload"))
        {
            if (s == null) throw new InvalidOperationException("The app file was not found inside the installer.");
            using (FileStream f = new FileStream(AppExe, FileMode.Create, FileAccess.Write))
            {
                s.CopyTo(f);
            }
        }

        // 2) برنامه حذف‌کننده (کپی همین فایل)
        string self = Path.GetFullPath(Application.ExecutablePath);
        if (!string.Equals(self, Path.GetFullPath(UninstallExe), StringComparison.OrdinalIgnoreCase))
            File.Copy(self, UninstallExe, true);

        // 3) میان‌برها
        CreateShortcut(StartMenuLnk, AppExe, InstallDir, AppName);
        if (desktopShortcut) CreateShortcut(DesktopLnk, AppExe, InstallDir, AppName);
        else if (File.Exists(DesktopLnk)) File.Delete(DesktopLnk);

        // 4) تنظیمات پیش‌فرض (اگر قبلاً ذخیره نشده باشد؛ تنظیمات قبلی کاربر حفظ می‌شود)
        using (RegistryKey probe = Registry.CurrentUser.OpenSubKey(AppSettingsKey))
        {
            if (probe == null)
            {
                using (RegistryKey k = Registry.CurrentUser.CreateSubKey(AppSettingsKey))
                {
                    k.SetValue("LightStart", "07:00");
                    k.SetValue("DarkStart", "19:00");
                    k.SetValue("Enabled", 1, RegistryValueKind.DWord);
                }
            }
        }

        // 5) اجرای خودکار با ویندوز
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(RunKey))
        {
            if (runAtStartup) k.SetValue(RunName, "\"" + AppExe + "\" --minimized");
            else k.DeleteValue(RunName, false);
        }

        // 6) ثبت در «Apps & features» ویندوز
        using (RegistryKey k = Registry.CurrentUser.CreateSubKey(UninstallRegKey))
        {
            k.SetValue("DisplayName", AppName);
            k.SetValue("DisplayVersion", Version);
            k.SetValue("InstallLocation", InstallDir);
            k.SetValue("DisplayIcon", AppExe);
            k.SetValue("UninstallString", "\"" + UninstallExe + "\" /uninstall");
            k.SetValue("InstallDate", DateTime.Now.ToString("yyyyMMdd"));
            k.SetValue("NoModify", 1, RegistryValueKind.DWord);
            k.SetValue("NoRepair", 1, RegistryValueKind.DWord);
        }
    }

    public static void LaunchApp()
    {
        try { Process.Start(AppExe); } catch (Exception) { }
    }

    // ---------------------------------------------------------------
    // حذف
    // ---------------------------------------------------------------

    // مرحله ۱: تأیید از کاربر، سپس اجرای یک کپی موقت (چون فایلی که در حال اجراست نمی‌تواند خودش را پاک کند)
    public static void UninstallStage1()
    {
        DialogResult r = MessageBox.Show(
            "Remove " + AppName + " from this computer?",
            AppName, MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
        if (r != DialogResult.Yes) return;

        string tmp = Path.Combine(Path.GetTempPath(), "ats_uninstall_" + Guid.NewGuid().ToString("N") + ".exe");
        File.Copy(Application.ExecutablePath, tmp, true);
        Process.Start(tmp, "/uninstall2");
    }

    // مرحله ۲: پاک‌سازی واقعی
    public static void UninstallStage2()
    {
        Thread.Sleep(800);   // صبر می‌کنیم مرحله ۱ کاملاً بسته شود
        KillApp();

        try { if (File.Exists(StartMenuLnk)) File.Delete(StartMenuLnk); } catch (Exception) { }
        try { if (File.Exists(DesktopLnk)) File.Delete(DesktopLnk); } catch (Exception) { }

        try
        {
            using (RegistryKey k = Registry.CurrentUser.OpenSubKey(RunKey, true))
            {
                if (k != null) k.DeleteValue(RunName, false);
            }
            Registry.CurrentUser.DeleteSubKeyTree(UninstallRegKey, false);
            Registry.CurrentUser.DeleteSubKeyTree(AppSettingsKey, false);
        }
        catch (Exception) { }

        bool deleted = false;
        for (int i = 0; i < 15 && !deleted; i++)
        {
            try
            {
                if (Directory.Exists(InstallDir)) Directory.Delete(InstallDir, true);
                deleted = true;
            }
            catch (Exception) { Thread.Sleep(300); }
        }

        MessageBox.Show(
            deleted ? AppName + " has been removed." : AppName + " was removed, but some files could not be deleted:\n" + InstallDir,
            AppName, MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);

        // کپی موقت بعد از بسته شدن خودش را پاک می‌کند
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo("cmd.exe",
                "/c ping 127.0.0.1 -n 3 >nul & del /f /q \"" + Application.ExecutablePath + "\"");
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;
            psi.WindowStyle = ProcessWindowStyle.Hidden;
            Process.Start(psi);
        }
        catch (Exception) { }
    }

    // ---------------------------------------------------------------
    // ابزارهای کمکی
    // ---------------------------------------------------------------

    static void KillApp()
    {
        foreach (Process p in Process.GetProcessesByName(ProcessName))
        {
            try { p.Kill(); p.WaitForExit(3000); }
            catch (Exception) { }
        }
    }

    static void CreateShortcut(string lnkPath, string target, string workDir, string description)
    {
        Type shellType = Type.GetTypeFromProgID("WScript.Shell");
        object shell = Activator.CreateInstance(shellType);
        try
        {
            object sc = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { lnkPath });
            try
            {
                Type st = sc.GetType();
                st.InvokeMember("TargetPath", BindingFlags.SetProperty, null, sc, new object[] { target });
                st.InvokeMember("WorkingDirectory", BindingFlags.SetProperty, null, sc, new object[] { workDir });
                st.InvokeMember("Description", BindingFlags.SetProperty, null, sc, new object[] { description });
                st.InvokeMember("IconLocation", BindingFlags.SetProperty, null, sc, new object[] { target + ",0" });
                st.InvokeMember("Save", BindingFlags.InvokeMethod, null, sc, null);
            }
            finally { Marshal.FinalReleaseComObject(sc); }
        }
        finally { Marshal.FinalReleaseComObject(shell); }
    }
}

class InstallForm : Form
{
    FToggle tgDesktop, tgStartup, tgLaunch;
    FButton btnMain, btnCancel;
    FText lblStatus;
    bool done;

    public InstallForm()
    {
        bool upgrade = Installer.IsInstalled();
        Fluent.Update(Fluent.SystemIsLight());

        Text = (upgrade ? "Update " : "Install ") + Installer.AppName;
        Font = Fluent.BodyFont;
        BackColor = Fluent.Window;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(Fluent.S(450), Fluent.S(432));
        try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); } catch (Exception) { }

        Fluent.Label(this, (upgrade ? "Update " : "Install ") + Installer.AppName,
            Fluent.TitleFont, false, false, false, 24, 18, 402, 40);

        Fluent.Label(this,
            "Automatically switches Windows between Light and Dark mode at the times you choose.",
            Fluent.BodyFont, true, false, true, 24, 62, 402, 46);

        Fluent.Label(this, "Install location (no administrator rights needed):",
            Fluent.SmallFont, true, false, false, 24, 114, 402, 20);

        Fluent.Label(this, Installer.InstallDir,
            Fluent.SmallFont, true, false, false, 24, 134, 402, 20);

        FCard card = new FCard();
        Fluent.Bounds(card, 24, 168, 402, 168);
        card.DividerYs = new int[] { 56, 112 };
        Controls.Add(card);

        Fluent.Label(card, "Create a desktop shortcut", Fluent.BodyFont, false, true, false, 16, 1, 310, 54);
        tgDesktop = new FToggle();
        Fluent.At(tgDesktop, 342, 17);
        tgDesktop.Checked = false;
        card.Controls.Add(tgDesktop);

        Fluent.Label(card, "Start with Windows", Fluent.BodyFont, false, true, false, 16, 57, 310, 54);
        tgStartup = new FToggle();
        Fluent.At(tgStartup, 342, 73);
        tgStartup.Checked = upgrade ? Installer.IsStartupEnabled() : true;
        card.Controls.Add(tgStartup);

        Fluent.Label(card, "Launch the app when finished", Fluent.BodyFont, false, true, false, 16, 113, 310, 54);
        tgLaunch = new FToggle();
        Fluent.At(tgLaunch, 342, 129);
        tgLaunch.Checked = true;
        card.Controls.Add(tgLaunch);

        lblStatus = Fluent.Label(this, "", Fluent.BodyFont, false, false, false, 24, 344, 402, 26);

        btnMain = new FButton(upgrade ? "Update" : "Install");
        Fluent.Bounds(btnMain, 276, 382, 150, 36);
        btnMain.Click += delegate { OnMainClick(); };
        Controls.Add(btnMain);

        btnCancel = new FButton("Cancel");
        btnCancel.Accent = false;
        Fluent.Bounds(btnCancel, 156, 382, 110, 36);
        btnCancel.Click += delegate { Close(); };
        Controls.Add(btnCancel);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        Fluent.StyleTitleBar(Handle);
    }

    void OnMainClick()
    {
        if (done)
        {
            if (tgLaunch.Checked) Installer.LaunchApp();
            Close();
            return;
        }

        btnMain.Enabled = false;
        btnCancel.Enabled = false;
        lblStatus.Text = "Installing...";
        Application.DoEvents();

        try
        {
            Installer.Install(tgDesktop.Checked, tgStartup.Checked);
            done = true;
            lblStatus.Text = "Installation complete.";
            tgDesktop.Enabled = false;
            tgStartup.Enabled = false;
            btnMain.Text = "Finish";
            btnMain.Enabled = true;
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Installation failed.";
            MessageBox.Show("Installation failed:\n" + ex.Message, Installer.AppName,
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnMain.Enabled = true;
            btnCancel.Enabled = true;
        }
    }
}
