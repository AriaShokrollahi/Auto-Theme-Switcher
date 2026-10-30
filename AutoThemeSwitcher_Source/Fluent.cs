// Fluent.cs
// Windows 11 (Fluent Design) style colors and controls shared by the app and the installer.
// Compatible with C# 5 / .NET Framework 4 (the compiler that ships with Windows).
//
// All sizes in this file are "design units" (96 DPI). They are multiplied by Fluent.Scale
// so the UI stays sharp on high-DPI screens (125%, 150%, ...).

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

static class Fluent
{
    public static bool Dark;
    public static Color Window, Card, CardBorder, Divider, Text, TextSecondary, Subtle;
    public static Color Accent, AccentHover, AccentPressed, AccentText;
    public static Color Fill, FillHover, FillPressed, FillBorder, ToggleBorder, ToggleKnob;

    public static float Scale = 1f;

    public static readonly Font BodyFont = new Font("Segoe UI", 10f);
    public static readonly Font SemiFont = new Font("Segoe UI Semibold", 10f);
    public static readonly Font SmallFont = new Font("Segoe UI", 9f);
    public static readonly Font TitleFont = new Font("Segoe UI Semibold", 18f);

    [DllImport("dwmapi.dll")]
    static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int value, int size);

    [DllImport("user32.dll")]
    static extern bool SetProcessDPIAware();

    // Must be called first thing in Main(), before any window is created.
    public static void InitDpi()
    {
        try { SetProcessDPIAware(); } catch (Exception) { }
        try
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero)) Scale = g.DpiX / 96f;
        }
        catch (Exception) { Scale = 1f; }
        if (Scale < 1f) Scale = 1f;
    }

    public static int S(int v) { return (int)Math.Round(v * Scale); }
    public static float SF(float v) { return v * Scale; }

    public static void Bounds(Control c, int x, int y, int w, int h) { c.SetBounds(S(x), S(y), S(w), S(h)); }
    public static void At(Control c, int x, int y) { c.Location = new Point(S(x), S(y)); }

    public static bool SystemIsLight()
    {
        object v = Registry.GetValue(
            @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
            "AppsUseLightTheme", 1);
        return !(v is int) || (int)v != 0;
    }

    static Color SystemAccent()
    {
        try
        {
            object v = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", null);
            if (v is int)
            {
                int a = (int)v;   // ABGR
                return Color.FromArgb(a & 0xFF, (a >> 8) & 0xFF, (a >> 16) & 0xFF);
            }
        }
        catch (Exception) { }
        return Color.FromArgb(0, 103, 192);
    }

    static double Luma(Color c) { return 0.299 * c.R + 0.587 * c.G + 0.114 * c.B; }

    public static Color Mix(Color a, Color b, float t)
    {
        if (t < 0f) t = 0f;
        if (t > 1f) t = 1f;
        return Color.FromArgb(255,
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t));
    }

    // Sets the palette from the current Windows theme (light/dark) and the system accent color.
    public static void Update(bool light)
    {
        Dark = !light;
        Color acc = SystemAccent();

        if (light)
        {
            Window = Color.FromArgb(243, 243, 243);
            Card = Color.FromArgb(255, 255, 255);
            CardBorder = Color.FromArgb(229, 229, 229);
            Divider = Color.FromArgb(235, 235, 235);
            Text = Color.FromArgb(26, 26, 26);
            TextSecondary = Color.FromArgb(96, 96, 96);
            Subtle = Color.FromArgb(230, 230, 230);
            Fill = Color.FromArgb(251, 251, 251);
            FillHover = Color.FromArgb(246, 246, 246);
            FillPressed = Color.FromArgb(240, 240, 240);
            FillBorder = Color.FromArgb(222, 222, 222);
            ToggleBorder = Color.FromArgb(137, 137, 137);
            ToggleKnob = Color.FromArgb(95, 95, 95);
            Accent = acc;
            AccentHover = Mix(acc, Color.Black, 0.12f);
            AccentPressed = Mix(acc, Color.Black, 0.22f);
        }
        else
        {
            Window = Color.FromArgb(32, 32, 32);
            Card = Color.FromArgb(43, 43, 43);
            CardBorder = Color.FromArgb(58, 58, 58);
            Divider = Color.FromArgb(56, 56, 56);
            Text = Color.White;
            TextSecondary = Color.FromArgb(197, 197, 197);
            Subtle = Color.FromArgb(54, 54, 54);
            Fill = Color.FromArgb(50, 50, 50);
            FillHover = Color.FromArgb(58, 58, 58);
            FillPressed = Color.FromArgb(44, 44, 44);
            FillBorder = Color.FromArgb(69, 69, 69);
            ToggleBorder = Color.FromArgb(160, 160, 160);
            ToggleKnob = Color.FromArgb(207, 207, 207);
            Accent = Luma(acc) < 110 ? Mix(acc, Color.White, 0.35f) : acc;
            AccentHover = Mix(Accent, Color.White, 0.12f);
            AccentPressed = Mix(Accent, Color.White, 0.22f);
        }

        AccentText = Luma(Accent) > 150 ? Color.Black : Color.White;
    }

    // Makes the window title bar follow the theme (Windows 10 / 11).
    public static void StyleTitleBar(IntPtr hwnd)
    {
        try
        {
            int dark = Dark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, 20, ref dark, 4) != 0)
                DwmSetWindowAttribute(hwnd, 19, ref dark, 4);

            int caption = Window.R | (Window.G << 8) | (Window.B << 16);   // Windows 11 only
            DwmSetWindowAttribute(hwnd, 35, ref caption, 4);
        }
        catch (Exception) { }
    }

    public static GraphicsPath Round(RectangleF r, float radius)
    {
        float d = radius * 2f;
        GraphicsPath p = new GraphicsPath();
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }

    // Adds a text label. x, y, w, h are in design units.
    public static FText Label(Control parent, string text, Font font, bool secondary, bool onCard,
                              bool wrap, int x, int y, int w, int h)
    {
        FText t = new FText(font, secondary, onCard, wrap);
        t.Text = text;
        Bounds(t, x, y, w, h);
        parent.Controls.Add(t);
        return t;
    }

    // Loads an image file into a w x h thumbnail (cropped to fill). Returns null if it fails.
    // The file is not kept open or locked.
    public static Bitmap LoadThumb(string path, int w, int h)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (Image src = Image.FromStream(fs, false, false))
            {
                Bitmap bmp = new Bitmap(w, h);
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    float s = Math.Max((float)w / src.Width, (float)h / src.Height);
                    float sw = src.Width * s;
                    float sh = src.Height * s;
                    g.DrawImage(src, (w - sw) / 2f, (h - sh) / 2f, sw, sh);
                }
                return bmp;
            }
        }
        catch (Exception) { return null; }
    }
}

