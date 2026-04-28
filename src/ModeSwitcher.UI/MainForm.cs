using ModeSwitcher.Core;
using ModeSwitcher.Core.Models;

namespace ModeSwitcher.UI;

public partial class MainForm : Form
{
    private readonly ICodeSwitcher _switcher;
    private List<ModeInfo>? _modes;
    private ModeInfo? _selectedMode;
    private bool _isExiting = false;

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

            RenderModes();
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

    private void RenderModes()
    {
        pnlModes!.Controls.Clear();

        if (_modes is null || _modes.Count == 0)
        {
            var lbl = new Label
            {
                Text = "Нет доступных режимов",
                Location = new Point(10, 10),
                AutoSize = true
            };
            pnlModes.Controls.Add(lbl);
            return;
        }

        var y = 10;
        foreach (var mode in _modes)
        {
            var displayName = GetDisplayName(mode.Name);
            var radio = new RadioButton
            {
                Text = mode.IsActive ? $"{displayName} (активен)" : displayName,
                Location = new Point(10, y),
                Width = pnlModes.Width - 20,
                Checked = mode.IsActive,
                Tag = mode,
                Font = new Font("Segoe UI", 10F, mode.IsActive ? FontStyle.Bold : FontStyle.Regular)
            };

            radio.CheckedChanged += (s, e) =>
            {
                if (radio.Checked)
                {
                    _selectedMode = mode;
                }
            };

            if (mode.IsActive)
            {
                _selectedMode = mode;
            }

            pnlModes.Controls.Add(radio);
            y += 35;
        }
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
