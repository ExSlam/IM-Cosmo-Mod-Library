using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

namespace IMDataCore.DataMigrationTool.Gui;

internal enum ButtonStyleKind
{
    Secondary,
    Primary,
    Accent,
    Danger
}

internal class CardPanel : Panel
{
    public int CornerRadius { get; set; } = 14;
    public Color FillColor { get; set; } = UiStyling.CardBackground;
    public Color BorderColor { get; set; } = UiStyling.CardBorder;

    public CardPanel()
    {
        SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.SupportsTransparentBackColor, true);
        DoubleBuffered = true;
        BackColor = Color.Transparent;
        Padding = new Padding(16);
        Margin = new Padding(0, 0, 0, 14);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Rectangle rect = new(0, 0, Math.Max(1, Width - 1), Math.Max(1, Height - 1));
        using GraphicsPath path = UiStyling.CreateRoundedPath(rect, CornerRadius);
        using SolidBrush brush = new(FillColor);
        using Pen pen = new(BorderColor);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }
}

internal sealed class ResponsiveBannerPanel : CardPanel
{
    public Image? BannerImage { get; set; }

    public ResponsiveBannerPanel()
    {
        CornerRadius = 14;
        Padding = new Padding(8);
        TabStop = false;
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        if (BannerImage is null) return;

        Rectangle available = ClientRectangle;
        available.Inflate(-Padding.Left, -Padding.Top);
        if (available.Width <= 0 || available.Height <= 0) return;

        double scale = Math.Min(
            available.Width / (double)BannerImage.Width,
            available.Height / (double)BannerImage.Height);
        int drawWidth = Math.Max(1, (int)Math.Round(BannerImage.Width * scale));
        int drawHeight = Math.Max(1, (int)Math.Round(BannerImage.Height * scale));
        int x = available.Left + (available.Width - drawWidth) / 2;
        int y = available.Top + (available.Height - drawHeight) / 2;

        e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
        e.Graphics.DrawImage(BannerImage, new Rectangle(x, y, drawWidth, drawHeight));
    }
}


internal sealed class ScrollablePopupHost : Panel
{
    private readonly Control _content;
    private readonly Size _minimumContentSize;

    public ScrollablePopupHost(Control content, Size minimumContentSize)
    {
        _content = content;
        _minimumContentSize = minimumContentSize;
        Dock = DockStyle.Fill;
        AutoScroll = true;
        BackColor = UiStyling.PageBackground;
        Padding = new Padding(0);
        Margin = new Padding(0);

        _content.Dock = DockStyle.None;
        _content.Location = Point.Empty;
        _content.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        Controls.Add(_content);

        Layout += (_, _) => UpdateContentBounds();
        Resize += (_, _) => UpdateContentBounds();
        _content.SizeChanged += (_, _) => UpdateScrollExtent();
    }

    private void UpdateContentBounds()
    {
        int verticalAllowance = SystemInformation.VerticalScrollBarWidth + 4;
        int horizontalAllowance = SystemInformation.HorizontalScrollBarHeight + 4;
        int targetWidth = Math.Max(_minimumContentSize.Width, Math.Max(1, ClientSize.Width - verticalAllowance));
        int targetHeight = Math.Max(_minimumContentSize.Height, Math.Max(1, ClientSize.Height - horizontalAllowance));

        Size preferred = _content.GetPreferredSize(new Size(targetWidth, 0));
        targetHeight = Math.Max(targetHeight, preferred.Height);
        _content.Size = new Size(targetWidth, targetHeight);
        UpdateScrollExtent();
    }

    private void UpdateScrollExtent()
    {
        AutoScrollMinSize = new Size(
            Math.Max(_minimumContentSize.Width, _content.Width),
            Math.Max(_minimumContentSize.Height, _content.Height));
    }
}

internal static class UiStyling
{
    public static readonly Color PageBackground = Color.FromArgb(242, 246, 252);
    public static readonly Color CardBackground = Color.FromArgb(250, 252, 255);
    public static readonly Color CardBorder = Color.FromArgb(214, 224, 238);
    public static readonly Color PrimaryText = Color.FromArgb(30, 41, 59);
    public static readonly Color SecondaryText = Color.FromArgb(71, 85, 105);
    public static readonly Color Accent = Color.FromArgb(79, 116, 255);
    public static readonly Color AccentDark = Color.FromArgb(60, 90, 214);
    public static readonly Color AccentLight = Color.FromArgb(232, 238, 255);
    public static readonly Color SurfaceInput = Color.White;
    public static readonly Color SurfaceReadOnly = Color.FromArgb(246, 248, 252);