// Text label
class FText : Control
{
    public bool Secondary, OnCard, Wrap, AlignRight;

    public FText(Font font, bool secondary, bool onCard, bool wrap)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        Font = font;
        Secondary = secondary;
        OnCard = onCard;
        Wrap = wrap;
    }

    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(OnCard ? Fluent.Card : Fluent.Window);
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using (StringFormat sf = new StringFormat())
        using (SolidBrush br = new SolidBrush(Secondary ? Fluent.TextSecondary : Fluent.Text))
        {
            sf.Alignment = AlignRight ? StringAlignment.Far : StringAlignment.Near;
            if (Wrap)
            {
                sf.LineAlignment = StringAlignment.Near;
            }
            else
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags |= StringFormatFlags.NoWrap;
                sf.Trimming = StringTrimming.EllipsisCharacter;
            }
            g.DrawString(Text, Font, br, new RectangleF(0, 0, Width, Height), sf);
        }
    }
}

// Page container (paints the window background)
class FPage : Control
{
    public FPage()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.Clear(Fluent.Window);
    }
}

// Rounded card (like the cards in Windows 11 Settings)
class FCard : Control
{
    public int[] DividerYs = new int[0];   // design units

    public FCard()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath p = Fluent.Round(r, Fluent.SF(8f)))
        using (SolidBrush b = new SolidBrush(Fluent.Card))
        using (Pen pen = new Pen(Fluent.CardBorder))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }

        g.SmoothingMode = SmoothingMode.None;
        using (Pen d = new Pen(Fluent.Divider))
        {
            foreach (int y in DividerYs) g.DrawLine(d, 1, Fluent.S(y), Width - 2, Fluent.S(y));
        }
    }
}

