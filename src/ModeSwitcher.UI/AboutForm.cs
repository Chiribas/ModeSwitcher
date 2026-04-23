using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace ModeSwitcher.UI;

public partial class AboutForm : Form
{
    private Button? btnClose;

    public AboutForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.SuspendLayout();

        // AboutForm
        this.ClientSize = new Size(600, 500);
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Справка - Code Switcher";
        this.BackColor = Color.White;

        // Main panel
        var pnlMain = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20)
        };

        // Title
        var lblTitle = new Label
        {
            Text = "🔄 Code Switcher",
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            ForeColor = Color.FromArgb(41, 128, 185),
            AutoSize = true,
            Location = new Point(0, 10)
        };

        // Description
        var lblDesc = new Label
        {
            Text = "Переключение конфигураций AI агентов",
            Font = new Font("Segoe UI", 11F),
            ForeColor = Color.FromArgb(100, 100, 100),
            AutoSize = true,
            Location = new Point(0, 50)
        };

        // RichTextBox with content
        var rtbContent = new RichTextBox
        {
            Location = new Point(0, 90),
            Size = new Size(540, 340),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 250, 250),
            ForeColor = Color.FromArgb(60, 60, 60),
            Font = new Font("Segoe UI", 10F),
            ReadOnly = true,
            Multiline = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Text =
@"⚙️  КОНФИГУРАЦИЯ

Файл modeswitcher.json (рядом с exe):

{
  ""TargetPath"": ""C:/Users/you/.claude"",
  ""Modes"": [
    { ""Name"": ""Зидан"",  ""Folder"": ""Z"" },
    { ""Name"": ""Жанклод"", ""Folder"": ""Claude"" }
  ]
}

• TargetPath — куда копировать конфиг
• Modes/Folder — папка в modes/


🚀 РЕЖИМЫ ЗАПУСКА

Обычный запуск:
  ModeSwitcher.UI.exe

С логами (отладка):
  ModeSwitcher.UI.exe --debug

Системный трей:
  • Двойной клик — открыть окно
  • Правый клик — меню режимов"
        };

        // Bottom panel
        var pnlBottom = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 50,
            BackColor = Color.White
        };

        btnClose = new Button
        {
            Text = "Закрыть",
            Size = new Size(120, 40),
            Location = new Point(465, 5),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            BackColor = Color.FromArgb(41, 128, 185),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            DialogResult = DialogResult.OK
        };
        btnClose.FlatAppearance.BorderColor = Color.FromArgb(41, 128, 185);
        pnlBottom.Controls.Add(btnClose);

        // Add controls
        pnlMain.Controls.Add(lblTitle);
        pnlMain.Controls.Add(lblDesc);
        pnlMain.Controls.Add(rtbContent);

        this.Controls.Add(pnlMain);
        this.Controls.Add(pnlBottom);

        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
