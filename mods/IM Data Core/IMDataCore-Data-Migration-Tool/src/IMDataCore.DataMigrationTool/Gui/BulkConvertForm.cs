using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using IMDataCore.DataMigrationTool.Migration;

namespace IMDataCore.DataMigrationTool.Gui;

internal sealed class BulkConvertForm : Form
{
    private readonly TextBox _legacyRoot = new() { Dock = DockStyle.Fill };
    private readonly TextBox _dataRoot = new() { Dock = DockStyle.Fill };
    private readonly TextBox _outputRoot = new() { Dock = DockStyle.Fill };
    private readonly Button _browseLegacy = new() { Tag = "btn_browse" };
    private readonly Button _browseData = new() { Tag = "btn_browse" };
    private readonly Button _browseOutput = new() { Tag = "btn_browse" };
    private readonly Label _legacyLabel = new() { AutoSize = true, Tag = "bulk_convert_legacy_root" };
    private readonly Label _dataLabel = new() { AutoSize = true, Tag = "bulk_convert_data_root" };
    private readonly Label _outputLabel = new() { AutoSize = true, Tag = "bulk_convert_output_root" };
    private readonly Label _policyLabel = new() { AutoSize = true, Tag = "bulk_convert_policy" };
    private readonly ComboBox _policy = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 360 };
    private readonly CheckBox _overwrite = new() { AutoSize = true, Tag = "bulk_convert_overwrite" };
    private readonly CheckBox _recycleAfterSuccess = new() { AutoSize = true, MaximumSize = new Size(920, 0), Tag = "bulk_cleanup_after_success" };
    private readonly Button _planButton = new() { Tag = "bulk_convert_plan" };
    private readonly Button _validateButton = new() { Tag = "bulk_convert_validate", Enabled = false };
    private readonly Button _convertButton = new() { Tag = "bulk_convert_run", Enabled = false };
    private readonly Button _selectReady = new() { Tag = "bulk_convert_select_ready", Enabled = false };
    private readonly Button _clearSelection = new() { Tag = "bulk_convert_clear", Enabled = false };
    private readonly Button _resolveMatch = new() { Tag = "bulk_match_resolve", Enabled = false };
    private readonly Button _exportPlan = new() { Tag = "bulk_convert_export_plan", Enabled = false };
    private readonly Button _exportResult = new() { Tag = "bulk_convert_export_result", Enabled = false };
    private readonly Button _openOutput = new() { Tag = "btn_open_destination" };
    private readonly Button _recycleSuccessful = new() { Tag = "bulk_cleanup_successful_button", Enabled = false };
    private readonly Button _recycleOrphans = new() { Tag = "bulk_cleanup_orphans_button", Enabled = false };
    private readonly Button _close = new() { Tag = "about_close" };
    private readonly Button _help = new() { Tag = "help_button" };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill };
    private readonly Label _progressLabel = new() { AutoSize = true };
    private readonly Label _summary = new() { AutoSize = true, MaximumSize = new Size(1300, 0) };
    private readonly DataGridView _grid = new()
    {
        Dock = DockStyle.Fill,
        AllowUserToAddRows = false,
        AllowUserToDeleteRows = false,
        MultiSelect = false,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        AutoGenerateColumns = false,
        RowHeadersVisible = false
    };
    private readonly RichTextBox _details = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, Font = new Font("Consolas", 9F) };
    private readonly GroupBox _detailsGroup = new() { Dock = DockStyle.Fill, Tag = "bulk_convert_details" };

    private readonly BulkConversionService _service = new();
    private BulkConvertPlan? _plan;
    private BulkConvertRunResult? _lastResult;

    public BulkConvertForm(string? initialLegacyRoot = null)
    {
        StartPosition = FormStartPosition.CenterParent;
        AutoScaleMode = AutoScaleMode.Dpi;
        UiStyling.FitWindowToWorkingArea(this, new Size(1560, 920), new Size(1120, 720));
        try { Icon = new Icon(Path.Combine(AppContext.BaseDirectory, "Assets", "data_migration_tool.ico")); } catch { }
        UiStyling.ApplyFormTheme(this);

        _legacyRoot.Text = initialLegacyRoot ?? string.Empty;
        _dataRoot.Text = VanillaSaveReader.DefaultDataDirectory();
        TryResolveDefaultOutputRoot();

        _browseLegacy.Click += (_, _) => BrowseFolder(_legacyRoot, Localization.T("bulk_convert_legacy_dialog"));
        _browseData.Click += (_, _) => { BrowseFolder(_dataRoot, Localization.T("bulk_convert_data_dialog")); TryResolveDefaultOutputRoot(); };
        _browseOutput.Click += (_, _) => BrowseFolder(_outputRoot, Localization.T("bulk_convert_output_dialog"));
        _planButton.Click += async (_, _) => await BuildPlanAsync();
        _validateButton.Click += async (_, _) => await RunAsync(dryRun: true);
        _convertButton.Click += async (_, _) => await RunAsync(dryRun: false);
        _selectReady.Click += (_, _) => SetReadySelection(true);
        _clearSelection.Click += (_, _) => SetReadySelection(false);
        _resolveMatch.Click += (_, _) => ResolveSelectedMatch();
        _exportPlan.Click += (_, _) => ExportPlan();
        _exportResult.Click += (_, _) => ExportResult();
        _openOutput.Click += (_, _) => OpenOutput();
        _recycleSuccessful.Click += (_, _) => RecycleSuccessfulSources();
        _recycleOrphans.Click += (_, _) => RecycleOrphanedSources();
        _close.Click += (_, _) => Close();
        _help.Click += (_, _) => new HelpForm(HelpTopic.BulkConvert).ShowDialog(this);
        _grid.SelectionChanged += (_, _) => { UpdateDetails(); UpdateResolveButton(); };
        _grid.CellValueChanged += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0 && _grid.Rows[e.RowIndex].Tag is BulkConvertItem item)
                item.Selected = Convert.ToBoolean(_grid.Rows[e.RowIndex].Cells[0].Value ?? false);
        };
        _grid.CurrentCellDirtyStateChanged += (_, _) =>
        {
            if (_grid.IsCurrentCellDirty) _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        };

        BuildGridColumns();
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
        AppLog.Info("Bulk convert window opened.");
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6, BackColor = UiStyling.PageBackground };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 36));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var optionsCard = new CardPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(16) };
        UiStyling.StyleCard(optionsCard);
        var options = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 3, BackColor = Color.Transparent };
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        options.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        AddPickerRow(options, _legacyLabel, _legacyRoot, _browseLegacy);
        AddPickerRow(options, _dataLabel, _dataRoot, _browseData);
        AddPickerRow(options, _outputLabel, _outputRoot, _browseOutput);
        int policyRow = options.RowCount++;
        _policyLabel.Anchor = AnchorStyles.Left;
        options.Controls.Add(_policyLabel, 0, policyRow);
        options.Controls.Add(_policy, 1, policyRow);
        options.SetColumnSpan(_policy, 2);
        int overwriteRow = options.RowCount++;
        options.Controls.Add(new Label { AutoSize = true, BackColor = Color.Transparent }, 0, overwriteRow);
        options.Controls.Add(_overwrite, 1, overwriteRow);
        options.SetColumnSpan(_overwrite, 2);
        int cleanupRow = options.RowCount++;
        options.Controls.Add(new Label { AutoSize = true, BackColor = Color.Transparent }, 0, cleanupRow);
        options.Controls.Add(_recycleAfterSuccess, 1, cleanupRow);
        options.SetColumnSpan(_recycleAfterSuccess, 2);
        optionsCard.Controls.Add(options);
        root.Controls.Add(optionsCard, 0, 0);

        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, FlowDirection = FlowDirection.LeftToRight, BackColor = UiStyling.PageBackground };
        actions.Controls.Add(_planButton);
        actions.Controls.Add(_validateButton);
        actions.Controls.Add(_convertButton);
        actions.Controls.Add(_selectReady);
        actions.Controls.Add(_clearSelection);
        actions.Controls.Add(_resolveMatch);
        actions.Controls.Add(_exportPlan);
        actions.Controls.Add(_exportResult);
        actions.Controls.Add(_openOutput);
        actions.Controls.Add(_recycleSuccessful);
        actions.Controls.Add(_recycleOrphans);
        actions.Controls.Add(_help);
        root.Controls.Add(actions, 0, 1);

        var progressCard = new CardPanel { Dock = DockStyle.Top, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(14) };
        UiStyling.StyleCard(progressCard);
        var progress = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, BackColor = Color.Transparent };
        progress.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 75));
        progress.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        progress.Controls.Add(_progress, 0, 0);
        progress.Controls.Add(_progressLabel, 1, 0);
        progress.Controls.Add(_summary, 0, 1);
        progress.SetColumnSpan(_summary, 2);
        progressCard.Controls.Add(progress);
        root.Controls.Add(progressCard, 0, 2);

        var gridCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        UiStyling.StyleCard(gridCard);
        gridCard.Controls.Add(_grid);
        root.Controls.Add(gridCard, 0, 3);

        var detailsCard = new CardPanel { Dock = DockStyle.Fill, Padding = new Padding(10) };
        UiStyling.StyleCard(detailsCard);
        _detailsGroup.Controls.Add(_details);
        detailsCard.Controls.Add(_detailsGroup);
        root.Controls.Add(detailsCard, 0, 4);

        var bottom = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, FlowDirection = FlowDirection.RightToLeft, BackColor = UiStyling.PageBackground };
        bottom.Controls.Add(_close);
        root.Controls.Add(bottom, 0, 5);
        Controls.Add(UiStyling.CreateScrollablePopupHost(root, new Size(1320, 840)));
        ApplyAppearance();
    }

    private static void AddPickerRow(TableLayoutPanel table, Label label, Control input, Control button)
    {
        int row = table.RowCount++;
        label.Anchor = AnchorStyles.Left;
        label.Margin = new Padding(3, 9, 12, 3);
        table.Controls.Add(label, 0, row);
        input.Margin = new Padding(3, 5, 3, 5);
        table.Controls.Add(input, 1, row);
        button.Margin = new Padding(6, 5, 3, 5);
        table.Controls.Add(button, 2, row);
    }

    private void BuildGridColumns()
    {
        _grid.Columns.Add(new DataGridViewCheckBoxColumn { Name = "Selected", Width = 58 });
        AddColumn("Status", 190);
        AddColumn("Action", 150);
        AddColumn("Source", 240);
        AddColumn("Save key", 210);
        AddColumn("Match", 105);
        AddColumn("Vanilla save", 260);
        AddColumn("Events", 85);
        AddColumn("Branches", 80);
        AddColumn("Output", 300);
    }

    private void AddColumn(string name, int width) =>
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = name, HeaderText = name, Width = width, SortMode = DataGridViewColumnSortMode.Automatic, ReadOnly = true });

    private async Task BuildPlanAsync()
    {
        if (!Directory.Exists(_legacyRoot.Text.Trim()))
        {
            MessageBox.Show(this, Localization.T("bulk_convert_missing_legacy"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (!Directory.Exists(_dataRoot.Text.Trim()))
        {
            MessageBox.Show(this, Localization.T("bulk_convert_missing_data"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true);
        _grid.Rows.Clear();
        _details.Clear();
        _summary.Text = Localization.T("bulk_convert_planning");
        _progress.Value = 0;
        _progress.Maximum = 1;
        _plan = null;
        _lastResult = null;
        _exportResult.Enabled = false;

        IProgress<BulkConvertProgress> progress = new Progress<BulkConvertProgress>(p =>
        {
            _progress.Maximum = Math.Max(1, p.Total);
            _progress.Value = Math.Min(_progress.Maximum, Math.Max(0, p.Current));
            _progressLabel.Text = p.Message;
        });

        try
        {
            BulkConvertContaminatedPolicy policy = PolicyFromIndex();
            string legacyRoot = _legacyRoot.Text.Trim();
            string dataRoot = _dataRoot.Text.Trim();
            string outputRoot = _outputRoot.Text.Trim();
            _plan = await Task.Run(() => _service.BuildPlan(legacyRoot, dataRoot, outputRoot, policy, p => progress.Report(p)));
            PopulatePlan();
            AppLog.Info($"Bulk convert plan built: {_plan.Items.Count} stream(s), {_plan.ReadyCount} ready, {_plan.SkippedCount} skipped.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Bulk convert planning failed.", ex);
            string localized = LocalizedLogFormatter.TranslateRuntimeMessage(ex.Message);
            MessageBox.Show(this, localized, Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            _summary.Text = localized;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void PopulatePlan()
    {
        if (_plan is null) return;
        _grid.Rows.Clear();
        foreach (BulkConvertItem item in _plan.Items
                     .OrderByDescending(x => x.IsReady)
                     .ThenByDescending(x => x.IsContaminated)
                     .ThenBy(x => x.SourceRelativePath, StringComparer.OrdinalIgnoreCase))
        {
            int row = _grid.Rows.Add(
                item.Selected && item.IsReady,
                item.Status,
                item.ActionDisplay,
                item.SourceRelativePath,
                item.SourceSaveKey ?? "",
                item.MatchScore == 0 ? "" : item.MatchScore + " / " + item.MatchKind,
                item.VanillaRelativePath,
                item.LegacyEventCount.ToString("N0"),
                item.BranchCount.ToString(),
                item.OutputSidecarPath);
            _grid.Rows[row].Tag = item;
            _grid.Rows[row].Cells[0].ReadOnly = !item.IsReady;
        }

        _summary.Text = Localization.Format(
            "bulk_convert_summary",
            _plan.LegacyFilesDiscovered,
            _plan.Items.Count,
            _plan.ReadyCount,
            _plan.SkippedCount,
            _plan.ContaminatedCount,
            _plan.VanillaSavesRecognized);
        _validateButton.Enabled = _plan.ReadyCount > 0;
        _convertButton.Enabled = _plan.ReadyCount > 0;
        _selectReady.Enabled = _plan.ReadyCount > 0;
        _clearSelection.Enabled = _plan.ReadyCount > 0;
        _exportPlan.Enabled = true;
        _recycleOrphans.Enabled = BulkConversionService.HasStrictOrphanSources(_plan);
        if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;
        UpdateDetails();
        UpdateResolveButton();
    }

    private async Task RunAsync(bool dryRun)
    {
        if (_plan is null) return;
        SyncSelectionFromGrid();
        int count = _plan.SelectedReadyCount;
        if (count == 0)
        {
            MessageBox.Show(this, Localization.T("bulk_convert_none_selected"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!dryRun)
        {
            string confirmation = Localization.Format("bulk_convert_confirm", count, _overwrite.Checked ? Localization.T("bulk_yes") : Localization.T("bulk_no"));
            if (_recycleAfterSuccess.Checked) confirmation += Environment.NewLine + Environment.NewLine + Localization.T("bulk_cleanup_convert_warning");
            if (MessageBox.Show(this, confirmation, Localization.T("title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        }

        SetBusy(true);
        IProgress<BulkConvertProgress> progress = new Progress<BulkConvertProgress>(p =>
        {
            _progress.Maximum = Math.Max(1, p.Total);
            _progress.Value = Math.Min(_progress.Maximum, Math.Max(0, p.Current));
            _progressLabel.Text = p.Message;
        });

        try
        {
            _lastResult = await Task.Run(() => _service.Run(_plan, _overwrite.Checked, dryRun, p => progress.Report(p), recycleAfterSuccess: !dryRun && _recycleAfterSuccess.Checked));
            _exportResult.Enabled = true;
            _recycleSuccessful.Enabled = !dryRun && _lastResult.Items.Any(x => x.Success && !x.Skipped && !x.LegacyCleanupSucceeded);
            ApplyRunResultToGrid(_lastResult);
            _summary.Text = Localization.Format(
                dryRun ? "bulk_convert_validation_summary" : "bulk_convert_run_summary",
                _lastResult.SuccessCount,
                _lastResult.SkippedCount,
                _lastResult.FailedCount);
            MessageBox.Show(this, _summary.Text, Localization.T("title"), MessageBoxButtons.OK,
                _lastResult.FailedCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            AppLog.Error("Bulk conversion execution failed.", ex);
            MessageBox.Show(this, LocalizedLogFormatter.TranslateRuntimeMessage(ex.Message), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void ApplyRunResultToGrid(BulkConvertRunResult result)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is not BulkConvertItem item) continue;
            BulkConvertRunItem? run = result.Items.LastOrDefault(x =>
                string.Equals(x.SourcePath, item.SourcePath, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.SourceSaveKey ?? "", item.SourceSaveKey ?? "", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.OutputSidecarPath, item.OutputSidecarPath, StringComparison.OrdinalIgnoreCase));
            if (run is null) continue;
            row.Cells[1].Value = run.Skipped ? Localization.T("bulk_convert_status_existing_skipped") : run.Success ? Localization.T("bulk_convert_status_success") : Localization.T("bulk_convert_status_failed");
            if (run.LegacyCleanupSucceeded) row.Cells[1].Value = Convert.ToString(row.Cells[1].Value) + " + Recycle Bin";
            item.Status = Convert.ToString(row.Cells[1].Value) ?? item.Status;
            item.Details = run.Summary;
        }
        UpdateDetails();
    }

    private void UpdateDetails()
    {
        if (_grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not BulkConvertItem item)
        {
            _details.Clear();
            return;
        }

        _details.Text =
            $"{Localization.T("bulk_col_status")}: {item.Status}{Environment.NewLine}" +
            $"{Localization.T("bulk_convert_action_col")}: {item.ActionDisplay}{Environment.NewLine}" +
            $"{Localization.T("bulk_col_source")}: {item.SourcePath}{Environment.NewLine}" +
            $"{Localization.T("bulk_col_save_key")}: {item.SourceSaveKey}{Environment.NewLine}" +
            $"{Localization.T("bulk_convert_vanilla_col")}: {item.VanillaSavePath}{Environment.NewLine}" +
            $"{Localization.T("bulk_convert_match_col")}: {item.MatchScore} / {item.MatchKind}{Environment.NewLine}" +
            $"{Localization.T("bulk_col_events")}: {item.LegacyEventCount:N0}{Environment.NewLine}" +
            $"{Localization.T("bulk_col_branches")}: {item.BranchCount}{Environment.NewLine}" +
            $"{Localization.T("bulk_convert_output_col")}: {item.OutputSidecarPath}{Environment.NewLine}" +
            (item.RepairBranchIndex.HasValue ? $"Repair branch: {item.RepairBranchIndex} ({item.RepairConfidence}){Environment.NewLine}" : "") +
            Environment.NewLine + LocalizedLogFormatter.TranslateMultiline(item.Details);
    }

    private void UpdateResolveButton()
    {
        _resolveMatch.Enabled = _plan is not null && _grid.SelectedRows.Count > 0 && _grid.SelectedRows[0].Tag is BulkConvertItem;
    }

    private void ResolveSelectedMatch()
    {
        if (_plan is null || _grid.SelectedRows.Count == 0 || _grid.SelectedRows[0].Tag is not BulkConvertItem item) return;
        try
        {
            IReadOnlyList<VanillaSaveCandidate> candidates = _service.GetCandidates(_plan, item);
            using var dialog = new BulkMatchResolverForm(item, candidates, _plan.DataRoot);
            if (dialog.ShowDialog(this) != DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedSavePath)) return;

            BulkConvertItem replacement = _service.ResolveManualMatch(_plan, item, dialog.SelectedSavePath, PolicyFromIndex());
            if (replacement.IsReady)
            {
                List<BulkConvertItem> conflicts = _plan.Items
                    .Where(x => !ReferenceEquals(x, item) && x.IsReady && string.Equals(x.OutputSidecarPath, replacement.OutputSidecarPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                if (conflicts.Count > 0)
                {
                    string message = Localization.Format("bulk_match_collision_confirm", conflicts.Count, replacement.VanillaRelativePath);
                    if (MessageBox.Show(this, message, Localization.T("title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
                    foreach (BulkConvertItem conflict in conflicts)
                    {
                        conflict.Selected = false;
                        conflict.Action = BulkConvertAction.Skip;
                        conflict.Status = Localization.T("bulk_match_superseded_manual");
                        conflict.Details += Environment.NewLine + Localization.T("bulk_match_superseded_manual_details");
                    }
                }
            }

            int index = _plan.Items.IndexOf(item);
            if (index >= 0) _plan.Items[index] = replacement;
            PopulatePlan();
            AppLog.Info($"Manual bulk match resolved: {replacement.SourcePath} -> {replacement.VanillaSavePath}; ready={replacement.IsReady}; action={replacement.Action}");
        }
        catch (Exception ex)
        {
            AppLog.Error("Manual bulk match resolution failed.", ex);
            MessageBox.Show(this, LocalizedLogFormatter.TranslateRuntimeMessage(ex.Message), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RecycleSuccessfulSources()
    {
        if (_lastResult is null) return;
        List<string> eligibleSources = _plan is null ? new List<string>() : BulkConversionService.GetSuccessfulCleanupSourcePaths(_plan, _lastResult).ToList();
        if (eligibleSources.Count == 0)
        {
            MessageBox.Show(this, Localization.T("bulk_cleanup_none_successful"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string message = Localization.Format("bulk_cleanup_confirm_successful", eligibleSources.Count);
        if (MessageBox.Show(this, message, Localization.T("title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        LegacyCleanupBatchResult cleanup = _service.RecycleSuccessfulSources(_plan!, _lastResult);
        MessageBox.Show(this, Localization.Format("bulk_cleanup_done", cleanup.SuccessCount, cleanup.FailedCount, cleanup.RecycledFileCount), Localization.T("title"),
            MessageBoxButtons.OK, cleanup.FailedCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        _recycleSuccessful.Enabled = _plan is not null && _lastResult.Items.Any(x => x.Success && !x.Skipped && !x.LegacyCleanupSucceeded);
        UpdateDetails();
    }

    private void RecycleOrphanedSources()
    {
        if (_plan is null) return;
        List<BulkConvertItem> orphans = _plan.Items.Where(BulkConversionService.IsStrictOrphan).ToList();
        List<string> orphanSources = BulkConversionService.GetStrictOrphanSourcePaths(_plan).ToList();
        if (orphanSources.Count == 0)
        {
            MessageBox.Show(this, Localization.T("bulk_cleanup_none_orphans"), Localization.T("title"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        string message = Localization.Format("bulk_cleanup_confirm_orphans", orphanSources.Count);
        if (MessageBox.Show(this, message, Localization.T("title"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;

        LegacyCleanupBatchResult cleanup = _service.RecycleOrphanedSources(_plan);
        foreach (BulkConvertItem orphan in orphans)
        {
            LegacyCleanupItemResult? item = cleanup.Items.FirstOrDefault(x => string.Equals(Path.GetFullPath(x.SourcePath), Path.GetFullPath(orphan.SourcePath), StringComparison.OrdinalIgnoreCase));
            if (item is null) continue;
            if (item.Success) orphan.Status = Localization.T("bulk_cleanup_status_orphan_recycled");
            orphan.Details += Environment.NewLine + (item.Success ? item.Summary : Localization.T("bulk_cleanup_status_failed") + ": " + item.Summary);
        }
        PopulatePlan();
        MessageBox.Show(this, Localization.Format("bulk_cleanup_done", cleanup.SuccessCount, cleanup.FailedCount, cleanup.RecycledFileCount), Localization.T("title"),
            MessageBoxButtons.OK, cleanup.FailedCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
    }

    private void SetReadySelection(bool selected)
    {
        foreach (DataGridViewRow row in _grid.Rows)
        {
            if (row.Tag is not BulkConvertItem item || !item.IsReady) continue;
            item.Selected = selected;
            row.Cells[0].Value = selected;
        }
    }

    private void SyncSelectionFromGrid()
    {
        foreach (DataGridViewRow row in _grid.Rows)
            if (row.Tag is BulkConvertItem item && item.IsReady)
                item.Selected = Convert.ToBoolean(row.Cells[0].Value ?? false);
    }

    private BulkConvertContaminatedPolicy PolicyFromIndex() => _policy.SelectedIndex switch
    {
        1 => BulkConvertContaminatedPolicy.Skip,
        2 => BulkConvertContaminatedPolicy.CleanBaseline,
        _ => BulkConvertContaminatedPolicy.AutoRepair
    };

    private void ApplyLanguage()
    {
        Text = Localization.T("bulk_convert_title");
        ApplyTranslationsRecursive(this);
        int selectedPolicy = Math.Max(0, _policy.SelectedIndex);
        _policy.Items.Clear();
        _policy.Items.Add(Localization.T("bulk_convert_policy_auto"));
        _policy.Items.Add(Localization.T("bulk_convert_policy_skip"));
        _policy.Items.Add(Localization.T("bulk_convert_policy_clean"));
        _policy.SelectedIndex = Math.Min(selectedPolicy, _policy.Items.Count - 1);
        if (_policy.SelectedIndex < 0) _policy.SelectedIndex = 0;

        string[] headers =
        {
            "bulk_convert_selected_col", "bulk_col_status", "bulk_convert_action_col", "bulk_col_source", "bulk_col_save_key",
            "bulk_convert_match_col", "bulk_convert_vanilla_col", "bulk_col_events", "bulk_col_branches", "bulk_convert_output_col"
        };
        for (int i = 0; i < headers.Length && i < _grid.Columns.Count; i++) _grid.Columns[i].HeaderText = Localization.T(headers[i]);
        _detailsGroup.Text = Localization.T("bulk_convert_details");
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
        foreach (Control c in new Control[] { _legacyRoot, _dataRoot, _outputRoot, _policy }) UiStyling.StyleInput(c);
        SizePolicyComboToContent();
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
        UiStyling.StyleButton(_browseLegacy, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_browseData, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_browseOutput, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_planButton, ButtonStyleKind.Accent);
        UiStyling.StyleButton(_validateButton, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_convertButton, ButtonStyleKind.Primary);
        UiStyling.StyleButton(_selectReady, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_clearSelection, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_resolveMatch, ButtonStyleKind.Accent);
        UiStyling.StyleButton(_exportPlan, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_exportResult, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_openOutput, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_recycleSuccessful, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_recycleOrphans, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_close, ButtonStyleKind.Secondary);
        UiStyling.StyleButton(_help, ButtonStyleKind.Secondary);
    }


    private void SizePolicyComboToContent()
    {
        // This policy list contains intentionally descriptive labels. A fixed-width
        // ComboBox clips both the selected value and the drop-down entries, especially
        // after UI font scaling or localization. Measure the actual localized strings
        // using the current scaled font and make the control wide enough for the
        // longest item. The popup itself is capped to the current working area.
        int longest = 0;
        foreach (object? item in _policy.Items)
        {
            string text = Convert.ToString(item) ?? string.Empty;
            if (text.Length == 0) continue;
            longest = Math.Max(longest, TextRenderer.MeasureText(
                text,
                _policy.Font,
                Size.Empty,
                TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width);
        }

        int chrome = SystemInformation.VerticalScrollBarWidth + 38;
        int desiredWidth = Math.Max(360, longest + chrome);

        int workingWidth;
        try
        {
            workingWidth = Screen.FromControl(this).WorkingArea.Width;
        }
        catch
        {
            workingWidth = 1920;
        }

        // The form's popup host is horizontally scrollable, so the ComboBox itself may
        // be wider than the currently visible client area. Do not let the drop-down
        // flyout extend beyond the monitor, though.
        int dropDownCap = Math.Max(360, workingWidth - 96);
        _policy.Width = desiredWidth;
        _policy.MinimumSize = new Size(desiredWidth, 0);
        _policy.DropDownWidth = Math.Min(desiredWidth, dropDownCap);
    }

    private void ApplyAccessibility()
    {
        static void Mark(Control control, string key)
        {
            string text = Localization.T(key);
            UiStyling.MarkAccessible(control, text, text);
        }
        Mark(_legacyRoot, "bulk_convert_legacy_root");
        Mark(_dataRoot, "bulk_convert_data_root");
        Mark(_outputRoot, "bulk_convert_output_root");
        Mark(_policy, "bulk_convert_policy");
        Mark(_overwrite, "bulk_convert_overwrite");
        Mark(_recycleAfterSuccess, "bulk_cleanup_after_success");
        Mark(_grid, "bulk_convert_title");
        Mark(_details, "bulk_convert_details");
        Mark(_planButton, "bulk_convert_plan");
        Mark(_validateButton, "bulk_convert_validate");
        Mark(_convertButton, "bulk_convert_run");
        Mark(_resolveMatch, "bulk_match_resolve");
        Mark(_recycleSuccessful, "bulk_cleanup_successful_button");
        Mark(_recycleOrphans, "bulk_cleanup_orphans_button");
        Mark(_help, "help_button");
    }

    private void SetBusy(bool busy)
    {
        _planButton.Enabled = !busy;
        _browseLegacy.Enabled = !busy;
        _browseData.Enabled = !busy;
        _browseOutput.Enabled = !busy;
        _policy.Enabled = !busy;
        _validateButton.Enabled = !busy && _plan is not null && _plan.ReadyCount > 0;
        _convertButton.Enabled = !busy && _plan is not null && _plan.ReadyCount > 0;
        _selectReady.Enabled = !busy && _plan is not null && _plan.ReadyCount > 0;
        _clearSelection.Enabled = !busy && _plan is not null && _plan.ReadyCount > 0;
        _resolveMatch.Enabled = !busy && _plan is not null && _grid.SelectedRows.Count > 0;
        _exportPlan.Enabled = !busy && _plan is not null;
        _exportResult.Enabled = !busy && _lastResult is not null;
        _recycleSuccessful.Enabled = !busy && _lastResult is not null && _lastResult.Items.Any(x => x.Success && !x.Skipped && !x.LegacyCleanupSucceeded);
        _recycleOrphans.Enabled = !busy && _plan is not null && BulkConversionService.HasStrictOrphanSources(_plan);
        _recycleAfterSuccess.Enabled = !busy;
        UseWaitCursor = busy;
    }

    private static void BrowseFolder(TextBox target, string description)
    {
        using var dialog = new FolderBrowserDialog { Description = description, SelectedPath = Directory.Exists(target.Text) ? target.Text : string.Empty };
        if (dialog.ShowDialog() == DialogResult.OK) target.Text = dialog.SelectedPath;
    }

    private void TryResolveDefaultOutputRoot()
    {
        try
        {
            if (!Directory.Exists(_dataRoot.Text)) return;
            string? persistent = Directory.GetParent(Path.GetFullPath(_dataRoot.Text))?.FullName;
            if (!string.IsNullOrWhiteSpace(persistent)) _outputRoot.Text = Path.Combine(persistent, "IMDataCore");
        }
        catch { }
    }

    private void ExportPlan()
    {
        if (_plan is null) return;
        using var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json|All files (*.*)|*.*", FileName = "imdatacore-bulk-convert-plan.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        BulkConversionService.WritePlanJson(_plan, dialog.FileName);
    }

    private void ExportResult()
    {
        if (_lastResult is null) return;
        using var dialog = new SaveFileDialog { Filter = "JSON (*.json)|*.json|All files (*.*)|*.*", FileName = "imdatacore-bulk-convert-result.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        BulkConversionService.WriteRunJson(_lastResult, dialog.FileName);
    }

    private void OpenOutput()
    {
        string path = _outputRoot.Text.Trim();
        if (!Directory.Exists(path)) return;
        Process.Start(new ProcessStartInfo("explorer.exe", path) { UseShellExecute = true });
    }
}