// Base for controls with a smooth hover animation
abstract class FBase : Control
{
    protected float HoverT;     // 0 = idle, 1 = hovered (animated)
    protected bool Hover;
    System.Windows.Forms.Timer anim;

    protected FBase()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
    }

    void StartAnim()
    {
        if (anim == null)
        {
            anim = new System.Windows.Forms.Timer();
            anim.Interval = 15;
            anim.Tick += delegate { StepAnim(); };
        }
        anim.Start();
    }

    void StepAnim()
    {
        float target = Hover ? 1f : 0f;
        float d = target - HoverT;
        if (Math.Abs(d) < 0.03f) { HoverT = target; anim.Stop(); }
        else HoverT += d * 0.35f;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); Hover = true; StartAnim(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); Hover = false; StartAnim(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing && anim != null) { anim.Dispose(); anim = null; }
        base.Dispose(disposing);
    }
}

// On/Off toggle switch (the knob slides)
class FToggle : FBase
{
    bool isChecked;
    float pos;      // 0 = off, 1 = on (animated)
    System.Windows.Forms.Timer slide;
    public bool Mirror = false;   // set to true for right-to-left layouts
    public event EventHandler CheckedChanged;

    public FToggle()
    {
        Size = new Size(Fluent.S(44), Fluent.S(22));
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public bool Checked
    {
        get { return isChecked; }
        set
        {
            if (isChecked == value) return;
            isChecked = value;
            StartSlide();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }
    }

    void StartSlide()
    {
        if (slide == null)
        {
            slide = new System.Windows.Forms.Timer();
            slide.Interval = 15;
            slide.Tick += delegate { StepSlide(); };
        }
        slide.Start();
    }

    void StepSlide()
    {
        float target = isChecked ? 1f : 0f;
        float d = target - pos;
        if (Math.Abs(d) < 0.03f) { pos = target; slide.Stop(); }
        else pos += d * 0.4f;
        Invalidate();
    }

    protected override void OnClick(EventArgs e) { base.OnClick(e); Checked = !Checked; }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && slide != null) { slide.Dispose(); slide = null; }
        base.Dispose(disposing);
    }

    // color with alpha scaled by k (and dimmed when the control is disabled)
    Color A(Color c, float k)
    {
        float f = k * (Enabled ? 1f : 0.43f);
        int a = (int)(c.A * f);
        if (a < 0) a = 0;
        if (a > 255) a = 255;
        return Color.FromArgb(a, c);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Card);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        RectangleF tr = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath p = Fluent.Round(tr, (Height - 1f) / 2f))
        {
            if (pos > 0.01f)
            {
                using (SolidBrush b = new SolidBrush(A(Fluent.Mix(Fluent.Accent, Fluent.AccentHover, HoverT), pos)))
                {
                    g.FillPath(b, p);
                }
            }
            if (pos < 0.99f)
            {
                using (Pen pen = new Pen(A(Fluent.ToggleBorder, 1f - pos)))
                {
                    g.DrawPath(pen, p);
                }
            }
        }

        float d = Fluent.SF(12f + 2f * HoverT);
        float margin = Fluent.SF(5f);
        float xLeft = margin;
        float xRight = Width - margin - d;
        float xOn = Mirror ? xLeft : xRight;
        float xOff = Mirror ? xRight : xLeft;
        float x = xOff + (xOn - xOff) * pos;
        float y = (Height - d) / 2f;
        using (SolidBrush kb = new SolidBrush(A(Fluent.Mix(Fluent.ToggleKnob, Fluent.AccentText, pos), 1f)))
        {
            g.FillEllipse(kb, x, y, d, d);
        }

        if (Focused && ShowFocusCues)
        {
            using (GraphicsPath fp = Fluent.Round(new RectangleF(1f, 1f, Width - 2f, Height - 2f), (Height - 2f) / 2f))
            using (Pen fpen = new Pen(Fluent.Text, Fluent.SF(2f)))
            {
                g.DrawPath(fpen, fp);
            }
        }
    }
}

