using System.Drawing;
using System.Windows.Forms;
using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class MainForm : Form
{
    private readonly TextBox _source = new() { Dock = DockStyle.Fill };
    private readonly TextBox _save = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputRoot = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputFile = new() { Dock = DockStyle.Fill, ReadOnly = true };
    private readonly TextBox _saveInfo = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 132, ScrollBars = ScrollBars.Vertical };
    private readonly TextBox _legacyHints = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 110, ScrollBars = ScrollBars.Vertical, Font = new Font("Consolas", 9F) };
    private readonly ComboBox _legacyMatches = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _reverseMatches = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly TextBox _reversePreview = new() { Dock = DockStyle.Fill, ReadOnly = true, Multiline = true, Height = 126, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _saveKey = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _version = new() { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _language = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
    private readonly ComboBox _fontSize = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly CheckBox _overwrite = new() { AutoSize = true, Tag = "check_overwrite" };
    private readonly CheckBox _allowUnverified = new() { AutoSize = true, Tag = "check_allow_unverified" };
    private readonly CheckBox _recycleSourceAfterSuccess = new() { AutoSize = true, MaximumSize = new Size(920, 0), Tag = "bulk_cleanup_after_success" };
    private readonly RichTextBox _log = new() { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9F), BorderStyle = BorderStyle.None };
    private readonly Label _sourceInfo = new() { Dock = DockStyle.Fill, AutoSize = true, ForeColor = UiStyling.SecondaryText };
    private readonly Label _title = new() { Font = new Font("Segoe UI", 20F, FontStyle.Bold), AutoSize = true, Tag = "title", ForeColor = UiStyling.PrimaryText };
    private readonly Label _subtitle = new() { AutoSize = true, MaximumSize = new Size(900, 0), Tag = "subtitle", ForeColor = UiStyling.SecondaryText };
    private readonly Label _languageLabel = new() { AutoSize = true, Margin = new Padding(0, 8, 8, 0), Tag = "language_label", ForeColor = UiStyling.SecondaryText };
    private readonly Label _fontSizeLabel = new() { AutoSize = true, Margin = new Padding(12, 8, 8, 0), Tag = "font_size_label", ForeColor = UiStyling.SecondaryText };
    private readonly GroupBox _logBox = new() { Dock = DockStyle.Fill, Padding = new Padding(12), Tag = "group_log" };
    private readonly Button _toggleBanner = new() { Width = 140, Height = 36 };
    private readonly Button _toggleHeader = new() { Width = 150, Height = 36 };
    private readonly Button _bulkScan = new() { Width = 200, Height = 36, Tag = "bulk_main_button" };
    private readonly Button _bulkConvert = new() { Width = 180, Height = 36, Tag = "bulk_convert_main_button" };
    private readonly Button _about = new() { Width = 110, Height = 36, Tag = "about_button" };
    private readonly Button _help = new() { Width = 110, Height = 36, Tag = "help_button" };
    private readonly Button _openLogs = new() { Width = 110, Height = 36, Tag = "open_logs_button" };
    private readonly Button _validate = new() { Width = 140, Height = 38, AutoSize = false, Tag = "btn_validate" };
    private readonly Button _repairContamination = new() { Width = 210, Height = 38, AutoSize = false, Tag = "repair_main_button" };
    private readonly Button _migrate = new() { Width = 150, Height = 38, AutoSize = false, Tag = "btn_migrate" };
    private readonly Button _openDestination = new() { Width = 155, Height = 38, AutoSize = false, Tag = "btn_open_destination" };
    private readonly Button _browseSave = new() { Width = 122, Height = 34, AutoSize = false, Tag = "btn_browse" };
    private readonly Button _browseSource = new() { Width = 122, Height = 34, AutoSize = false, Tag = "btn_browse" };
    private readonly Button _useMatch = new() { Width = 122, Height = 34, AutoSize = false, Tag = "btn_use_match" };
    private readonly Button _browseOutput = new() { Width = 122, Height = 34, AutoSize = false, Tag = "btn_browse" };
    private readonly Button _scanLegacyRoot = new() { Width = 210, Height = 34, AutoSize = false, Tag = "btn_scan_root" };
    private readonly Button _inspect = new() { Width = 122, Height = 34, AutoSize = false, Tag = "btn_inspect" };
    private readonly Button _useVanillaMatch = new() { Width = 122, Height = 34, AutoSize = false, Tag = "btn_use_vanilla_match" };
    private readonly Button _scanVanillaRoot = new() { Width = 210, Height = 34, AutoSize = false, Tag = "btn_scan_vanilla_root" };
    private readonly Label _labelSave = new() { AutoSize = true, Tag = "row_raw_save", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelSaveIdentity = new() { AutoSize = true, Tag = "row_save_identity", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelLegacyHints = new() { AutoSize = true, Tag = "row_legacy_hints", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelLikelyLegacy = new() { AutoSize = true, Tag = "row_likely_legacy", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelLegacyFile = new() { AutoSize = true, Tag = "row_legacy_file", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelDestinationRoot = new() { AutoSize = true, Tag = "row_destination_root", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelResolvedV6 = new() { AutoSize = true, Tag = "row_resolved_v6", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelSaveKey = new() { AutoSize = true, Tag = "row_save_key", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelVersion = new() { AutoSize = true, Tag = "row_release_hint", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelSourceDetection = new() { AutoSize = true, Tag = "row_source_detection", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelReverseVanilla = new() { AutoSize = true, Tag = "row_reverse_vanilla", ForeColor = UiStyling.PrimaryText };
    private readonly Label _labelReverseDetails = new() { AutoSize = true, Tag = "row_reverse_details", ForeColor = UiStyling.PrimaryText };
    private readonly MigrationService _service = new();
    private VanillaSaveInfo? _selectedSaveInfo;
    private LegacyInspection? _lastLegacyInspection;
    private string? _lastReverseDataRoot;
    private ResponsiveBannerPanel? _bannerCard;
    private TableLayoutPanel? _headerContent;
    private bool _bannerExpanded = true;
    private bool _headerExpanded = true;

    public MainForm()
    {
        StartPosition = FormStartPosition.CenterScreen;
        AutoScaleMode = AutoScaleMode.Dpi;
        ApplyStartupWindowSize();
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }

        _version.Items.AddRange(new object[] { "Auto", "1.0.0", "1.1.0", "1.2.0", "1.3.0" });
        _version.SelectedIndex = 0;
        _saveKey.Items.Add("Auto / only key");
        _saveKey.SelectedIndex = 0;
        _legacyMatches.Items.Add("Select a vanilla save to scan for likely legacy data");
        _legacyMatches.SelectedIndex = 0;
        _reverseMatches.Items.Add("Inspect legacy IMDataCore data to find matching vanilla saves");
        _reverseMatches.SelectedIndex = 0;
        _reverseMatches.Format += (_, e) =>
        {
            if (e.ListItem is VanillaSaveCandidate candidate) e.Value = FormatReverseCandidate(candidate);
        };
        _reverseMatches.SelectedIndexChanged += (_, _) => UpdateReversePreview();

        foreach (LanguageOption language in Localization.Languages) _language.Items.Add(language);
        _language.SelectedIndexChanged += (_, _) =>
        {
            if (_language.SelectedItem is LanguageOption option) Localization.SetLanguage(option.Code);
        };
        for (int i = 0; i < _language.Items.Count; i++)
        {
            if (_language.Items[i] is LanguageOption option && option.Code == Localization.CurrentCode)
            {
                _language.SelectedIndex = i;
                break;
            }
        }
        if (_language.SelectedIndex < 0 && _language.Items.Count > 0) _language.SelectedIndex = 0;

        foreach (FontScaleOption option in UiSettings.FontScaleOptions) _fontSize.Items.Add(option);
        _fontSize.SelectedIndexChanged += (_, _) =>
        {
            if (_fontSize.SelectedItem is FontScaleOption option) UiSettings.SetFontScale(option.Scale);
        };
        for (int i = 0; i < _fontSize.Items.Count; i++)
        {
            if (_fontSize.Items[i] is FontScaleOption option && Math.Abs(option.Scale - UiSettings.FontScale) < 0.001f)
            {
                _fontSize.SelectedIndex = i;
                break;
            }
        }
        if (_fontSize.SelectedIndex < 0 && _fontSize.Items.Count > 0) _fontSize.SelectedIndex = 1;

        _browseSave.Click += BrowseSave;
        _browseSource.Click += BrowseSource;
        _browseOutput.Click += BrowseOutput;
        _useMatch.Click += UseLegacyMatch;
        _scanLegacyRoot.Click += ScanLegacyFolder;
        _inspect.Click += InspectSource;
        _useVanillaMatch.Click += UseVanillaMatch;
        _scanVanillaRoot.Click += ScanVanillaFolder;
        _toggleBanner.Click += (_, _) => ToggleBanner();
        _toggleHeader.Click += (_, _) => ToggleHeader();
        _bulkScan.Click += (_, _) => new BulkScanForm(GetBulkScanDefaultRoot()).ShowDialog(this);
        _bulkConvert.Click += (_, _) => new BulkConvertForm(GetBulkScanDefaultRoot()).ShowDialog(this);
        _about.Click += (_, _) => new AboutForm().ShowDialog(this);
        _help.Click += (_, _) => new HelpForm(HelpTopic.Overview).ShowDialog(this);
        _openLogs.Click += (_, _) => OpenLogs();
        _validate.Click += (_, _) => Run(false);
        _repairContamination.Click += (_, _) => OpenRepair();
        _migrate.Click += (_, _) => Run(true);
        _openDestination.Click += (_, _) => OpenDestination();

        BuildUi();
        ApplyAccessibility();
        Localization.LanguageChanged += ApplyLanguage;
        UiSettings.AppearanceChanged += ApplyAppearance;
        Disposed += (_, _) =>
        {
            Localization.LanguageChanged -= ApplyLanguage;
            UiSettings.AppearanceChanged -= ApplyAppearance;
        };
        ApplyLanguage();
        Shown += (_, _) =>
        {
            AppLog.Info("Main window shown successfully.");
            Append("Session diagnostic log: " + AppLog.SessionLogPath);
        };
        Resize += (_, _) => UpdateBannerLayout();
        FormClosed += (_, _) => AppLog.Info("Main window closed.");
    }

    private void ToggleBanner()
    {
        _bannerExpanded = !_bannerExpanded;
        UpdateBannerLayout();
        AppLog.Info("Banner " + (_bannerExpanded ? "expanded." : "collapsed."));
    }

    private void ToggleHeader()
    {
        _headerExpanded = !_headerExpanded;
        UpdateHeaderLayout();
        AppLog.Info("Main header/settings section " + (_headerExpanded ? "expanded." : "collapsed."));
    }

    private void UpdateHeaderLayout()
    {
        if (_headerContent is null || _headerContent.IsDisposed) return;
        _headerContent.Visible = _headerExpanded;
        _toggleHeader.Text = Localization.T(_headerExpanded ? "header_hide_button" : "header_show_button");
        _toggleHeader.AccessibleName = _toggleHeader.Text;
        _toggleHeader.AccessibleDescription = _toggleHeader.Text;
        _headerContent.Parent?.PerformLayout();
    }

    private int CalculateBannerHeight()
    {
        // Keep the banner useful on large displays without letting it dominate a
        // 1080p/high-DPI working area. The user can collapse it entirely.
        int clientHeight = ClientSize.Height > 0 ? ClientSize.Height : Height;
        return Math.Clamp((int)Math.Round(clientHeight * 0.16), 104, 154);
    }

    private void UpdateBannerLayout()
    {
        if (_bannerCard is null || _bannerCard.IsDisposed) return;
        _bannerCard.Visible = _bannerExpanded;
        _bannerCard.Height = _bannerExpanded ? CalculateBannerHeight() : 0;
        _toggleBanner.Text = Localization.T(_bannerExpanded ? "banner_hide_button" : "banner_show_button");
        _toggleHeader.Text = Localization.T(_headerExpanded ? "header_hide_button" : "header_show_button");
        _toggleBanner.AccessibleName = _toggleBanner.Text;
        _toggleBanner.AccessibleDescription = _toggleBanner.Text;
        PerformLayout();
    }

    private void ApplyStartupWindowSize()
    {
        UiStyling.FitWindowToWorkingArea(this, new Size(1500, 900), new Size(1050, 640));
    }

    private void BuildUi()
    {
        UiStyling.ApplyFormTheme(this);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 2,
            BackColor = UiStyling.PageBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _bannerCard = new ResponsiveBannerPanel
        {
            Dock = DockStyle.Top,
            Height = CalculateBannerHeight(),
            Margin = new Padding(0, 0, 0, 10),
            AccessibleName = Localization.T("title"),
            AccessibleDescription = Localization.T("subtitle")
        };
        UiStyling.StyleCard(_bannerCard);
        try { _bannerCard.BannerImage = new Bitmap(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool_banner.png")); } catch { }
        root.Controls.Add(_bannerCard, 0, 0);

        var split = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            BackColor = UiStyling.PageBackground,
            FixedPanel = FixedPanel.None,
            SplitterWidth = 8
        };
        split.HandleCreated += (_, _) => split.BeginInvoke(new Action(() => ApplyResponsiveSplit(split)));
        split.Resize += (_, _) => ApplyResponsiveSplit(split);

        // Everything workflow-related on the left now belongs to one scrollable
        // document. The header/settings area and migration actions therefore scroll
        // away with the save fields instead of permanently consuming screen space.
        var leftScroll = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            BackColor = UiStyling.PageBackground,
            Margin = new Padding(0)
        };
        var mainDocument = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 3,
            BackColor = UiStyling.PageBackground,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainDocument.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        mainDocument.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainDocument.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainDocument.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var headerCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18, 10, 18, 12),
            Margin = new Padding(0, 0, 0, 10)
        };
        UiStyling.StyleCard(headerCard);
        var headerShell = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        headerShell.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        headerShell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerShell.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // This compact strip intentionally remains when the header content is
        // collapsed so both the header and banner can always be restored. It is
        // still part of the scrolling document and is therefore not sticky.
        var collapseBar = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };
        collapseBar.Controls.Add(_toggleHeader);
        collapseBar.Controls.Add(_toggleBanner);
        headerShell.Controls.Add(collapseBar, 0, 0);

        _headerContent = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        _headerContent.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        _headerContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _headerContent.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var header = new FlowLayoutPanel
        {
            AutoSize = true,
            FlowDirection = FlowDirection.TopDown,
            Dock = DockStyle.Fill,
            WrapContents = false,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
        };
        header.Controls.Add(_title);
        header.Controls.Add(_subtitle);
        _headerContent.Controls.Add(header, 0, 0);

        var tools = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            Dock = DockStyle.Fill,
            WrapContents = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 8, 0, 0)
        };
        tools.Controls.Add(_languageLabel);
        tools.Controls.Add(_language);
        tools.Controls.Add(_fontSizeLabel);
        tools.Controls.Add(_fontSize);
        tools.Controls.Add(_bulkScan);
        tools.Controls.Add(_bulkConvert);
        tools.Controls.Add(_openLogs);
        tools.Controls.Add(_help);
        tools.Controls.Add(_about);
        _headerContent.Controls.Add(tools, 0, 1);
        headerShell.Controls.Add(_headerContent, 0, 1);
        headerCard.Controls.Add(headerShell);
        mainDocument.Controls.Add(headerCard, 0, 0);

        var fieldsCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(18),
            Margin = new Padding(0, 0, 0, 10)
        };
        UiStyling.StyleCard(fieldsCard);
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3,
            BackColor = Color.Transparent
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        AddRow(fields, _labelSave, _save, _browseSave);
        AddTallRow(fields, _labelSaveIdentity, _saveInfo);
        AddTallRow(fields, _labelLegacyHints, _legacyHints);
        AddRow(fields, _labelLikelyLegacy, _legacyMatches, _useMatch);
        AddRow(fields, _labelLegacyFile, _source, _browseSource);
        AddActionRow(fields, new Label { Text = "", AutoSize = true, BackColor = Color.Transparent }, _scanLegacyRoot);
        AddRow(fields, _labelReverseVanilla, _reverseMatches, _useVanillaMatch);
        AddTallRow(fields, _labelReverseDetails, _reversePreview);
        AddActionRow(fields, new Label { Text = "", AutoSize = true, BackColor = Color.Transparent }, _scanVanillaRoot);
        AddRow(fields, _labelDestinationRoot, _outputRoot, _browseOutput);
        AddRow(fields, _labelResolvedV6, _outputFile, new Label { Text = string.Empty, BackColor = Color.Transparent });
        AddRow(fields, _labelSaveKey, _saveKey, _inspect);
        AddRow(fields, _labelVersion, _version, new Label { Text = string.Empty, BackColor = Color.Transparent });
        fields.Controls.Add(_labelSourceDetection, 0, fields.RowCount);
        fields.Controls.Add(_sourceInfo, 1, fields.RowCount);
        fields.SetColumnSpan(_sourceInfo, 2);
        fields.RowCount++;
        fields.Controls.Add(new Label { BackColor = Color.Transparent }, 0, fields.RowCount);
        fields.Controls.Add(_overwrite, 1, fields.RowCount);
        fields.SetColumnSpan(_overwrite, 2);
        fields.RowCount++;
        fields.Controls.Add(new Label { BackColor = Color.Transparent }, 0, fields.RowCount);
        fields.Controls.Add(_allowUnverified, 1, fields.RowCount);
        fields.SetColumnSpan(_allowUnverified, 2);
        fields.RowCount++;
        fields.Controls.Add(new Label { BackColor = Color.Transparent }, 0, fields.RowCount);
        fields.Controls.Add(_recycleSourceAfterSuccess, 1, fields.RowCount);
        fields.SetColumnSpan(_recycleSourceAfterSuccess, 2);
        fields.RowCount++;
        fieldsCard.Controls.Add(fields);
        mainDocument.Controls.Add(fieldsCard, 0, 1);

        // Migration actions now live at the end of the main scrolling workflow
        // instead of in a sticky footer. This avoids permanently reserving vertical
        // space on 1080p displays and keeps actions next to the fields they operate on.
        var actionCard = new CardPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Padding = new Padding(14),
            Margin = new Padding(0)
        };
        UiStyling.StyleCard(actionCard);
        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0),
            Margin = new Padding(0),
            BackColor = Color.Transparent
        };
        buttons.Controls.Add(_validate);
        buttons.Controls.Add(_repairContamination);
        buttons.Controls.Add(_migrate);
        buttons.Controls.Add(_openDestination);
        actionCard.Controls.Add(buttons);
        mainDocument.Controls.Add(actionCard, 0, 2);

        leftScroll.Controls.Add(mainDocument);
        split.Panel1.Controls.Add(leftScroll);

        var logCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(0) };
        UiStyling.StyleCard(logCard);
        _logBox.BackColor = Color.Transparent;
        _log.BackColor = UiStyling.SurfaceReadOnly;
        _log.ForeColor = UiStyling.PrimaryText;
        _logBox.Controls.Add(_log);
        logCard.Controls.Add(_logBox);
        split.Panel2.Controls.Add(logCard);
        root.Controls.Add(split, 0, 1);

        Controls.Add(root);
        UpdateHeaderLayout();
        ApplyAppearance();
        ApplyAccessibility();
    }

    private static void AddRow(TableLayoutPanel table, Label label, Control main, Control button)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        label.Anchor = AnchorStyles.Left;
        label.Margin = new Padding(3, 9, 3, 3);
        label.BackColor = Color.Transparent;
        table.Controls.Add(label, 0, row);
        main.Margin = new Padding(3, 5, 3, 5);
        table.Controls.Add(main, 1, row);
        button.Margin = new Padding(3, 5, 3, 5);
        table.Controls.Add(button, 2, row);
    }

    private static void AddTallRow(TableLayoutPanel table, Label label, Control main)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        label.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        label.Margin = new Padding(3, 9, 3, 3);
        label.BackColor = Color.Transparent;
        table.Controls.Add(label, 0, row);
        main.Margin = new Padding(3, 5, 3, 5);
        table.Controls.Add(main, 1, row);
        table.SetColumnSpan(main, 2);
    }

    private static void AddActionRow(TableLayoutPanel table, Label label, Control action)
    {
        int row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        label.BackColor = Color.Transparent;
        table.Controls.Add(label, 0, row);
        var panel = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.LeftToRight, BackColor = Color.Transparent };
        panel.Controls.Add(action);
        table.Controls.Add(panel, 1, row);
        table.SetColumnSpan(panel, 2);
    }

    private void ApplyAppearance()
    {
        UiStyling.ApplyFormTheme(this);
        UiStyling.ApplyFontScaling(this);

        foreach (Control control in new Control[] { _source, _save, _outputRoot, _outputFile, _saveInfo, _legacyHints, _reversePreview, _language, _fontSize, _legacyMatches, _reverseMatches, _saveKey, _version })
            UiStyling.StyleInput(control, control == _outputFile || control == _saveInfo || control == _legacyHints || control == _reversePreview);

        UiStyling.StyleButton(_browseSave, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_browseSource, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_browseOutput, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_useMatch, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_scanLegacyRoot, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_inspect, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_useVanillaMatch, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_scanVanillaRoot, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_toggleBanner, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_toggleHeader, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_bulkScan, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_bulkConvert, ButtonStyleKind.Accent);
        UiStyling.StyleButton(_about, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_help, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_openLogs, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_validate, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_repairContamination, ButtonStyleKind.Accent);
        UiStyling.StyleButton(_migrate, ButtonStyleKind.Primary);
        UiStyling.StyleButton(_openDestination, ButtonStyleKind.Secondary);

        _logBox.ForeColor = UiStyling.PrimaryText;
        _log.BackColor = UiStyling.SurfaceReadOnly;
        _log.ForeColor = UiStyling.PrimaryText;
        _sourceInfo.ForeColor = UiStyling.SecondaryText;
        UpdateBannerLayout();
    }

    private void ApplyAccessibility()
    {
        static void Mark(Control control, string key)
        {
            string text = Localization.T(key);
            UiStyling.MarkAccessible(control, text, text);
        }

        Mark(_save, "row_raw_save");
        Mark(_source, "row_legacy_file");
        Mark(_outputRoot, "row_destination_root");
        Mark(_outputFile, "row_resolved_v6");
        Mark(_saveInfo, "row_save_identity");
        Mark(_legacyHints, "row_legacy_hints");
        Mark(_legacyMatches, "row_likely_legacy");
        Mark(_reverseMatches, "row_reverse_vanilla");
        Mark(_reversePreview, "row_reverse_details");
        Mark(_saveKey, "row_save_key");
        Mark(_version, "row_release_hint");
        Mark(_language, "language_label");
        Mark(_fontSize, "font_size_label");
        Mark(_overwrite, "check_overwrite");
        Mark(_allowUnverified, "check_allow_unverified");
        Mark(_recycleSourceAfterSuccess, "bulk_cleanup_after_success");
        Mark(_log, "group_log");
        Mark(_browseSave, "btn_browse");
        Mark(_browseSource, "btn_browse");
        Mark(_browseOutput, "btn_browse");
        Mark(_useMatch, "btn_use_match");
        Mark(_scanLegacyRoot, "btn_scan_root");
        Mark(_inspect, "btn_inspect");
        Mark(_useVanillaMatch, "btn_use_vanilla_match");
        Mark(_scanVanillaRoot, "btn_scan_vanilla_root");
        UiStyling.MarkAccessible(_toggleBanner, _toggleBanner.Text, _toggleBanner.Text);
        UiStyling.MarkAccessible(_toggleHeader, _toggleHeader.Text, _toggleHeader.Text);
        Mark(_bulkScan, "bulk_main_button");
        Mark(_bulkConvert, "bulk_convert_main_button");
        Mark(_about, "about_button");
        Mark(_help, "help_button");
        Mark(_openLogs, "open_logs_button");
        Mark(_validate, "btn_validate");
        Mark(_repairContamination, "repair_main_button");
        Mark(_migrate, "btn_migrate");
        Mark(_openDestination, "btn_open_destination");
        if (_bannerCard is not null)
            UiStyling.MarkAccessible(_bannerCard, Localization.T("title"), Localization.T("subtitle"));
    }

    private void ApplyLanguage()
    {
        Text = $"{Localization.T("title")} {ToolInfo.Version}";
        ApplyTranslationsRecursive(this);
        _toggleBanner.Text = Localization.T(_bannerExpanded ? "banner_hide_button" : "banner_show_button");
        _toggleHeader.Text = Localization.T(_headerExpanded ? "header_hide_button" : "header_show_button");
        string selectedVersion = _version.SelectedIndex > 0 ? _version.SelectedItem?.ToString() ?? string.Empty : string.Empty;
        _version.Items.Clear();
        _version.Items.AddRange(new object[] { Localization.T("version_auto"), "1.0.0", "1.1.0", "1.2.0", "1.3.0" });
        _version.SelectedIndex = 0;
        if (!string.IsNullOrWhiteSpace(selectedVersion))
        {
            int idx = _version.Items.IndexOf(selectedVersion);
            if (idx >= 0) _version.SelectedIndex = idx;
        }
        ResetSaveKeyPlaceholder();
        if (_selectedSaveInfo is not null)
        {
            _saveInfo.Text = _selectedSaveInfo.ToIdentityMultilineString(Localization.T);
            _legacyHints.Text = _selectedSaveInfo.ToLegacyMatchMultilineString(Localization.T);
        }
        else
        {
            _legacyMatches.Items.Clear();
            _legacyMatches.Items.Add(Localization.T("legacy_matches_prompt"));
            _legacyMatches.SelectedIndex = 0;
        }

        if (_lastLegacyInspection is null)
        {
            _reverseMatches.Items.Clear();
            _reverseMatches.Items.Add(Localization.T("reverse_matches_prompt"));
            _reverseMatches.SelectedIndex = 0;
            _reversePreview.Clear();
        }
        else
        {
            _reverseMatches.Refresh();
            UpdateReversePreview();
        }
        UpdateHeaderLayout();
        ApplyAppearance();
        ApplyAccessibility();
    }


    private static void ApplyResponsiveSplit(SplitContainer split)
    {
        try
        {
            int width = split.ClientSize.Width;
            if (width < 720) return;

            // Keep the log usable on 1080p instead of shrinking it to a narrow strip.
            int rightMin = Math.Min(480, Math.Max(380, (int)Math.Round(width * 0.28)));
            int leftMin = Math.Min(680, Math.Max(480, (int)Math.Round(width * 0.42)));
            int maxDistance = width - rightMin - split.SplitterWidth;
            if (maxDistance <= leftMin) return;

            int desired = (int)Math.Round(width * 0.64);
            int distance = Math.Clamp(desired, leftMin, maxDistance);

            split.SplitterDistance = distance;
            split.Panel1MinSize = leftMin;
            split.Panel2MinSize = rightMin;
        }
        catch (Exception ex)
        {
            AppLog.Warning("Responsive split layout adjustment failed but was ignored: " + ex.Message);
        }
    }

    private void OpenLogs()
    {
        try
        {
            AppLog.Info("Opening diagnostic log directory from the GUI.");
            AppLog.OpenLogDirectory();
        }
        catch (Exception ex)
        {
            ShowError("Could not open the diagnostic log folder.\n\n" + ex.Message + "\n\nLog path: " + AppLog.LogDirectory);
        }
    }
    private static void ApplyTranslationsRecursive(Control root)
    {
        if (root.Tag is string key && !string.IsNullOrWhiteSpace(key)) root.Text = Localization.T(key);
        foreach (Control child in root.Controls) ApplyTranslationsRecursive(child);
    }

    private void ResetSaveKeyPlaceholder()
    {
        if (_saveKey.Items.Count == 0 || _saveKey.Items[0] is not string first || first == Localization.T("auto_save_key")) return;
        _saveKey.Items[0] = Localization.T("auto_save_key");
    }

    private string GetBulkScanDefaultRoot()
    {
        if (!string.IsNullOrWhiteSpace(_source.Text))
        {
            string? sourceDir = Path.GetDirectoryName(_source.Text);
            if (!string.IsNullOrWhiteSpace(sourceDir))
            {
                DirectoryInfo? parent = Directory.GetParent(sourceDir);
                if (parent is not null && parent.Exists) return parent.FullName;
                if (Directory.Exists(sourceDir)) return sourceDir;
            }
        }

        if (_selectedSaveInfo is not null && Directory.Exists(_selectedSaveInfo.LegacySavesRoot))
            return _selectedSaveInfo.LegacySavesRoot;

        try
        {
            string data = VanillaSaveReader.DefaultDataDirectory();
            string? persistentRoot = Directory.GetParent(data)?.FullName;
            if (!string.IsNullOrWhiteSpace(persistentRoot))
            {
                string legacy = Path.Combine(persistentRoot, "Mods", "IMDataCore", "saves");
                if (Directory.Exists(legacy)) return legacy;
            }
        }
        catch { }
        return string.Empty;
    }

    private void BrowseSource(object? sender, EventArgs e)
    {
        string initial = _selectedSaveInfo?.LegacySavesRoot ?? "";
        using var dialog = new OpenFileDialog
        {
            Title = Localization.T("dlg_select_legacy_title"),
            Filter = "IMDataCore data (*.db;*.json)|*.db;*.json|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(initial) ? initial : ""
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _source.Text = dialog.FileName;
            InspectSource(null, EventArgs.Empty);
        }
    }

    private void BrowseSave(object? sender, EventArgs e)
    {
        string initial = VanillaSaveReader.DefaultDataDirectory();
        using var dialog = new OpenFileDialog
        {
            Title = Localization.T("dlg_select_save_title"),
            Filter = "Idol Manager save (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(initial) ? initial : ""
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _save.Text = dialog.FileName;
        try
        {
            VanillaSaveInfo info = VanillaSaveReader.Read(dialog.FileName);
            ApplySaveInfo(info);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ApplySaveInfo(VanillaSaveInfo info, bool preserveLegacySource = false)
    {
        if (!preserveLegacySource)
        {
            _source.Text = "";
            _sourceInfo.Text = "";
            _saveKey.Items.Clear();
            _saveKey.Items.Add(Localization.T("auto_save_key"));
            _saveKey.SelectedIndex = 0;
            _lastLegacyInspection = null;
            ClearReverseMatches();
        }

        _selectedSaveInfo = info;
        _saveInfo.Text = info.ToIdentityMultilineString(Localization.T);
        _legacyHints.Text = info.ToLegacyMatchMultilineString(Localization.T);
        _outputRoot.Text = Path.Combine(info.PersistentRoot, "IMDataCore");
        _outputFile.Text = Path.Combine(_outputRoot.Text, info.RelativeSavePath.Replace('/', Path.DirectorySeparatorChar));
        Append(Localization.T("save_identified_log") + Environment.NewLine + info.ToIdentityMultilineString(Localization.T) + Environment.NewLine + Environment.NewLine + info.ToLegacyMatchMultilineString(Localization.T));
        PopulateLegacyMatches(info, null, true);
    }

    private void PopulateLegacyMatches(VanillaSaveInfo info, string? rootOverride, bool autoUseUniqueBest)
    {
        List<LegacySourceCandidate> matches = VanillaSaveReader.FindLegacySourceCandidates(info, rootOverride);
        _legacyMatches.Items.Clear();
        foreach (LegacySourceCandidate match in matches) _legacyMatches.Items.Add(match);

        if (matches.Count == 0)
        {
            string root = string.IsNullOrWhiteSpace(rootOverride) ? info.LegacySavesRoot : rootOverride;
            _legacyMatches.Items.Add(Localization.Format("no_legacy_found", root));
            _legacyMatches.SelectedIndex = 0;
            Append(Localization.Format("no_legacy_found_log", root));
            return;
        }

        _legacyMatches.SelectedIndex = 0;
        Append(Localization.T("legacy_candidates_log") + Environment.NewLine + string.Join(Environment.NewLine, matches.Select(x => "  " + x)));

        bool uniqueBest = matches.Count == 1 || matches[0].Score > matches[1].Score;
        if (autoUseUniqueBest && uniqueBest && matches[0].Score >= 90 && string.IsNullOrWhiteSpace(_source.Text))
        {
            _source.Text = matches[0].SourcePath;
            Append(Localization.Format("auto_selected_log", matches[0].SourcePath));
            InspectSource(null, EventArgs.Empty);
        }
    }

    private void ScanLegacyFolder(object? sender, EventArgs e)
    {
        if (_selectedSaveInfo is null)
        {
            ShowError(Localization.T("select_save_first"));
            return;
        }

        string initial = Directory.Exists(_selectedSaveInfo.LegacySavesRoot) ? _selectedSaveInfo.LegacySavesRoot : _selectedSaveInfo.PersistentRoot;
        using var dialog = new FolderBrowserDialog { Description = Localization.T("dlg_select_legacy_root"), SelectedPath = initial };
        if (dialog.ShowDialog(this) == DialogResult.OK) PopulateLegacyMatches(_selectedSaveInfo, dialog.SelectedPath, false);
    }

    private void UseLegacyMatch(object? sender, EventArgs e)
    {
        if (_legacyMatches.SelectedItem is not LegacySourceCandidate candidate)
        {
            ShowError(Localization.T("no_candidate_selected"));
            return;
        }
        _source.Text = candidate.SourcePath;
        InspectSource(null, EventArgs.Empty);
    }

    private void BrowseOutput(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = Localization.T("dlg_select_output_root"), SelectedPath = Directory.Exists(_outputRoot.Text) ? _outputRoot.Text : "" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        _outputRoot.Text = dialog.SelectedPath;
        RefreshOutputPath();
    }

    private async void InspectSource(object? sender, EventArgs e)
    {
        try
        {
            LegacyInspection info = _service.InspectLegacy(_source.Text);
            _lastLegacyInspection = info;
            _sourceInfo.Text = info.DetectedVersion + " • " + info.BackendKind + (info.HasCheckpointSupport ? " • exact checkpoint capable" : " • pre-checkpoint generation");
            _saveKey.Items.Clear();
            _saveKey.Items.Add(Localization.T("auto_save_key"));
            foreach (string key in info.SaveKeys) _saveKey.Items.Add(key);
            _saveKey.SelectedIndex = 0;
            Append(info.ToMultilineString());

            if (_selectedSaveInfo is null && Directory.Exists(VanillaSaveReader.DefaultDataDirectory()))
                await PopulateReverseMatchesAsync(info, null);
            else if (_selectedSaveInfo is not null)
                await PopulateReverseMatchesAsync(info, _selectedSaveInfo.DataRoot);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void ScanVanillaFolder(object? sender, EventArgs e)
    {
        LegacyInspection? inspection = _lastLegacyInspection;
        if (inspection is null)
        {
            ShowError(Localization.T("select_legacy_first"));
            return;
        }

        string initial = _lastReverseDataRoot ?? _selectedSaveInfo?.DataRoot ?? VanillaSaveReader.DefaultDataDirectory();
        using var dialog = new FolderBrowserDialog
        {
            Description = Localization.T("dlg_select_vanilla_root"),
            SelectedPath = Directory.Exists(initial) ? initial : ""
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
            await PopulateReverseMatchesAsync(inspection, dialog.SelectedPath);
    }

    private async Task PopulateReverseMatchesAsync(LegacyInspection inspection, string? dataRoot)
    {
        string? selectedKey = _saveKey.SelectedIndex > 0 ? _saveKey.SelectedItem?.ToString() : null;
        _scanVanillaRoot.Enabled = false;
        _useVanillaMatch.Enabled = false;
        try
        {
            ReverseMatchScanResult result = await Task.Run(() => VanillaReverseMatcher.Scan(inspection, dataRoot, selectedKey));
            if (!ReferenceEquals(_lastLegacyInspection, inspection)) return;
            _lastReverseDataRoot = result.DataRoot;
            _reverseMatches.Items.Clear();
            foreach (VanillaSaveCandidate candidate in result.Candidates) _reverseMatches.Items.Add(candidate);

            Append(Localization.Format("reverse_scan_summary", result.JsonFilesExamined, result.ValidSavesRead));

            if (result.Candidates.Count == 0)
            {
                _reverseMatches.Items.Add(Localization.Format("no_reverse_found", result.DataRoot));
                _reverseMatches.SelectedIndex = 0;
                _reversePreview.Text = Localization.Format("no_reverse_found_log", result.DataRoot);
                Append(Localization.Format("no_reverse_found_log", result.DataRoot));
                return;
            }

            _reverseMatches.SelectedIndex = 0;
            Append(Localization.T("reverse_candidates_log") + Environment.NewLine +
                   string.Join(Environment.NewLine, result.Candidates.Select(x => "  " + FormatReverseCandidate(x))));
            UpdateReversePreview();

            // When reverse matching started without a vanilla save, automatically
            // apply only a unique exact historical save-key match. Agency keys can
            // legitimately match multiple copies/branches of the same campaign, so
            // ties remain manual rather than guessing.
            bool uniqueBest = result.Candidates.Count == 1 || result.Candidates[0].Score > result.Candidates[1].Score;
            bool exactHistoricalKey = result.Candidates[0].Score >= 108;
            if (_selectedSaveInfo is null && string.IsNullOrWhiteSpace(_save.Text) && uniqueBest && exactHistoricalKey)
                ApplyReverseCandidate(result.Candidates[0]);
        }
        catch (Exception ex)
        {
            ClearReverseMatches();
            ShowError(ex.Message);
        }
        finally
        {
            _scanVanillaRoot.Enabled = true;
            _useVanillaMatch.Enabled = true;
        }
    }

    private void UseVanillaMatch(object? sender, EventArgs e)
    {
        if (_reverseMatches.SelectedItem is not VanillaSaveCandidate candidate)
        {
            ShowError(Localization.T("no_reverse_candidate_selected"));
            return;
        }

        ApplyReverseCandidate(candidate);
    }

    private void ApplyReverseCandidate(VanillaSaveCandidate candidate)
    {
        VanillaSaveInfo fullInfo;
        try
        {
            fullInfo = VanillaSaveReader.Read(candidate.SaveInfo.FilePath);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
            return;
        }

        // Preserve the already-selected legacy source. Applying the matched vanilla
        // save also recomputes the normal live IMDataCore root and resolved v6 path.
        _save.Text = fullInfo.FilePath;
        ApplySaveInfo(fullInfo, preserveLegacySource: true);

        if (!string.IsNullOrWhiteSpace(candidate.MatchedSaveKey))
        {
            for (int i = 1; i < _saveKey.Items.Count; i++)
            {
                if (string.Equals(_saveKey.Items[i]?.ToString(), candidate.MatchedSaveKey, StringComparison.OrdinalIgnoreCase))
                {
                    _saveKey.SelectedIndex = i;
                    break;
                }
            }
        }

        Append(Localization.Format("reverse_used_log", candidate.SaveInfo.RelativeSavePath));
    }

    private void ClearReverseMatches()
    {
        _reverseMatches.Items.Clear();
        _reverseMatches.Items.Add(Localization.T("reverse_matches_prompt"));
        _reverseMatches.SelectedIndex = 0;
        _reversePreview.Clear();
        _lastReverseDataRoot = null;
    }

    private void UpdateReversePreview()
    {
        if (_reverseMatches.SelectedItem is not VanillaSaveCandidate candidate)
        {
            if (_reverseMatches.SelectedItem is string text) _reversePreview.Text = text;
            return;
        }

        _reversePreview.Text =
            Localization.T("reverse_match_reason_label") + ReverseReason(candidate.MatchKind) + Environment.NewLine +
            Localization.T("reverse_matched_key_label") + candidate.MatchedSaveKey + Environment.NewLine +
            Localization.T("reverse_folder_token_label") + (string.IsNullOrWhiteSpace(candidate.SaveInfo.SlotToken) ? Localization.T("not_stored") : candidate.SaveInfo.SlotToken) + Environment.NewLine +
            candidate.SaveInfo.ToIdentityMultilineString(Localization.T);
    }

    private static string FormatReverseCandidate(VanillaSaveCandidate candidate)
    {
        string group = string.IsNullOrWhiteSpace(candidate.SaveInfo.GroupName) ? "?" : candidate.SaveInfo.GroupName;
        string player = string.IsNullOrWhiteSpace(candidate.SaveInfo.PlayerName) ? "?" : candidate.SaveInfo.PlayerName;
        string slot = string.IsNullOrWhiteSpace(candidate.SaveInfo.SlotToken) ? "?" : candidate.SaveInfo.SlotToken;
        return $"{ReverseReason(candidate.MatchKind)} • {group} • {player} • {slot} • {candidate.SaveInfo.RelativeSavePath}";
    }

    private static string ReverseReason(string kind) => kind switch
    {
        "exact_file_key" => Localization.T("reverse_reason_exact_file"),
        "exact_agency_key" => Localization.T("reverse_reason_exact_agency"),
        "exact_agency_fallback_key" => Localization.T("reverse_reason_exact_agency_fallback"),
        "file_path_token" => Localization.T("reverse_reason_file_path_token"),
        "source_folder_file_key" => Localization.T("reverse_reason_source_folder_file"),
        "source_folder_agency_key" => Localization.T("reverse_reason_source_folder_agency"),
        "source_folder_agency_fallback_key" => Localization.T("reverse_reason_source_folder_agency_fallback"),
        "source_folder_slot" => Localization.T("reverse_reason_source_folder_slot"),
        _ => kind
    };

    private void RefreshOutputPath()
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_save.Text) || string.IsNullOrWhiteSpace(_outputRoot.Text)) return;
            VanillaSaveInfo info = VanillaSaveReader.Read(_save.Text);
            _outputFile.Text = Path.Combine(_outputRoot.Text, info.RelativeSavePath.Replace('/', Path.DirectorySeparatorChar));
        }
        catch
        {
            _outputFile.Text = "";
        }
    }

    private void Run(bool write)
    {
        try
        {
            string? key = _saveKey.SelectedIndex > 0 ? _saveKey.SelectedItem?.ToString() : null;
            string? version = _version.SelectedIndex > 0 ? _version.SelectedItem?.ToString() : null;
            MigrationPlan plan = _service.BuildPlan(_source.Text, _save.Text, _outputRoot.Text, key, version, _allowUnverified.Checked);
            _outputFile.Text = plan.OutputSidecarPath;
            MigrationReport report = write ? _service.Migrate(plan, _overwrite.Checked) : _service.ValidatePlan(plan);
            if (write && report.Success && _recycleSourceAfterSuccess.Checked)
            {
                if (LegacyCleanupService.CanRecycleWholeSource(_service, plan, out string cleanupReason))
                {
                    LegacyCleanupBatchResult cleanup = LegacyCleanupService.RecycleSources(new[] { plan.SourcePath });
                    LegacyCleanupItemResult cleanupItem = cleanup.Items.Single();
                    report.Messages.Add("Legacy source cleanup: " + cleanupItem.Summary);
                }
                else
                {
                    report.Messages.Add("Legacy source cleanup: " + cleanupReason);
                }
            }
            Append(report.ToMultilineString());
            MessageBox.Show(this, report.Summary, report.Success ? Localization.T("message_success_title") : Localization.T("message_blocked_title"), MessageBoxButtons.OK, report.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenRepair()
    {
        try
        {
            string? key = _saveKey.SelectedIndex > 0 ? _saveKey.SelectedItem?.ToString() : null;
            string? version = _version.SelectedIndex > 0 ? _version.SelectedItem?.ToString() : null;
            MigrationPlan plan = _service.BuildPlan(_source.Text, _save.Text, _outputRoot.Text, key, version, allowUnverified: true);
            using var dialog = new RepairForm(_service, plan, _overwrite.Checked, _recycleSourceAfterSuccess.Checked);
            dialog.ShowDialog(this);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenDestination()
    {
        try
        {
            string target = !string.IsNullOrWhiteSpace(_outputFile.Text) ? Path.GetDirectoryName(_outputFile.Text)! : _outputRoot.Text;
            if (Directory.Exists(target)) System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("explorer.exe", target) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void Append(string text)
    {
        AppLog.Info(text.Replace(Environment.NewLine, " | "));
        if (_log.TextLength > 0) _log.AppendText(Environment.NewLine + Environment.NewLine);
        _log.AppendText(text);
    }

    private void ShowError(string message)
    {
        AppLog.Error(message);
        Append("ERROR: " + message);
        MessageBox.Show(this, message, Localization.T("message_error_title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
    }
}