    private static readonly ConditionalWeakTable<Control, Font> BaseFonts = new();

    public static void ApplyFormTheme(Form form)
    {
        form.BackColor = PageBackground;
        form.ForeColor = PrimaryText;
    }

    public static void FitWindowToWorkingArea(Form form, Size desired, Size minimum, int margin = 48)
    {
        try
        {
            Rectangle work = Screen.FromPoint(Cursor.Position).WorkingArea;
            int availableWidth = Math.Max(640, work.Width - margin);
            int availableHeight = Math.Max(480, work.Height - margin);
            int width = Math.Min(desired.Width, availableWidth);
            int height = Math.Min(desired.Height, availableHeight);
            form.Size = new Size(width, height);
            form.MinimumSize = new Size(Math.Min(minimum.Width, width), Math.Min(minimum.Height, height));
        }
        catch
        {
            form.Size = desired;
            form.MinimumSize = minimum;
        }
    }

    public static void ApplyFontScaling(Control root)
    {
        ApplyFontScalingRecursive(root);
    }

    private static void ApplyFontScalingRecursive(Control control)
    {
        if (!BaseFonts.TryGetValue(control, out Font? baseFont))
        {
            baseFont = control.Font;
            BaseFonts.Add(control, baseFont);
        }

        control.Font = new Font(baseFont.FontFamily, baseFont.Size * UiSettings.FontScale, baseFont.Style, baseFont.Unit);

        foreach (Control child in control.Controls)
        {
            ApplyFontScalingRecursive(child);
        }
    }

    public static void StyleInput(Control control, bool readOnly = false)
    {
        control.BackColor = readOnly ? SurfaceReadOnly : SurfaceInput;
        control.ForeColor = PrimaryText;
        if (control is ComboBox combo)
        {
            combo.FlatStyle = FlatStyle.Flat;
            combo.IntegralHeight = false;
            combo.DropDownHeight = 280;
            combo.DropDownWidth = Math.Max(combo.Width, 280);
        }
        else if (control is TextBoxBase box)
        {
            box.BorderStyle = BorderStyle.FixedSingle;
        }
    }

    public static void StyleButton(Button button, ButtonStyleKind kind)
    {
        int horizontalPadding = Math.Max(10, (int)Math.Round(12 * UiSettings.FontScale));
        int verticalPadding = Math.Max(5, (int)Math.Round(6 * UiSettings.FontScale));
        int minimumHeight = Math.Max(34, TextRenderer.MeasureText("Ag", button.Font).Height + (verticalPadding * 2) + 4);

        // Keep buttons readable at every supported font scale, but otherwise use the
        // normal Windows button appearance. No rounded clipping and no colored fills.
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.MinimumSize = new Size(96, minimumHeight);
        button.MaximumSize = Size.Empty;
        button.Padding = new Padding(horizontalPadding, verticalPadding, horizontalPadding, verticalPadding);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.Cursor = Cursors.Hand;

        // ButtonStyleKind remains part of the call surface so existing forms do not need
        // special cases, but all kinds intentionally render as the same neutral system button.
        _ = kind;
        button.Resize -= ButtonResizeRoundHandler;
        button.Region = null;
        button.FlatStyle = FlatStyle.Standard;
        button.UseVisualStyleBackColor = true;
        button.BackColor = SystemColors.Control;
        button.ForeColor = SystemColors.ControlText;
    }

    private static void ButtonResizeRoundHandler(object? sender, EventArgs e)
    {
        // Kept only so StyleButton can detach handlers that may have been attached by an
        // older styling pass. Buttons are deliberately rectangular now.
    }

    public static void StyleCard(CardPanel card)
    {
        card.FillColor = CardBackground;
        card.BorderColor = CardBorder;
        card.ForeColor = PrimaryText;
    }

    public static void ApplyRoundedRegion(Control control, int radius)
    {
        Rectangle rect = new(0, 0, Math.Max(1, control.Width), Math.Max(1, control.Height));
        using GraphicsPath path = CreateRoundedPath(rect, radius);
        control.Region = new Region(path);
    }

    public static GraphicsPath CreateRoundedPath(Rectangle bounds, int radius)
    {
        int diameter = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        Rectangle arc = new(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }

    public static ScrollablePopupHost CreateScrollablePopupHost(Control content, Size minimumContentSize)
    {
        return new ScrollablePopupHost(content, minimumContentSize);
    }

    public static void MarkAccessible(Control control, string name, string description)
    {
        control.AccessibleName = name;
        control.AccessibleDescription = description;
    }
}