// Button (accent-colored primary, or plain)
class FButton : FBase
{
    public bool Accent = true;
    public bool OnCard;
    bool down;

    public FButton(string text)
    {
        Font = Fluent.BodyFont;
        Text = text;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(Fluent.S(120), Fluent.S(36));
    }

    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); down = false; Invalidate(); }
    protected override void OnMouseUp(MouseEventArgs e) { base.OnMouseUp(e); down = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }
    protected override void OnTextChanged(EventArgs e) { base.OnTextChanged(e); Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button == MouseButtons.Left) { down = true; Invalidate(); }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(OnCard ? Fluent.Card : Fluent.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Color fill, textColor;
        bool border = false;
        if (!Enabled)
        {
            fill = Fluent.Dark ? Color.FromArgb(50, 50, 50) : Color.FromArgb(240, 240, 240);
            textColor = Fluent.Dark ? Color.FromArgb(120, 120, 120) : Color.FromArgb(160, 160, 160);
        }
        else if (Accent)
        {
            fill = down ? Fluent.AccentPressed : Fluent.Mix(Fluent.Accent, Fluent.AccentHover, HoverT);
            textColor = Fluent.AccentText;
        }
        else
        {
            fill = down ? Fluent.FillPressed : Fluent.Mix(Fluent.Fill, Fluent.FillHover, HoverT);
            textColor = Fluent.Text;
            border = true;
        }

        RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath p = Fluent.Round(r, Fluent.SF(4f)))
        using (SolidBrush b = new SolidBrush(fill))
        {
            g.FillPath(b, p);
            if (border)
            {
                using (Pen pen = new Pen(Fluent.FillBorder)) g.DrawPath(pen, p);
            }
        }

        if (Focused && ShowFocusCues)
        {
            float inset = Fluent.SF(2f);
            using (GraphicsPath fp = Fluent.Round(new RectangleF(inset, inset, Width - 2f * inset, Height - 2f * inset), Fluent.SF(3f)))
            using (Pen fpen = new Pen(Fluent.Dark ? Color.White : Color.Black, 1.5f))
            {
                g.DrawPath(fpen, fp);
            }
        }

        using (StringFormat sf = new StringFormat())
        using (SolidBrush tb = new SolidBrush(textColor))
        {
            sf.FormatFlags = StringFormatFlags.NoWrap;
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            g.DrawString(Text, Font, tb, new RectangleF(0, 0, Width, Height), sf);
        }
    }
}

// Sidebar navigation item
class FNavItem : FBase
{
    public bool Selected;

    public FNavItem(string text)
    {
        Text = text;
        Font = Fluent.BodyFont;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(Fluent.S(176), Fluent.S(40));
    }

    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            OnClick(EventArgs.Empty);
            e.Handled = true;
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        Color bg = Selected ? Fluent.Subtle : Fluent.Mix(Fluent.Window, Fluent.Subtle, HoverT * 0.6f);
        using (GraphicsPath p = Fluent.Round(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Fluent.SF(4f)))
        using (SolidBrush b = new SolidBrush(bg))
        {
            g.FillPath(b, p);
        }

