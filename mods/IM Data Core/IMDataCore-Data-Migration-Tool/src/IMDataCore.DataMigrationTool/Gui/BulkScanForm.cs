using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class BulkScanForm : Form
{
    private readonly TextBox _root = new() { Dock = DockStyle.Fill };
    private readonly Button _browse = new() { Width = 120, Height = 34 };
    private readonly Button _scan = new() { Width = 150, Height = 36 };
    private readonly Button _bulkConvert = new() { Width = 150, Height = 36, Tag = "bulk_convert_main_button" };
    private readonly Button _exportCsv = new() { Width = 130, Height = 36, Enabled = false };
    private readonly Button _exportJson = new() { Width = 130, Height = 36, Enabled = false };
    private readonly Button _openSource = new() { Width = 150, Height = 36, Enabled = false };
    private readonly Button _close = new() { Width = 110, Height = 36 };
    private readonly Button _help = new() { Tag = "help_button" };
    private readonly Label _rootLabel = new() { AutoSize = true };
    private readonly Label _summary = new() { AutoSize = false, Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly Label _progressLabel = new() { AutoSize = false, Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleRight };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Style = ProgressBarStyle.Continuous };
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
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
        AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None,
        ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing
    };
    private readonly RichTextBox _details = new() { Dock = DockStyle.Fill, ReadOnly = true, Font = new Font("Consolas", 9F) };
    private readonly GroupBox _detailsGroup = new() { Dock = DockStyle.Fill, Padding = new Padding(8) };

    private BulkContaminationScanResult? _result;
    private SplitContainer? _resultsSplit;
    private RowStyle? _progressRowStyle;

    public BulkScanForm(string? initialRoot = null)
    {
        Text = Localization.T("bulk_title");
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        UiStyling.FitWindowToWorkingArea(this, new Size(1440, 860), new Size(1000, 660));
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }
        UiStyling.ApplyFormTheme(this);

        _root.Text = initialRoot ?? string.Empty;
        _browse.Click += BrowseRoot;
        _scan.Click += RunScan;
        _bulkConvert.Click += (_, _) => new BulkConvertForm(_root.Text).ShowDialog(this);
        _exportCsv.Click += (_, _) => ExportCsv();
        _exportJson.Click += (_, _) => ExportJson();
        _openSource.Click += (_, _) => OpenSelectedSource();
        _close.Click += (_, _) => Close();
        _help.Click += (_, _) => new HelpForm(HelpTopic.BulkScan).ShowDialog(this);
        _grid.SelectionChanged += (_, _) => UpdateDetails();

        BuildGridColumns();
        BuildUi();
        ApplyAccessibility();
        Localization.LanguageChanged += ApplyLanguage;
        UiSettings.AppearanceChanged += ApplyAppearance;
        Disposed += (_, _) => { Localization.LanguageChanged -= ApplyLanguage; UiSettings.AppearanceChanged -= ApplyAppearance; };
        ApplyLanguage();
        Shown += (_, _) => ApplyStableResultsSplit();
        Resize += (_, _) => ApplyStableResultsSplit();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 1,
            RowCount = 5,
            BackColor = UiStyling.PageBackground
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        _progressRowStyle = new RowStyle(SizeType.Absolute, 104);
        root.RowStyles.Add(_progressRowStyle);
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var pickerCard = new CardPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16), Margin = new Padding(0, 0, 0, 8) };
        UiStyling.StyleCard(pickerCard);
        var picker = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, BackColor = Color.Transparent };
        picker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        picker.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        picker.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        _rootLabel.Anchor = AnchorStyles.Left;
        _rootLabel.Margin = new Padding(0, 8, 12, 0);
        picker.Controls.Add(_rootLabel, 0, 0);
        picker.Controls.Add(_root, 1, 0);
        picker.Controls.Add(_browse, 2, 0);
        pickerCard.Controls.Add(picker);
        root.Controls.Add(pickerCard, 0, 0);

        var actionRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            Padding = new Padding(0, 4, 0, 6),
            Margin = new Padding(0),
            BackColor = UiStyling.PageBackground
        };
        actionRow.Controls.Add(_scan);
        actionRow.Controls.Add(_bulkConvert);
        actionRow.Controls.Add(_exportCsv);
        actionRow.Controls.Add(_exportJson);
        actionRow.Controls.Add(_openSource);
        actionRow.Controls.Add(_help);
        root.Controls.Add(actionRow, 0, 1);

        var progressCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(14), Margin = new Padding(0, 0, 0, 8) };
        UiStyling.StyleCard(progressCard);
        var progressPanel = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, BackColor = Color.Transparent };
        progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 68));
        progressPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 32));
        progressPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));
        progressPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        progressPanel.Controls.Add(_progress, 0, 0);
        progressPanel.Controls.Add(_progressLabel, 1, 0);
        progressPanel.Controls.Add(_summary, 0, 1);
        progressPanel.SetColumnSpan(_summary, 2);
        progressCard.Controls.Add(progressPanel);
        root.Controls.Add(progressCard, 0, 2);

        _resultsSplit = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Horizontal,
            SplitterWidth = 8,
            FixedPanel = FixedPanel.None,
            BackColor = UiStyling.PageBackground,
            Panel1MinSize = 180,
            Panel2MinSize = 120
        };
        _resultsSplit.HandleCreated += (_, _) => _resultsSplit.BeginInvoke(new Action(ApplyStableResultsSplit));
        _resultsSplit.Resize += (_, _) => ApplyStableResultsSplit();

        var gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10), Margin = new Padding(0) };
        UiStyling.StyleCard(gridCard);
        _grid.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
        gridCard.Controls.Add(_grid);
        _resultsSplit.Panel1.Controls.Add(gridCard);

        var detailsCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10), Margin = new Padding(0) };
        UiStyling.StyleCard(detailsCard);
        _detailsGroup.Controls.Add(_details);
        detailsCard.Controls.Add(_detailsGroup);
        _resultsSplit.Panel2.Controls.Add(detailsCard);
        root.Controls.Add(_resultsSplit, 0, 3);

        var bottom = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Padding = new Padding(0, 8, 0, 0),
            Margin = new Padding(0),
            BackColor = UiStyling.PageBackground
        };
        bottom.Controls.Add(_close);
        root.Controls.Add(bottom, 0, 4);

        Controls.Add(UiStyling.CreateScrollablePopupHost(root, new Size(1240, 780)));
        ApplyAppearance();
    }

    private void ApplyStableResultsSplit()
    {
        if (_resultsSplit is null || _resultsSplit.IsDisposed || _resultsSplit.ClientSize.Height <= 0) return;

        int total = _resultsSplit.ClientSize.Height;
        if (total < _resultsSplit.Panel1MinSize + _resultsSplit.Panel2MinSize + _resultsSplit.SplitterWidth + 2) return;
        int minTop = Math.Min(_resultsSplit.Panel1MinSize, Math.Max(0, total - _resultsSplit.Panel2MinSize - _resultsSplit.SplitterWidth));
        int maxTop = Math.Max(minTop, total - _resultsSplit.Panel2MinSize - _resultsSplit.SplitterWidth);
        int desired = (int)Math.Round(total * 0.64);
        int safe = Math.Clamp(desired, minTop, maxTop);
        if (safe >= 0 && safe <= maxTop && _resultsSplit.SplitterDistance != safe)
            _resultsSplit.SplitterDistance = safe;
    }

    private void UpdateStableMetrics()
    {
        if (_progressRowStyle is not null)
        {
            int line = TextRenderer.MeasureText("Ag", Font).Height;
            _progressRowStyle.Height = Math.Max(104, (line * 3) + 42);
        }
        ApplyStableResultsSplit();
    }

    private void ApplyAppearance()
    {
        UiStyling.ApplyFormTheme(this);
        UiStyling.ApplyFontScaling(this);
        UiStyling.StyleInput(_root);
        _details.BackColor = UiStyling.SurfaceReadOnly;
        _details.ForeColor = UiStyling.PrimaryText;
        _grid.BackgroundColor = UiStyling.CardBackground;
        _grid.BorderStyle = BorderStyle.None;
        _grid.DefaultCellStyle.BackColor = Color.White;
        _grid.DefaultCellStyle.ForeColor = UiStyling.PrimaryText;
        _grid.DefaultCellStyle.SelectionBackColor = UiStyling.AccentLight;
        _grid.DefaultCellStyle.SelectionForeColor = UiStyling.PrimaryText;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = UiStyling.AccentLight;
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = UiStyling.PrimaryText;
        _grid.EnableHeadersVisualStyles = false;
        UiStyling.StyleButton(_browse, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_scan, ButtonStyleKind.Primary);
        UiStyling.StyleButton(_bulkConvert, ButtonStyleKind.Accent);
        UiStyling.StyleButton(_exportCsv, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_exportJson, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_openSource, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_close, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_help, ButtonStyleKind.Secondary);
        UpdateStableMetrics();
    }

    private void ApplyAccessibility()
    {
        static void Mark(Control control, string key)
        {
            string text = Localization.T(key);
            UiStyling.MarkAccessible(control, text, text);
        }
        Mark(_root, "bulk_root_label");
        Mark(_browse, "btn_browse");
        Mark(_scan, "bulk_scan_button");
        Mark(_bulkConvert, "bulk_convert_main_button");
        Mark(_exportCsv, "bulk_export_csv");
        Mark(_exportJson, "bulk_export_json");
        Mark(_openSource, "bulk_open_source");
        Mark(_grid, "bulk_title");
        Mark(_details, "bulk_details_group");
        Mark(_close, "about_close");
        Mark(_help, "help_button");
    }

    private void BuildGridColumns()
    {
        AddTextColumn("Status", 210);
        AddTextColumn("Source", 240);
        AddTextColumn("Save key", 230);
        AddTextColumn("Events", 75);
        AddTextColumn("Branches", 75);
        AddTextColumn("Divergence event", 110);
        AddTextColumn("From date", 155);
        AddTextColumn("To date", 155);
        AddTextColumn("Rollback", 105);
        AddTextColumn("Checkpoint", 90);
    }

    private void AddTextColumn(string name, int width)
    {
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = name, Width = width, SortMode = DataGridViewColumnSortMode.Automatic });
    }

    private void ApplyLanguage()
    {
        Text = Localization.T("bulk_title");
        _rootLabel.Text = Localization.T("bulk_root_label");
        _browse.Text = Localization.T("btn_browse");
        _scan.Text = Localization.T("bulk_scan_button");
        _bulkConvert.Text = Localization.T("bulk_convert_main_button");
        _exportCsv.Text = Localization.T("bulk_export_csv");
        _exportJson.Text = Localization.T("bulk_export_json");
        _openSource.Text = Localization.T("bulk_open_source");
        _close.Text = Localization.T("about_close");
        _help.Text = Localization.T("help_button");
        _detailsGroup.Text = Localization.T("bulk_details_group");

        string[] headers =
        {
            "bulk_col_status", "bulk_col_source", "bulk_col_save_key", "bulk_col_events", "bulk_col_branches",
            "bulk_col_divergence_event", "bulk_col_from_date", "bulk_col_to_date", "bulk_col_rollback", "bulk_col_checkpoint"
        };
        for (int i = 0; i < headers.Length && i < _grid.Columns.Count; i++) _grid.Columns[i].HeaderText = Localization.T(headers[i]);

        if (_result is not null) PopulateGrid(_result);
        else _summary.Text = Localization.T("bulk_summary_not_scanned");
        ApplyAppearance();
        ApplyAccessibility();
    }

    private void BrowseRoot(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = Localization.T("bulk_root_dialog"),
            SelectedPath = Directory.Exists(_root.Text) ? _root.Text : string.Empty
        };
        if (dialog.ShowDialog(this) == DialogResult.OK) _root.Text = dialog.SelectedPath;
    }

    private async void RunScan(object? sender, EventArgs e)
    {
        string requestedRoot = _root.Text.Trim();
        if (!Directory.Exists(requestedRoot))
        {
            MessageBox.Show(this, Localization.T("bulk_root_missing"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        _grid.Rows.Clear();
        _details.Clear();
        _result = null;
        _progress.Value = 0;
        _progress.Maximum = 1;
        _summary.Text = Localization.T("bulk_scanning");

        IProgress<BulkScanProgress> progress = new Progress<BulkScanProgress>(p =>
        {
            _progress.Maximum = Math.Max(1, p.TotalFiles);
            _progress.Value = Math.Min(_progress.Maximum, Math.Max(0, p.CurrentFile));
            _progressLabel.Text = Localization.Format("bulk_progress", p.CurrentFile, p.TotalFiles, Path.GetFileName(Path.GetDirectoryName(p.SourcePath) ?? p.SourcePath));
        });

        try
        {
            _result = await Task.Run(() => LegacyContaminationScanner.ScanRoot(requestedRoot, p => progress.Report(p)));
            PopulateGrid(_result);
        }
        catch (Exception ex)
        {
            AppLog.Error("Bulk contamination scan failed.", ex);
            MessageBox.Show(this, ex.Message, Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            _summary.Text = ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PopulateGrid(BulkContaminationScanResult result)
    {
        _grid.Rows.Clear();
        foreach (LegacyContaminationEntry entry in result.Entries
                     .OrderByDescending(x => x.IsContaminated)
                     .ThenBy(x => x.SourceRelativePath, StringComparer.OrdinalIgnoreCase)
                     .ThenBy(x => x.SaveKey, StringComparer.OrdinalIgnoreCase))
        {
            LegacyDivergencePoint? primary = entry.PrimaryDivergence;
            int rowIndex = _grid.Rows.Add(
                LocalizedStatus(entry),
                entry.SourceRelativePath,
                entry.SaveKey,
                entry.EventCount.ToString("N0"),
                entry.BranchCount.ToString("N0"),
                primary?.DivergenceEventId.ToString() ?? "",
                primary?.PreviousGameDateTime ?? "",
                primary?.DivergenceGameDateTime ?? "",
                primary is null ? "" : LegacyDivergencePoint.FormatRollback(primary.RollbackSeconds),
                entry.HasCheckpointSupport ? Localization.T("bulk_yes") : Localization.T("bulk_no"));
            _grid.Rows[rowIndex].Tag = entry;
        }

        foreach (BulkScanError error in result.Errors)
        {
            int rowIndex = _grid.Rows.Add(Localization.T("bulk_status_error"), error.SourceRelativePath, "", "", "", "", "", "", "", "");
            _grid.Rows[rowIndex].Tag = error;
        }

        _summary.Text = Localization.Format(
            "bulk_summary",
            result.FilesDiscovered,
            result.FilesScanned,
            result.FilesFailed,
            result.Entries.Count,
            result.ContaminatedEntries,
            result.CleanEntries);
        _exportCsv.Enabled = true;
        _exportJson.Enabled = true;
        if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
        UpdateDetails();
    }

    private static string LocalizedStatus(LegacyContaminationEntry entry)
    {
        if (entry.IsContaminated) return Localization.T(entry.HasCheckpointSupport ? "bulk_status_reset_checkpoint" : "bulk_status_contaminated");
        if (entry.InvalidDateCount > 0) return Localization.T("bulk_status_invalid_dates");
        return Localization.T("bulk_status_clean");
    }

    private void UpdateDetails()
    {
        _openSource.Enabled = false;
        if (_grid.SelectedRows.Count == 0)
        {
            _details.Clear();
            return;
        }

        object? tag = _grid.SelectedRows[0].Tag;
        if (tag is LegacyContaminationEntry entry)
        {
            _details.Text = BuildLocalizedDetails(entry);
            _openSource.Enabled = true;
        }
        else if (tag is BulkScanError error)
        {
            _details.Text = Localization.T("bulk_status_error") + Environment.NewLine + error.SourcePath + Environment.NewLine + error.Message;
            _openSource.Enabled = true;
        }
    }

    private static string BuildLocalizedDetails(LegacyContaminationEntry entry)
    {
        var lines = new List<string>
        {
            Localization.T("bulk_col_status") + ": " + LocalizedStatus(entry),
            Localization.T("bulk_col_source") + ": " + entry.SourcePath,
            Localization.T("bulk_col_save_key") + ": " + entry.SaveKey,
            Localization.T("bulk_col_events") + $": {entry.EventCount:N0} ({entry.ParsedEventCount:N0} parsed, {entry.InvalidDateCount:N0} invalid date rows)",
            Localization.T("bulk_col_branches") + ": " + entry.BranchCount,
            Localization.T("bulk_col_checkpoint") + ": " + (entry.HasCheckpointSupport ? Localization.T("bulk_yes") : Localization.T("bulk_no"))
        };

        if (!string.IsNullOrWhiteSpace(entry.FirstGameDateTime))
            lines.Add($"Timeline: event {entry.FirstParsedEventId} {entry.FirstGameDateTime} -> event {entry.LastParsedEventId} {entry.LastGameDateTime}");

        if (entry.Divergences.Count > 0)
        {
            lines.Add("");
            lines.Add(Localization.T("bulk_primary_divergence") + ": " + entry.PrimaryDivergence);
            lines.Add(Localization.T("bulk_all_divergences") + ":");
            foreach (LegacyDivergencePoint point in entry.Divergences) lines.Add("  " + point);
        }
        return string.Join(Environment.NewLine, lines);
    }

    private void ExportCsv()
    {
        if (_result is null) return;
        using var dialog = new SaveFileDialog { Filter = "CSV (*.csv)|*.csv|All files (*.*)|*.*", FileName = "imdatacore-contamination-scan.csv" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        LegacyContaminationScanner.WriteCsv(_result, dialog.FileName);
        MessageBox.Show(this, Localization.Format("bulk_exported", dialog.FileName), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportJson()
    {
        if (_result is null) return;
        using var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json|All files (*.*)|*.*", FileName = "imdatacore-contamination-scan.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        LegacyContaminationScanner.WriteJson(_result, dialog.FileName);
        MessageBox.Show(this, Localization.Format("bulk_exported", dialog.FileName), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void OpenSelectedSource()
    {
        if (_grid.SelectedRows.Count == 0) return;
        string? path = _grid.SelectedRows[0].Tag switch
        {
            LegacyContaminationEntry entry => entry.SourcePath,
            BulkScanError error => error.SourcePath,
            _ => null
        };
        if (string.IsNullOrWhiteSpace(path)) return;
        string? dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir) && Directory.Exists(dir)) Process.Start(new ProcessStartInfo("explorer.exe", dir) { UseShellExecute = true });
    }

    private void SetBusy(bool busy)
    {
        _scan.Enabled = !busy;
        _browse.Enabled = !busy;
        _root.Enabled = !busy;
        _exportCsv.Enabled = !busy && _result is not null;
        _exportJson.Enabled = !busy && _result is not null;
        UseWaitCursor = busy;
    }
}
