using System.Drawing;
using System.Windows.Forms;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class HelpForm : Form
{
    private readonly HelpTopic _initialTopic;
    private readonly FlowLayoutPanel _content = new()
    {
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        BackColor = UiStyling.CardBackground,
        Padding = new Padding(26, 22, 26, 26),
        Margin = new Padding(0)
    };
    private readonly Dictionary<HelpTopic, Control> _anchors = new();
    private ScrollablePopupHost? _host;
    private bool _firstShown = true;

    public HelpForm(HelpTopic initialTopic = HelpTopic.Overview)
    {
        _initialTopic = initialTopic;
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        UiStyling.ApplyFormTheme(this);
        UiStyling.FitWindowToWorkingArea(this, new Size(1040, 860), new Size(760, 560));
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }

        _host = UiStyling.CreateScrollablePopupHost(_content, new Size(900, 720));
        Controls.Add(_host);

        Localization.LanguageChanged += ApplyLanguage;
        UiSettings.AppearanceChanged += ApplyAppearance;
        Disposed += (_, _) =>
        {
            Localization.LanguageChanged -= ApplyLanguage;
            UiSettings.AppearanceChanged -= ApplyAppearance;
        };

        Shown += (_, _) =>
        {
            if (!_firstShown) return;
            _firstShown = false;
            BeginInvoke(new Action(() => ScrollToTopic(_initialTopic)));
        };

        ApplyLanguage();
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("help_window_title");
        RebuildContent();
        ApplyAppearance();
    }

    private void RebuildContent()
    {
        _content.SuspendLayout();
        try
        {
            _content.Controls.Clear();
            _anchors.Clear();

            var title = new Label
            {
                AutoSize = true,
                Text = Localization.T("help_header"),
                Font = new Font("Segoe UI", 20F, FontStyle.Bold),
                ForeColor = UiStyling.PrimaryText,
                Margin = new Padding(0, 0, 0, 6)
            };
            _content.Controls.Add(title);

            var intro = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(980, 0),
                Text = Localization.T("help_intro"),
                ForeColor = UiStyling.SecondaryText,
                Margin = new Padding(0, 0, 0, 18)
            };
            _content.Controls.Add(intro);

            foreach (HelpSection section in HelpContent.Current())
            {
                var heading = new Label
                {
                    AutoSize = true,
                    MaximumSize = new Size(980, 0),
                    Text = section.Title,
                    Font = new Font("Segoe UI", 13F, FontStyle.Bold),
                    ForeColor = UiStyling.PrimaryText,
                    Margin = new Padding(0, 10, 0, 5),
                };
                _content.Controls.Add(heading);
                _anchors[section.Topic] = heading;

                var body = new Label
                {
                    AutoSize = true,
                    MaximumSize = new Size(980, 0),
                    Text = section.Body,
                    ForeColor = UiStyling.PrimaryText,
                    Margin = new Padding(0, 0, 0, 14)
                };
                _content.Controls.Add(body);
            }

            var close = new Button { Tag = "about_close", Margin = new Padding(0, 10, 0, 0) };
            close.Click += (_, _) => Close();
            close.Text = Localization.T("about_close");
            UiStyling.StyleButton(close, ButtonStyleKind.Secondary);
            UiStyling.MarkAccessible(close, Localization.T("about_close"), Localization.T("help_close_accessible"));
            _content.Controls.Add(close);
        }
        finally
        {
            _content.ResumeLayout(true);
        }
    }

    private void ApplyAppearance()
    {
        UiStyling.ApplyFormTheme(this);
        UiStyling.ApplyFontScaling(this);
        _content.BackColor = UiStyling.CardBackground;
        foreach (Control control in _content.Controls)
        {
            if (control is Button button)
            {
                UiStyling.StyleButton(button, ButtonStyleKind.Secondary);
            }
        }
        _content.PerformLayout();
        _host?.PerformLayout();
    }

    private void ScrollToTopic(HelpTopic topic)
    {
        if (_host is null) return;
        if (_anchors.TryGetValue(topic, out Control? target))
        {
            _host.ScrollControlIntoView(target);
            target.Focus();
        }
    }
}