        if (Selected)
        {
            float pw = Fluent.SF(3f);
            float ph = Fluent.SF(16f);
            RectangleF pr = new RectangleF(Fluent.SF(4f), (Height - ph) / 2f, pw, ph);
            using (GraphicsPath pp = Fluent.Round(pr, pw / 2f))
            using (SolidBrush ab = new SolidBrush(Fluent.Accent))
            {
                g.FillPath(ab, pp);
            }
        }

        if (Focused && ShowFocusCues)
        {
            using (GraphicsPath fp = Fluent.Round(new RectangleF(1f, 1f, Width - 2f, Height - 2f), Fluent.SF(4f)))
            using (Pen fpen = new Pen(Fluent.Text, 1.5f))
            {
                g.DrawPath(fpen, fp);
            }
        }

        using (StringFormat sf = new StringFormat())
        using (SolidBrush tb = new SolidBrush(Fluent.Text))
        {
            sf.Alignment = StringAlignment.Near;
            sf.LineAlignment = StringAlignment.Center;
            sf.FormatFlags = StringFormatFlags.NoWrap;
            g.DrawString(Text, Selected ? Fluent.SemiFont : Fluent.BodyFont, tb,
                new RectangleF(Fluent.SF(16f), 0, Width - Fluent.SF(16f), Height), sf);
        }
    }
}

// Segmented control (one option selected at a time)
class FSegmented : FBase
{
    string[] items;
    int selected;
    public event EventHandler SelectedChanged;

    public FSegmented(string[] items)
    {
        this.items = items;
        Font = Fluent.SmallFont;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(Fluent.S(330), Fluent.S(36));
    }

    public int SelectedIndex
    {
        get { return selected; }
        set
        {
            if (value < 0 || value >= items.Length || value == selected) return;
            selected = value;
            Invalidate();
            if (SelectedChanged != null) SelectedChanged(this, EventArgs.Empty);
        }
    }

    RectangleF SegRect(int i)
    {
        float pad = Fluent.SF(2f);
        float gap = Fluent.SF(2f);
        float w = (Width - 2f * pad - gap * (items.Length - 1)) / items.Length;
        return new RectangleF(pad + i * (w + gap), pad, w, Height - 2f * pad);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        for (int i = 0; i < items.Length; i++)
        {
            if (SegRect(i).Contains(e.X, e.Y))
            {
                SelectedIndex = i;
                Focus();
                break;
            }
        }
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys k = keyData & Keys.KeyCode;
        if (k == Keys.Left || k == Keys.Right) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Left) { SelectedIndex = selected - 1; e.Handled = true; }
        else if (e.KeyCode == Keys.Right) { SelectedIndex = selected + 1; e.Handled = true; }
    }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Card);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        using (GraphicsPath cp = Fluent.Round(new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f), Fluent.SF(6f)))
        using (SolidBrush cb = new SolidBrush(Fluent.Subtle))
        {
            g.FillPath(cb, cp);
        }

        using (StringFormat sf = new StringFormat())
        {
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            sf.FormatFlags = StringFormatFlags.NoWrap;

            for (int i = 0; i < items.Length; i++)
            {
                RectangleF r = SegRect(i);
                bool on = i == selected;
                if (on)
                {
                    using (GraphicsPath sp = Fluent.Round(r, Fluent.SF(4f)))
                    using (SolidBrush sb = new SolidBrush(Fluent.Accent))
                    {
                        g.FillPath(sb, sp);
                    }
                }
                using (SolidBrush tb = new SolidBrush(on ? Fluent.AccentText : Fluent.Text))
                {
                    g.DrawString(items[i], on ? Fluent.SemiFont : Font, tb, r, sf);
                }
            }
        }

        if (!Enabled)
        {
            using (SolidBrush ob = new SolidBrush(Color.FromArgb(150, Fluent.Card)))
            {
                g.FillRectangle(ob, 0, 0, Width, Height);
            }
        }
    }
}

