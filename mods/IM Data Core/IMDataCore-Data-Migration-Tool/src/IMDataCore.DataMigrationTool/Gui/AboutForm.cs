using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class AboutForm : Form
{
    private readonly Label _title = new() { AutoSize = true, Font = new Font("Segoe UI", 15F, FontStyle.Bold), ForeColor = UiStyling.PrimaryText };
    private readonly Label _author = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = UiStyling.SecondaryText };
    private readonly Label _copyright = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = UiStyling.SecondaryText };
    private readonly Label _powered = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = UiStyling.SecondaryText };
    private readonly Label _graphics = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = UiStyling.SecondaryText };
    private readonly Label _accessibility = new() { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = UiStyling.SecondaryText };
    private readonly Label _linksGroup = new() { AutoSize = true, Font = new Font("Segoe UI", 10F, FontStyle.Bold), ForeColor = UiStyling.PrimaryText };
    private readonly LinkLabel _sourceLink = new() { AutoSize = true, MaximumSize = new Size(520, 0), Margin = new Padding(3, 3, 3, 12), LinkColor = UiStyling.Accent, ActiveLinkColor = UiStyling.AccentDark };
    private readonly LinkLabel _modsLink = new() { AutoSize = true, MaximumSize = new Size(520, 0), Margin = new Padding(3, 3, 3, 6), LinkColor = UiStyling.Accent, ActiveLinkColor = UiStyling.AccentDark };
    private readonly Button _close = new() { Width = 120, Height = 36 };
    private readonly PictureBox _iconBox = new() { Width = 96, Height = 96, SizeMode = PictureBoxSizeMode.Zoom, Margin = new Padding(0, 0, 20, 0) };

    public AboutForm()
    {
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        UiStyling.FitWindowToWorkingArea(this, new Size(780, 560), new Size(640, 440));
        FormBorderStyle = FormBorderStyle.SizableToolWindow;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }

        UiStyling.ApplyFormTheme(this);

        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 2 };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var card = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(20) };
        UiStyling.StyleCard(card);

        var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, AutoSize = true, BackColor = Color.Transparent };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 132));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        try { _iconBox.Image = Image.FromFile(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.png")); } catch { }
        body.Controls.Add(_iconBox, 0, 0);

        var infoFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.TopDown, WrapContents = false, AutoSize = true, Dock = DockStyle.Fill, BackColor = Color.Transparent };
        infoFlow.Controls.Add(_title);
        infoFlow.Controls.Add(_author);
        infoFlow.Controls.Add(_copyright);
        infoFlow.Controls.Add(_powered);
        infoFlow.Controls.Add(_graphics);
        infoFlow.Controls.Add(_accessibility);
        infoFlow.Controls.Add(_linksGroup);
        infoFlow.Controls.Add(_sourceLink);
        infoFlow.Controls.Add(_modsLink);
        body.Controls.Add(infoFlow, 1, 0);

        card.Controls.Add(body);
        root.Controls.Add(card, 0, 0);

        var buttonFlow = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill, AutoSize = true, Padding = new Padding(0, 10, 0, 0), BackColor = Color.Transparent };
        _close.Click += (_, _) => Close();
        UiStyling.StyleButton(_close, ButtonStyleKind.Primary);
        buttonFlow.Controls.Add(_close);
        root.Controls.Add(buttonFlow, 0, 1);

        Controls.Add(UiStyling.CreateScrollablePopupHost(root, new Size(700, 500)));

        _sourceLink.LinkClicked += (_, _) => OpenUrl(ToolInfo.SourceUrl);
        _modsLink.LinkClicked += (_, _) => OpenUrl(ToolInfo.ModsUrl);
        UiStyling.ApplyFontScaling(this);
        Localization.LanguageChanged += ApplyLanguage;
        UiSettings.AppearanceChanged += ApplyAppearance;
        Disposed += (_, _) =>
        {
            Localization.LanguageChanged -= ApplyLanguage;
            UiSettings.AppearanceChanged -= ApplyAppearance;
        };
        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("about_title");
        _title.Text = Localization.T("about_header") + " " + ToolInfo.Version;
        _author.Text = Localization.T("about_author");
        _copyright.Text = Localization.T("about_copyright");
        _powered.Text = Localization.T("about_powered");
        _graphics.Text = Localization.T("about_graphics");
        _accessibility.Text = Localization.T("ui_accessibility_note");
        _linksGroup.Text = Localization.T("about_links");
        SetDestinationLink(_sourceLink, Localization.T("about_source_link"), ToolInfo.SourceUrl);
        SetDestinationLink(_modsLink, Localization.T("about_mods_link"), ToolInfo.ModsUrl);
        _close.Text = Localization.T("about_close");
        UiStyling.MarkAccessible(_close, Localization.T("about_close"), Localization.T("about_close"));
        UiStyling.MarkAccessible(_iconBox, Localization.T("about_header"), Localization.T("about_powered"));
        ApplyAppearance();
    }

    private void ApplyAppearance()
    {
        UiStyling.ApplyFormTheme(this);
        UiStyling.ApplyFontScaling(this);
        UiStyling.StyleButton(_close, ButtonStyleKind.Primary);
    }

    private static void SetDestinationLink(LinkLabel label, string caption, string url)
    {
        string cleanCaption = caption.TrimEnd().TrimEnd(':', '：');
        label.Text = cleanCaption + ":" + Environment.NewLine + url;
        label.Links.Clear();
        label.Links.Add(0, label.Text.Length, url);
        label.AccessibleName = cleanCaption;
        label.AccessibleDescription = url;
    }

    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
        }
    }
}
