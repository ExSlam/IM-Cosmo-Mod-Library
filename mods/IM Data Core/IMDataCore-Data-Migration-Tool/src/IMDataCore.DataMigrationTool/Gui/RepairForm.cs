using System.Drawing;
using System.Windows.Forms;
using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class RepairForm : Form
{
    private readonly MigrationService _service;
    private readonly MigrationPlan _plan;
    private readonly RichTextBox _summary = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None };
    private readonly DataGridView _branches = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoGenerateColumns = false,
        RowHeadersVisible = false
    };
    private readonly RadioButton _salvage = new() { AutoSize = true, Tag = "repair_mode_salvage" };
    private readonly RadioButton _clean = new() { AutoSize = true, Tag = "repair_mode_clean" };
    private readonly CheckBox _overwrite = new() { AutoSize = true, Tag = "repair_overwrite" };
    private readonly CheckBox _recycleSource = new() { AutoSize = true, Tag = "bulk_cleanup_after_success" };
    private readonly Label _warning = new() { AutoSize = true, MaximumSize = new Size(1000, 0), Tag = "repair_custom_warning", ForeColor = UiStyling.SecondaryText };
    private readonly Button _validate = new() { Width = 150, Height = 38, Tag = "repair_validate" };
    private readonly Button _repair = new() { Width = 150, Height = 38, Tag = "repair_write" };
    private readonly Button _close = new() { Width = 110, Height = 38, Tag = "about_close" };
    private readonly Button _help = new() { Tag = "help_button" };
    private LegacyRepairAnalysis? _analysis;

    public RepairForm(MigrationService service, MigrationPlan plan, bool overwriteDefault, bool recycleSourceDefault = false)
    {
        _service = service;
        _plan = plan;
        _overwrite.Checked = overwriteDefault;
        _recycleSource.Checked = recycleSourceDefault;
        Text = Localization.T("repair_title");
        StartPosition = FormStartPosition.CenterParent;
        UiStyling.FitWindowToWorkingArea(this, new Size(1240, 820), new Size(960, 640));
        AutoScaleMode = AutoScaleMode.Dpi;
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }
        UiStyling.ApplyFormTheme(this);

        BuildGrid();
        BuildUi();
        ApplyAccessibility();
        Localization.LanguageChanged += ApplyLanguage;
        UiSettings.AppearanceChanged += ApplyAppearance;
        Disposed += (_, _) =>
        {
            Localization.LanguageChanged -= ApplyLanguage;
            UiSettings.AppearanceChanged -= ApplyAppearance;
        };
        Shown += (_, _) => LoadAnalysis();
        ApplyLanguage();
    }

    private void BuildGrid()
    {
        AddColumn("repair_col_branch", 70);
        AddColumn("repair_col_range", 160);
        AddColumn("repair_col_dates", 250);
        AddColumn("repair_col_events", 80);
        AddColumn("repair_col_eligible", 90);
        AddColumn("repair_col_future", 80);
        AddColumn("repair_col_matches", 90);
        AddColumn("repair_col_mismatches", 100);
        AddColumn("repair_col_score", 75);
        AddColumn("repair_col_confidence", 100);
    }

    private void AddColumn(string tag, int width)
    {
        _branches.Columns.Add(new DataGridViewTextBoxColumn { Name = tag, HeaderText = tag, Width = width, SortMode = DataGridViewColumnSortMode.NotSortable });
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(18), ColumnCount = 1, RowCount = 5, BackColor = UiStyling.PageBackground };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var summaryCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(16) };
        UiStyling.StyleCard(summaryCard);
        _summary.BackColor = UiStyling.SurfaceReadOnly;
        _summary.ForeColor = UiStyling.PrimaryText;
        summaryCard.Controls.Add(_summary);
        root.Controls.Add(summaryCard, 0, 0);

        var branchCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(12) };
        UiStyling.StyleCard(branchCard);
        branchCard.Controls.Add(_branches);
        root.Controls.Add(branchCard, 0, 1);

        var modesCard = new CardPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16) };
        UiStyling.StyleCard(modesCard);
        var modes = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false, BackColor = Color.Transparent };
        modes.Controls.Add(_salvage);
        modes.Controls.Add(_clean);
        modes.Controls.Add(_warning);
        modes.Controls.Add(_overwrite);
        modes.Controls.Add(_recycleSource);
        modesCard.Controls.Add(modes);
        root.Controls.Add(modesCard, 0, 2);

        var destination = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(1100, 0),
            Text = Localization.T("repair_destination") + Environment.NewLine + _plan.OutputSidecarPath,
            ForeColor = UiStyling.SecondaryText,
            Padding = new Padding(4, 4, 4, 4)
        };
        destination.AccessibleName = Localization.T("repair_destination");
        destination.AccessibleDescription = _plan.OutputSidecarPath;
        root.Controls.Add(destination, 0, 3);

        var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.LeftToRight, WrapContents = true, Padding = new Padding(0, 8, 0, 0), BackColor = UiStyling.PageBackground };
        _validate.Click += (_, _) => RunRepair(write: false);
        _repair.Click += (_, _) => RunRepair(write: true);
        _close.Click += (_, _) => Close();
        _help.Click += (_, _) => new HelpForm(HelpTopic.BranchRepair).ShowDialog(this);
        buttons.Controls.Add(_validate);
        buttons.Controls.Add(_repair);
        buttons.Controls.Add(_close);
        buttons.Controls.Add(_help);
        root.Controls.Add(buttons, 0, 4);
        Controls.Add(UiStyling.CreateScrollablePopupHost(root, new Size(1240, 800)));
    }

    private void LoadAnalysis()
    {
        try
        {
            UseWaitCursor = true;
            _analysis = _service.AnalyzeRepair(_plan);
            _summary.Text = _analysis.ToMultilineString();
            PopulateBranches(_analysis);
            if (_analysis.RecommendedBranchIndex.HasValue)
            {
                _salvage.Checked = true;
                SelectBranch(_analysis.RecommendedBranchIndex.Value);
            }
            else
            {
                _clean.Checked = true;
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Branch repair analysis failed.", ex);
            _summary.Text = ex.Message + Environment.NewLine + Environment.NewLine + "Diagnostic log: " + AppLog.SessionLogPath;
            _repair.Enabled = false;
            _validate.Enabled = false;
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void PopulateBranches(LegacyRepairAnalysis analysis)
    {
        _branches.Rows.Clear();
        foreach (LegacyRepairBranch branch in analysis.Branches)
        {
            int index = _branches.Rows.Add(
                branch.Index,
                $"{branch.StartEventId}-{branch.EndEventId}",
                $"{branch.StartGameDateTime} → {branch.EndGameDateTime}",
                branch.EventCount.ToString("N0"),
                branch.EligibleEventCount.ToString("N0"),
                branch.FutureEventCount.ToString("N0"),
                branch.DirectIdentityMatches.ToString("N0"),
                branch.DirectIdentityMismatches.ToString("N0"),
                branch.Score,
                branch.Confidence);
            _branches.Rows[index].Tag = branch;
        }
        if (_branches.Rows.Count > 0 && _branches.SelectedRows.Count == 0) _branches.Rows[0].Selected = true;
    }

    private void SelectBranch(int branchIndex)
    {
        foreach (DataGridViewRow row in _branches.Rows)
        {
            if (row.Tag is LegacyRepairBranch branch && branch.Index == branchIndex)
            {
                row.Selected = true;
                _branches.CurrentCell = row.Cells[0];
                break;
            }
        }
    }

    private void RunRepair(bool write)
    {
        if (_analysis is null) return;
        LegacyRepairMode mode = _clean.Checked ? LegacyRepairMode.CleanBaseline : LegacyRepairMode.ConservativeBranchSalvage;
        int? branchIndex = null;
        if (mode == LegacyRepairMode.ConservativeBranchSalvage)
        {
            if (_branches.SelectedRows.Count == 0 || _branches.SelectedRows[0].Tag is not LegacyRepairBranch branch)
            {
                MessageBox.Show(this, Localization.T("repair_select_branch"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            branchIndex = branch.Index;
            bool recommended = _analysis.RecommendedBranchIndex == branchIndex && (_analysis.RecommendationConfidence == "high" || _analysis.RecommendationConfidence == "medium");
            if (!recommended)
            {
                DialogResult answer = MessageBox.Show(
                    this,
                    Localization.T("repair_manual_warning"),
                    Localization.T("repair_title"),
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);
                if (answer != DialogResult.Yes) return;
            }
        }

        MigrationReport report = write
            ? _service.RepairAndMigrate(_plan, mode, branchIndex, _overwrite.Checked)
            : _service.ValidateRepairPlan(_plan, mode, branchIndex);

        if (write && report.Success && _recycleSource.Checked)
        {
            if (LegacyCleanupService.CanRecycleWholeSource(_service, _plan, out string cleanupReason))
            {
                LegacyCleanupBatchResult cleanup = LegacyCleanupService.RecycleSources(new[] { _plan.SourcePath });
                LegacyCleanupItemResult cleanupItem = cleanup.Items.Single();
                report.Messages.Add("Legacy source cleanup: " + cleanupItem.Summary);
            }
            else
            {
                report.Messages.Add("Legacy source cleanup: " + cleanupReason);
            }
        }

        _summary.Text = report.ToMultilineString();
        MessageBox.Show(
            this,
            report.Summary,
            report.Success ? Localization.T("title") : Localization.T("message_blocked_title"),
            MessageBoxButtons.OK,
            report.Success ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("repair_title");
        ApplyTranslationsRecursive(this);
        foreach (DataGridViewColumn column in _branches.Columns)
            column.HeaderText = Localization.T(column.Name);
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
        _summary.BackColor = UiStyling.SurfaceReadOnly;
        _summary.ForeColor = UiStyling.PrimaryText;
        _branches.BackgroundColor = UiStyling.CardBackground;
        _branches.BorderStyle = BorderStyle.None;
        _branches.DefaultCellStyle.BackColor = Color.White;
        _branches.DefaultCellStyle.ForeColor = UiStyling.PrimaryText;
        _branches.DefaultCellStyle.SelectionBackColor = UiStyling.AccentLight;
        _branches.DefaultCellStyle.SelectionForeColor = UiStyling.PrimaryText;
        _branches.ColumnHeadersDefaultCellStyle.BackColor = UiStyling.AccentLight;
        _branches.ColumnHeadersDefaultCellStyle.ForeColor = UiStyling.PrimaryText;
        _branches.EnableHeadersVisualStyles = false;
        UiStyling.StyleButton(_validate, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_repair, ButtonStyleKind.Primary);
        UiStyling.StyleButton(_close, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_help, ButtonStyleKind.Secondary);
    }

    private void ApplyAccessibility()
    {
        static void Mark(Control control, string key)
        {
            string text = Localization.T(key);
            UiStyling.MarkAccessible(control, text, text);
        }
        Mark(_summary, "repair_title");
        Mark(_branches, "repair_select_branch");
        Mark(_salvage, "repair_mode_salvage");
        Mark(_clean, "repair_mode_clean");
        Mark(_overwrite, "repair_overwrite");
        Mark(_recycleSource, "bulk_cleanup_after_success");
        Mark(_validate, "repair_validate");
        Mark(_repair, "repair_write");
        Mark(_close, "about_close");
        Mark(_help, "help_button");
    }
}
