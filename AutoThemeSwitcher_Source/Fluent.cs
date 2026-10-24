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
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

static class Fluent
{
    public static bool Dark;
    public static Color Window, Card, CardBorder, Divider, Text, TextSecondary;
    public static Color Accent, AccentHover, AccentPressed, AccentText;
    public static Color Fill, FillHover, FillPressed, FillBorder, ToggleBorder, ToggleKnob;

    public static float Scale = 1f;

    public static readonly Font BodyFont = new Font("Segoe UI", 10f);
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

    static Color Blend(Color a, Color b, float t)
    {
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
            Fill = Color.FromArgb(251, 251, 251);
            FillHover = Color.FromArgb(246, 246, 246);
            FillPressed = Color.FromArgb(240, 240, 240);
            FillBorder = Color.FromArgb(222, 222, 222);
            ToggleBorder = Color.FromArgb(137, 137, 137);
            ToggleKnob = Color.FromArgb(95, 95, 95);
            Accent = acc;
            AccentHover = Blend(acc, Color.Black, 0.12f);
            AccentPressed = Blend(acc, Color.Black, 0.22f);
        }
        else
        {
            Window = Color.FromArgb(32, 32, 32);
            Card = Color.FromArgb(43, 43, 43);
            CardBorder = Color.FromArgb(58, 58, 58);
            Divider = Color.FromArgb(56, 56, 56);
            Text = Color.White;
            TextSecondary = Color.FromArgb(197, 197, 197);
            Fill = Color.FromArgb(50, 50, 50);
            FillHover = Color.FromArgb(58, 58, 58);
            FillPressed = Color.FromArgb(44, 44, 44);
            FillBorder = Color.FromArgb(69, 69, 69);
            ToggleBorder = Color.FromArgb(160, 160, 160);
            ToggleKnob = Color.FromArgb(207, 207, 207);
            Accent = Luma(acc) < 110 ? Blend(acc, Color.White, 0.35f) : acc;
            AccentHover = Blend(Accent, Color.White, 0.12f);
            AccentPressed = Blend(Accent, Color.White, 0.22f);
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
}

// Text label
class FText : Control
{
    public bool Secondary, OnCard, Wrap;

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
            sf.Alignment = StringAlignment.Near;
            if (Wrap)
            {
                sf.LineAlignment = StringAlignment.Near;
            }
            else
            {
                sf.LineAlignment = StringAlignment.Center;
                sf.FormatFlags |= StringFormatFlags.NoWrap;
            }
            g.DrawString(Text, Font, br, new RectangleF(0, 0, Width, Height), sf);
        }
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

// On/Off toggle switch
class FToggle : Control
{
    bool isChecked, hover;
    public bool Mirror = false;   // set to true for right-to-left layouts
    public event EventHandler CheckedChanged;

    public FToggle()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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
            Invalidate();
            if (CheckedChanged != null) CheckedChanged(this, EventArgs.Empty);
        }
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
    protected override void OnClick(EventArgs e) { base.OnClick(e); Checked = !Checked; }
    protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
    protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
    protected override void OnEnabledChanged(EventArgs e) { base.OnEnabledChanged(e); Invalidate(); }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Space) { Checked = !Checked; e.Handled = true; }
    }

    Color Dim(Color c) { return Enabled ? c : Color.FromArgb(110, c); }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.Clear(Fluent.Card);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        RectangleF tr = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (GraphicsPath p = Fluent.Round(tr, (Height - 1f) / 2f))
        {
            if (isChecked)
            {
                using (SolidBrush b = new SolidBrush(Dim(hover ? Fluent.AccentHover : Fluent.Accent)))
                {
                    g.FillPath(b, p);
                }
            }
            else
            {
                using (Pen pen = new Pen(Dim(Fluent.ToggleBorder)))
                {
                    g.DrawPath(pen, p);
                }
            }
        }

        float d = hover ? Fluent.SF(14f) : Fluent.SF(12f);
        float margin = Fluent.SF(5f);
        bool knobRight = Mirror ? !isChecked : isChecked;
        float x = knobRight ? Width - margin - d : margin;
        float y = (Height - d) / 2f;
        using (SolidBrush kb = new SolidBrush(Dim(isChecked ? Fluent.AccentText : Fluent.ToggleKnob)))
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
class FButton : Control
{
    public bool Accent = true;
    public bool OnCard;
    bool hover, down;

    public FButton(string text)
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        Font = Fluent.BodyFont;
        Text = text;
        Cursor = Cursors.Hand;
        TabStop = true;
        Size = new Size(Fluent.S(120), Fluent.S(36));
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; down = false; Invalidate(); }
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
            fill = down ? Fluent.AccentPressed : (hover ? Fluent.AccentHover : Fluent.Accent);
            textColor = Fluent.AccentText;
        }
        else
        {
            fill = down ? Fluent.FillPressed : (hover ? Fluent.FillHover : Fluent.Fill);
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