// Wallpaper tile: image preview, title, file name and a Browse button
class FWallTile : Control
{
    public string Title, FileName;
    public bool Active;
    bool lightStyle;
    Bitmap thumb;
    FButton browseButton;
    public event EventHandler Browse;

    public FWallTile(string title, bool lightStyle)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        SetStyle(ControlStyles.Selectable, false);
        TabStop = false;
        Title = title;
        this.lightStyle = lightStyle;

        browseButton = new FButton("Browse");
        browseButton.Accent = false;
        browseButton.OnCard = true;
        browseButton.Size = new Size(Fluent.S(84), Fluent.S(32));
        browseButton.Click += delegate { RaiseBrowse(); };
        Controls.Add(browseButton);

        Size = new Size(Fluent.S(283), Fluent.S(188));
    }

    void RaiseBrowse()
    {
        if (Browse != null) Browse(this, EventArgs.Empty);
    }

    Rectangle PreviewRect
    {
        get { return new Rectangle(Fluent.S(10), Fluent.S(10), Width - 2 * Fluent.S(10), Fluent.S(120)); }
    }

    public void SetImage(string path)
    {
        if (thumb != null) { thumb.Dispose(); thumb = null; }
        Rectangle pv = PreviewRect;
        if (pv.Width > 0 && pv.Height > 0) thumb = Fluent.LoadThumb(path, pv.Width, pv.Height);
        Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        browseButton.Location = new Point(Width - Fluent.S(10) - browseButton.Width,
                                          Height - Fluent.S(10) - browseButton.Height);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (Enabled && PreviewRect.Contains(e.Location)) RaiseBrowse();
    }

    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void Dispose(bool disposing)
    {
        if (disposing && thumb != null) { thumb.Dispose(); thumb = null; }
        base.Dispose(disposing);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Window);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath p = Fluent.Round(r, Fluent.SF(8f)))
        using (SolidBrush cb = new SolidBrush(Fluent.Card))
        using (Pen bp = new Pen(Active ? Fluent.Accent : Fluent.CardBorder, Active ? Fluent.SF(2f) : 1f))
        {
            g.FillPath(cb, p);
            g.DrawPath(bp, p);
        }

        Rectangle pv = PreviewRect;
        using (GraphicsPath pp = Fluent.Round(new RectangleF(pv.X, pv.Y, pv.Width, pv.Height), Fluent.SF(6f)))
        {
            g.SetClip(pp);
            if (thumb != null)
            {
                g.DrawImage(thumb, pv);
            }
            else
            {
                Color c1 = lightStyle ? Color.FromArgb(207, 227, 255) : Color.FromArgb(10, 15, 31);
                Color c2 = lightStyle ? Color.FromArgb(243, 209, 255) : Color.FromArgb(74, 35, 128);
                using (LinearGradientBrush lb = new LinearGradientBrush(pv, c1, c2, 45f))
                {
                    g.FillRectangle(lb, pv);
                }
                using (StringFormat cf = new StringFormat())
                using (SolidBrush ph = new SolidBrush(lightStyle ? Color.FromArgb(50, 70, 110) : Color.FromArgb(215, 215, 230)))
                {
                    cf.Alignment = StringAlignment.Center;
                    cf.LineAlignment = StringAlignment.Center;
                    g.DrawString("No image selected", Fluent.SmallFont, ph,
                        new RectangleF(pv.X, pv.Y, pv.Width, pv.Height - Fluent.S(10)), cf);
                }
            }
            using (SolidBrush tb = new SolidBrush(Color.FromArgb(90, 0, 0, 0)))
            {
                g.FillRectangle(tb, pv.X, pv.Bottom - Fluent.S(10), pv.Width, Fluent.S(10));
            }
            g.ResetClip();
        }

        if (Active)
        {
            SizeF sz = g.MeasureString("In use now", Fluent.SmallFont);
            RectangleF pr = new RectangleF(pv.X + Fluent.SF(8f), pv.Y + Fluent.SF(8f), sz.Width + Fluent.SF(6f), sz.Height + Fluent.SF(2f));
            using (GraphicsPath pillPath = Fluent.Round(pr, pr.Height / 2f))
            using (SolidBrush pb = new SolidBrush(Fluent.Accent))
            using (SolidBrush pt = new SolidBrush(Fluent.AccentText))
            using (StringFormat pf = new StringFormat())
            {
                pf.Alignment = StringAlignment.Center;
                pf.LineAlignment = StringAlignment.Center;
                pf.FormatFlags = StringFormatFlags.NoWrap;
                g.FillPath(pb, pillPath);
                g.DrawString("In use now", Fluent.SmallFont, pt, pr, pf);
            }
        }

        float textW = Width - 2f * Fluent.SF(10f) - browseButton.Width - Fluent.SF(8f);
        using (StringFormat tf = new StringFormat())
        using (SolidBrush t1 = new SolidBrush(Fluent.Text))
        using (SolidBrush t2 = new SolidBrush(Fluent.TextSecondary))
        {
            tf.Alignment = StringAlignment.Near;
            tf.LineAlignment = StringAlignment.Center;
            tf.FormatFlags = StringFormatFlags.NoWrap;
            tf.Trimming = StringTrimming.EllipsisCharacter;
            g.DrawString(Title, Fluent.SemiFont, t1,
                new RectangleF(Fluent.SF(10f), pv.Bottom + Fluent.SF(8f), textW, Fluent.SF(20f)), tf);
            g.DrawString(string.IsNullOrEmpty(FileName) ? "No image selected" : FileName, Fluent.SmallFont, t2,
                new RectangleF(Fluent.SF(10f), pv.Bottom + Fluent.SF(28f), textW, Fluent.SF(18f)), tf);
        }

        if (!Enabled)
        {
            using (GraphicsPath op = Fluent.Round(r, Fluent.SF(8f)))
            using (SolidBrush ob = new SolidBrush(Color.FromArgb(150, Fluent.Window)))
            {
                g.FillPath(ob, op);
            }
        }
    }
}

