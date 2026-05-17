using ModeSwitcher.Core;
using ModeSwitcher.Core.FileSystem;
using ModeSwitcher.Core.Models;
using ModeSwitcher.Core.Services;

namespace ModeSwitcher.UI;

public partial class MainForm : Form
{
    private readonly ICodeSwitcher _switcher;
    private List<ModeInfo>? _modes;
    private ModeInfo? _selectedMode;
    private bool _isExiting = false;
    private const int DELETE_COLUMN_INDEX = 1;

    public MainForm(ICodeSwitcher switcher)
    {
        _switcher = switcher;
        InitializeComponent();
        LoadData();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_isExiting)
        {
            e.Cancel = true;
            Hide();
            notifyIcon!.Visible = true;
        }
        base.OnFormClosing(e);
    }

    private void NotifyIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            RestoreWindow();
        }
    }

    private void NotifyIcon_DoubleClick(object? sender, EventArgs e)
    {
        RestoreWindow();
    }

    private void RestoreWindow()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private void LoadData()
    {
        try
        {
            _modes = _switcher.GetModes().ToList();

            var currentMode = _switcher.DetectCurrentMode();
            if (currentMode is not null)
            {
                var displayName = GetDisplayName(currentMode.ModeName);
                lblCurrentMode!.Text = $"Агент: {displayName} ✓";
                notifyIcon!.Text = $"Code Switcher - {displayName}";
            }
            else
            {
                lblCurrentMode!.Text = "Агент: не выбран";
                notifyIcon!.Text = "Code Switcher - выберите режим";
            }

            RenderModesToGrid(currentMode);
            UpdateTrayMenu(currentMode);
            SetStatus("Готово");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            SetStatus($"Ошибка: {ex.Message}");
        }
    }

    private string GetDisplayName(string modeName)
    {
        return modeName switch
        {
            "Z" => "🤖 Z",
            "Claude" => "🧠 Claude",
            _ => modeName
        };
    }

    private void RenderModesToGrid(CurrentModeResult? currentMode)
    {
        dgvModes!.Rows.Clear();

        if (_modes is null || _modes.Count == 0)
        {
            var emptyIndex = dgvModes.Rows.Add("Нет доступных режимов", string.Empty);
            var emptyRow = dgvModes.Rows[emptyIndex];
            emptyRow.DefaultCellStyle.ForeColor = Color.Gray;
            emptyRow.Tag = null;
            emptyRow.Cells[DELETE_COLUMN_INDEX] = new DataGridViewTextBoxCell { Value = string.Empty };
            return;
        }

        var currentModeName = currentMode?.ModeName;

        foreach (var mode in _modes)
        {
            var displayName = GetDisplayName(mode.Name);
            var isActive = currentModeName == mode.Name;
            var label = isActive ? $"{displayName} (активен)" : displayName;

            var rowIndex = dgvModes.Rows.Add(label, "Удалить");
            var row = dgvModes.Rows[rowIndex];
            row.Tag = mode;

            if (isActive)
            {
                row.DefaultCellStyle.Font = new Font(dgvModes.Font, FontStyle.Bold);
                row.DefaultCellStyle.BackColor = Color.FromArgb(220, 245, 220);
                row.DefaultCellStyle.ForeColor = Color.FromArgb(0, 100, 0);
                row.DefaultCellStyle.SelectionBackColor = Color.FromArgb(180, 230, 180);
                row.DefaultCellStyle.SelectionForeColor = Color.FromArgb(0, 60, 0);
            }
        }

        if (dgvModes.Rows.Count > 0)
        {
            dgvModes.ClearSelection();
        }
    }

    private void DgvModes_SelectionChanged(object? sender, EventArgs e)
    {
        if (dgvModes!.SelectedRows.Count > 0)
        {
            _selectedMode = dgvModes.SelectedRows[0].Tag as ModeInfo;
        }
        else
        {
            _selectedMode = null;
        }
    }

    private async void DgvModes_CellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex != DELETE_COLUMN_INDEX) return;
        if (dgvModes!.Rows[e.RowIndex].Tag is not ModeInfo mode) return;

        dgvModes.Enabled = false;
        try
        {
            await HandleDeleteModeAsync(mode);
        }
        finally
        {
            dgvModes.Enabled = true;
        }
    }

    private async Task HandleDeleteModeAsync(ModeInfo mode)
    {
        try
        {
            await DeleteModeAsync(mode);
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
            MessageBox.Show($"Не удалось удалить режим:\n{ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task DeleteModeAsync(ModeInfo mode)
    {
        var confirm = MessageBox.Show(this,
            $"Удалить режим \"{mode.Name}\"?",
            "Подтверждение",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        SetStatus($"Удаление режима \"{mode.Name}\"...");
        await _switcher.DeleteModeAsync(mode.Name);
        SetStatus($"Режим \"{mode.Name}\" удалён.");
        LoadData();
    }

    private void UpdateTrayMenu(CurrentModeResult? currentMode)
    {
        trayMenu!.Items.Clear();

        if (_modes is not null)
        {
            foreach (var mode in _modes)
            {
                var isActive = currentMode?.ModeName == mode.Name;
                var displayName = GetDisplayName(mode.Name);

                var item = new ToolStripMenuItem(displayName);
                if (isActive)
                {
                    item.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    item.ForeColor = Color.FromArgb(0, 180, 0); // Зелёный
                    item.Checked = true;
                    item.CheckState = CheckState.Checked;
                }

                item.Click += async (s, e) => await ApplyModeFromTray(mode.Name);
                trayMenu.Items.Add(item);
            }

            trayMenu.Items.Add(new ToolStripSeparator());
        }

        var restoreItem = new ToolStripMenuItem("👁️ Открыть");
        restoreItem.Click += (s, e) => RestoreWindow();
        trayMenu.Items.Add(restoreItem);

        var exitItem = new ToolStripMenuItem("❌ Выход");
        exitItem.Click += (s, e) => ExitApplication();
        trayMenu.Items.Add(exitItem);
    }

    private async Task ApplyModeFromTray(string modeName)
    {
        try
        {
            var result = await _switcher.ApplyModeAsync(modeName);
            if (result)
            {
                notifyIcon!.BalloonTipTitle = "Режим применён";
                notifyIcon.BalloonTipText = $"Переключено на: {modeName}";
                notifyIcon.ShowBalloonTip(2000);
                LoadData();
            }
            else
            {
                notifyIcon!.BalloonTipTitle = "Ошибка";
                notifyIcon.BalloonTipText = "Не удалось применить режим";
                notifyIcon.ShowBalloonTip(3000);
            }
        }
        catch (Exception ex)
        {
            notifyIcon!.BalloonTipTitle = "Ошибка";
            notifyIcon.BalloonTipText = ex.Message;
            notifyIcon.ShowBalloonTip(3000);
        }
    }

    private void ExitApplication()
    {
        _isExiting = true;
        notifyIcon!.Visible = false;
        Application.Exit();
    }

    private async void BtnApply_Click(object? sender, EventArgs e)
    {
        if (_selectedMode is null)
        {
            MessageBox.Show("Выберите режим для применения", "Предупреждение",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetStatus("Применение режима...");
        btnApply!.Enabled = false;

        try
        {
            var result = await _switcher.ApplyModeAsync(_selectedMode.Name);

            if (result)
            {
                SetStatus("Режим применён успешно!");
                LoadData();
            }
            else
            {
                SetStatus("Не удалось применить режим");
                MessageBox.Show("Не удалось применить выбранный режим", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
            MessageBox.Show($"Ошибка применения: {ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            btnApply!.Enabled = true;
        }
    }

    private async void BtnSaveCurrent_Click(object? sender, EventArgs e)
    {
        try
        {
            var current = _switcher.DetectCurrentMode();
            var config = LoadConfigOrNull();
            if (config is null)
            {
                MessageBox.Show("Конфиг не загружен.", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var modesBasePath = Path.Combine(Path.GetDirectoryName(_switcher.ConfigPath)!, "modes");
            string? currentModePath = null;
            if (current is not null)
            {
                var def = config.Modes.FirstOrDefault(m => m.Name == current.ModeName);
                if (def is not null)
                {
                    currentModePath = Path.Combine(modesBasePath, def.Folder);
                }
            }

            var fs = new RealFileSystem();
            var modeSaver = new ModeSaver(fs);
            var candidates = modeSaver.GetCandidates(config.TargetPath, currentModePath);

            var settingsPath = Path.Combine(config.TargetPath, "settings.json");
            var suggestedName = ModeNameSuggester.SuggestFromSettings(settingsPath, fs);

            using var dialog = new SaveCurrentModeDialog(
                candidates,
                suggestedName,
                currentModeDisplayName: current?.ModeName,
                existingNames: config.Modes.Select(m => m.Name),
                existingFolders: config.Modes.Select(m => m.Folder));

            if (dialog.ShowDialog(this) != DialogResult.OK) return;

            SetStatus("Сохранение режима...");
            await _switcher.SaveCurrentAsModeAsync(
                dialog.ModeName,
                dialog.FolderName,
                dialog.SelectedRelativePaths,
                dialog.OverwriteRequested);

            SetStatus($"Режим \"{dialog.ModeName}\" сохранён.");
            LoadData();
        }
        catch (Exception ex)
        {
            SetStatus($"Ошибка: {ex.Message}");
            MessageBox.Show($"Не удалось сохранить режим:\n{ex.Message}", "Ошибка",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private SwitcherConfig? LoadConfigOrNull()
    {
        try
        {
            var loader = new ConfigLoader(new RealFileSystem());
            return loader.Load(_switcher.ConfigPath);
        }
        catch
        {
            return null;
        }
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        LoadData();
    }

    private void BtnAbout_Click(object? sender, EventArgs e)
    {
        using var form = new AboutForm();
        form.ShowDialog(this);
    }

    private void BtnExit_Click(object? sender, EventArgs e)
    {
        ExitApplication();
    }

    private void SetStatus(string message)
    {
        lblStatus!.Text = message;
    }
}
