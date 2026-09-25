using System.Drawing;
using System.Windows.Forms;
using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class BulkMatchResolverForm : Form
{
    private readonly BulkConvertItem _item;
    private readonly IReadOnlyList<VanillaSaveCandidate> _candidates;
    private readonly string _dataRoot;
    private readonly Label _intro = new() { AutoSize = true, MaximumSize = new Size(1120, 0) };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoGenerateColumns = false,
        RowHeadersVisible = false,
        ScrollBars = ScrollBars.Both,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None
    };
    private readonly RichTextBox _details = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None };
    private readonly Button _use = new() { Tag = "bulk_match_use", Enabled = false };
    private readonly Button _browse = new() { Tag = "bulk_match_browse" };
    private readonly Button _cancel = new() { Tag = "bulk_match_cancel" };
    private readonly Button _help = new() { Tag = "help_button" };

    public string? SelectedSavePath { get; private set; }

    public BulkMatchResolverForm(BulkConvertItem item, IReadOnlyList<VanillaSaveCandidate> candidates, string dataRoot)
    {
        _item = item;
        _candidates = candidates;
        _dataRoot = dataRoot;

        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        UiStyling.FitWindowToWorkingArea(this, new Size(1280, 820), new Size(900, 620));
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }
        UiStyling.ApplyFormTheme(this);

        BuildGrid();
        BuildUi();
        _grid.SelectionChanged += (_, _) => UpdateDetails();
        _grid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) UseSelected(); };
        _use.Click += (_, _) => UseSelected();
        _browse.Click += (_, _) => BrowseSave();
        _cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        _help.Click += (_, _) => new HelpForm(HelpTopic.MatchResolver).ShowDialog(this);

        Localization.LanguageChanged += ApplyLanguage;
        UiSettings.AppearanceChanged += ApplyAppearance;
        Disposed += (_, _) =>
        {
            Localization.LanguageChanged -= ApplyLanguage;
            UiSettings.AppearanceChanged -= ApplyAppearance;
        };
        ApplyAccessibility();
        ApplyLanguage();
        Populate();
    }

    private void BuildGrid()
    {
        AddColumn("Score", 70);
        AddColumn("Match", 185);
        AddColumn("Save", 190);
        AddColumn("Player", 150);
        AddColumn("Group", 170);
        AddColumn("Last saved", 160);
        AddColumn("Game date", 160);
        AddColumn("Path", 360);
    }

    private void AddColumn(string name, int width) => _grid.Columns.Add(new DataGridViewTextBoxColumn
    {
        Name = name,
        HeaderText = name,
        Width = width,
        SortMode = DataGridViewColumnSortMode.Automatic
    });

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4, BackColor = UiStyling.PageBackground };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 66));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 34));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var introCard = new CardPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16) };
        UiStyling.StyleCard(introCard);
        introCard.Controls.Add(_intro);
        root.Controls.Add(introCard, 0, 0);

        var gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        UiStyling.StyleCard(gridCard);
        gridCard.Controls.Add(_grid);
        root.Controls.Add(gridCard, 0, 1);

        var detailsCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        UiStyling.StyleCard(detailsCard);
        detailsCard.Controls.Add(_details);
        root.Controls.Add(detailsCard, 0, 2);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.RightToLeft, BackColor = UiStyling.PageBackground };
        actions.Controls.Add(_cancel);
        actions.Controls.Add(_use);
        actions.Controls.Add(_browse);
        actions.Controls.Add(_help);
        root.Controls.Add(actions, 0, 3);
        Controls.Add(UiStyling.CreateScrollablePopupHost(root, new Size(1180, 760)));
        ApplyAppearance();
    }

    private void Populate()
    {
        _grid.Rows.Clear();
        foreach (VanillaSaveCandidate candidate in _candidates)
        {
            VanillaSaveInfo s = candidate.SaveInfo;
            string display = string.IsNullOrWhiteSpace(s.SaveDisplayName) ? s.SlotToken : s.SaveDisplayName;
            int row = _grid.Rows.Add(
                candidate.Score == 0 ? "" : candidate.Score.ToString(),
                candidate.MatchKind,
                display,
                s.PlayerName,
                s.GroupName,
                s.LastSave,
                s.GameDateTime,
                s.RelativeSavePath);
            _grid.Rows[row].Tag = candidate;
        }
        if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        _use.Enabled = _grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is VanillaSaveCandidate;
        if (!_use.Enabled)
        {
            _details.Text = Localization.T("bulk_match_no_candidate");
            return;
        }

        var candidate = (VanillaSaveCandidate)_grid.SelectedRows[0].Tag!;
        VanillaSaveInfo s = candidate.SaveInfo;
        _details.Text =
            $"{Localization.T("bulk_convert_match_col")}: {(candidate.Score == 0 ? Localization.T("bulk_match_manual_review") : candidate.Score + " / " + candidate.MatchKind)}{Environment.NewLine}" +
            s.ToIdentityMultilineString(Localization.T) + Environment.NewLine + Environment.NewLine +
            s.ToLegacyMatchMultilineString(Localization.T);
    }

    private void UseSelected()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not VanillaSaveCandidate candidate) return;
        SelectedSavePath = candidate.SaveInfo.FilePath;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void BrowseSave()
    {
        using var dialog = new OpenFileDialog
        {
            Title = Localization.T("dlg_select_save_title"),
            Filter = "Idol Manager save (*.json)|*.json|All files (*.*)|*.*",
            InitialDirectory = Directory.Exists(_dataRoot) ? _dataRoot : string.Empty
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            VanillaSaveReader.ReadForIdentification(dialog.FileName);
            SelectedSavePath = dialog.FileName;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("bulk_match_title");
        _intro.Text = Localization.Format("bulk_match_intro", _item.SourceRelativePath, _item.SourceSaveKey ?? Localization.T("not_stored"));
        ApplyTranslationsRecursive(this);
        string[] headers = { "bulk_match_col_score", "bulk_match_col_kind", "bulk_match_col_save", "bulk_match_col_player", "bulk_match_col_group", "bulk_match_col_last_saved", "bulk_match_col_game_date", "bulk_match_col_path" };
        for (int i = 0; i < headers.Length && i < _grid.Columns.Count; i++) _grid.Columns[i].HeaderText = Localization.T(headers[i]);
        UpdateDetails();
        ApplyAppearance();
        ApplyAccessibility();
    }

    private static void ApplyTranslationsRecursive(Control root)
    {
        if (root.Tag is string key && !string.IsNullOrWhiteSpace(key)) root.Text = Localization.T(key);
        foreach (Control child in root.Controls) ApplyTranslationsRecursive(child);
    }

    private void ApplyAppearance()
    {
        UiStyling.ApplyFormTheme(this);
        UiStyling.ApplyFontScaling(this);
        _grid.BackgroundColor = UiStyling.CardBackground;
        _grid.BorderStyle = BorderStyle.None;
        _grid.DefaultCellStyle.BackColor = Color.White;
        _grid.DefaultCellStyle.ForeColor = UiStyling.PrimaryText;
        _grid.DefaultCellStyle.SelectionBackColor = UiStyling.AccentLight;
        _grid.DefaultCellStyle.SelectionForeColor = UiStyling.PrimaryText;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = UiStyling.AccentLight;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UiStyling.PrimaryText;
        _grid.EnableHeadersVisualStyles = false;
        _details.BackColor = UiStyling.SurfaceReadOnly;
        _details.ForeColor = UiStyling.PrimaryText;
        UiStyling.StyleButton(_use, ButtonStyleKind.Primary);
        UiStyling.StyleButton(_browse, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_cancel, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_help, ButtonStyleKind.Secondary);
    }

    private void ApplyAccessibility()
    {
        static void Mark(Control control, string key)
        {
            string text = Localization.T(key);
            UiStyling.MarkAccessible(control, text, text);
        }
        Mark(_grid, "bulk_match_title");
        Mark(_details, "bulk_convert_details");
        Mark(_use, "bulk_match_use");
        Mark(_browse, "bulk_match_browse");
        Mark(_cancel, "bulk_match_cancel");
        Mark(_help, "help_button");
    }
}