// Time picker: hour and minute with up/down arrows.
// Mouse: click the hour or minute, then use the arrows or the mouse wheel.
// Keyboard: type digits directly (e.g. 0 7), or use Up/Down/Left/Right.
class FTimeBox : Control
{
    int hour, minute;
    int seg;            // 0 = hour, 1 = minute
    int typedCount, typedValue;
    bool hover;

    public FTimeBox()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = Fluent.BodyFont;
        Size = new Size(Fluent.S(128), Fluent.S(36));
        Cursor = Cursors.Hand;
        TabStop = true;
    }

    public TimeSpan Value
    {
        get { return new TimeSpan(hour, minute, 0); }
        set { hour = value.Hours; minute = value.Minutes; Invalidate(); }
    }

    Rectangle HourRect { get { return new Rectangle(Fluent.S(8), Fluent.S(6), Fluent.S(36), Fluent.S(24)); } }
    Rectangle MinuteRect { get { return new Rectangle(Fluent.S(52), Fluent.S(6), Fluent.S(36), Fluent.S(24)); } }

    void Step(int dir)
    {
        if (seg == 0) hour = (hour + dir + 24) % 24;
        else minute = (minute + dir + 60) % 60;
        typedCount = 0;
        Invalidate();
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); typedCount = 0; Invalidate(); }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        Focus();
        typedCount = 0;
        if (e.X >= Fluent.S(96)) Step(e.Y < Height / 2 ? 1 : -1);
        else seg = e.X < Fluent.S(48) ? 0 : 1;
        Invalidate();
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (e.X < Fluent.S(96)) seg = e.X < Fluent.S(48) ? 0 : 1;
        Step(e.Delta > 0 ? 1 : -1);
    }

    protected override bool IsInputKey(Keys keyData)
    {
        Keys k = keyData & Keys.KeyCode;
        if (k == Keys.Up || k == Keys.Down || k == Keys.Left || k == Keys.Right) return true;
        return base.IsInputKey(keyData);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        switch (e.KeyCode)
        {
            case Keys.Up: Step(1); e.Handled = true; break;
            case Keys.Down: Step(-1); e.Handled = true; break;
            case Keys.Left: seg = 0; typedCount = 0; Invalidate(); e.Handled = true; break;
            case Keys.Right: seg = 1; typedCount = 0; Invalidate(); e.Handled = true; break;
        }
    }

    protected override void OnKeyPress(KeyPressEventArgs e)
    {
        base.OnKeyPress(e);
        if (e.KeyChar < '0' || e.KeyChar > '9') return;

        int d = e.KeyChar - '0';
        int max = seg == 0 ? 23 : 59;
        int cur = typedCount == 1 ? typedValue * 10 + d : d;
        if (cur > max) { cur = d; typedCount = 0; }

        if (seg == 0) hour = cur; else minute = cur;

        if (typedCount == 1 || cur * 10 > max)
        {
            typedCount = 0;
            if (seg == 0) seg = 1;
        }
        else
        {
            typedCount = 1;
            typedValue = cur;
        }
        e.Handled = true;
        Invalidate();
    }

    PointF P(float x, float y) { return new PointF(Fluent.SF(x), Fluent.SF(y)); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Card);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

        RectangleF r = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath p = Fluent.Round(r, Fluent.SF(4f)))
        using (SolidBrush b = new SolidBrush(hover ? Fluent.FillHover : Fluent.Fill))
        using (Pen pen = new Pen(Focused ? Fluent.Accent : Fluent.FillBorder, Focused ? Fluent.SF(2f) : 1f))
        {
            g.FillPath(b, p);
            g.DrawPath(pen, p);
        }

        Rectangle hr = HourRect;
        Rectangle mr = MinuteRect;

        if (Focused)
        {
            Rectangle sel = seg == 0 ? hr : mr;
            using (GraphicsPath sp = Fluent.Round(new RectangleF(sel.X, sel.Y, sel.Width, sel.Height), Fluent.SF(3f)))
            using (SolidBrush sb = new SolidBrush(Fluent.Accent))
            {
                g.FillPath(sb, sp);
            }
        }

        using (StringFormat sf = new StringFormat())
        using (SolidBrush normal = new SolidBrush(Fluent.Text))
        using (SolidBrush onAccent = new SolidBrush(Fluent.AccentText))
        {
            sf.Alignment = StringAlignment.Center;
            sf.LineAlignment = StringAlignment.Center;
            sf.FormatFlags = StringFormatFlags.NoWrap;

            g.DrawString(hour.ToString("00"), Font, (Focused && seg == 0) ? onAccent : normal, hr, sf);
            g.DrawString(minute.ToString("00"), Font, (Focused && seg == 1) ? onAccent : normal, mr, sf);
            g.DrawString(":", Font, normal, new Rectangle(Fluent.S(44), Fluent.S(5), Fluent.S(8), Fluent.S(24)), sf);
        }

        g.SmoothingMode = SmoothingMode.None;
        using (Pen sep = new Pen(Fluent.FillBorder))
        {
            int sx = Fluent.S(95);
            g.DrawLine(sep, sx, Fluent.S(8), sx, Height - Fluent.S(9));
        }

        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (Pen cp = new Pen(Fluent.TextSecondary, Fluent.SF(1.6f)))
        {
            cp.StartCap = LineCap.Round;
            cp.EndCap = LineCap.Round;
            cp.LineJoin = LineJoin.Round;
            g.DrawLines(cp, new PointF[] { P(109f, 14f), P(112f, 11f), P(115f, 14f) });
            g.DrawLines(cp, new PointF[] { P(109f, 22f), P(112f, 25f), P(115f, 22f) });
        }
    }
}
