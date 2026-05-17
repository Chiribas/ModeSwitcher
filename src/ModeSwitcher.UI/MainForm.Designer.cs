#nullable disable
namespace ModeSwitcher.UI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private Label? lblCurrentMode;
    private Panel? pnlModes;
    private Button? btnApply;
    private Button? btnSaveCurrent;
    private Button? btnRefresh;
    private Button? btnAbout;
    private Button? btnExit;
    private StatusStrip? statusStrip;
    private ToolStripStatusLabel? lblStatus;
    private NotifyIcon? notifyIcon;
    private ContextMenuStrip? trayMenu;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.lblCurrentMode = new Label();
        this.pnlModes = new Panel();
        this.btnApply = new Button();
        this.btnRefresh = new Button();
        this.btnExit = new Button();
        this.statusStrip = new StatusStrip();
        this.lblStatus = new ToolStripStatusLabel();
        this.trayMenu = new ContextMenuStrip(this.components);
        this.statusStrip.SuspendLayout();
        this.SuspendLayout();

        // lblCurrentMode
        this.lblCurrentMode.AutoSize = true;
        this.lblCurrentMode.Location = new Point(20, 20);
        this.lblCurrentMode.Name = "lblCurrentMode";
        this.lblCurrentMode.Size = new Size(150, 20);
        this.lblCurrentMode.Text = "Текущий режим: ...";
        this.lblCurrentMode.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        // pnlModes
        this.pnlModes.Location = new Point(20, 60);
        this.pnlModes.Name = "pnlModes";
        this.pnlModes.Size = new Size(360, 150);
        this.pnlModes.BorderStyle = BorderStyle.FixedSingle;
        this.pnlModes.AutoScroll = true;
        this.pnlModes.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // btnApply
        this.btnApply.Location = new Point(20, 230);
        this.btnApply.Name = "btnApply";
        this.btnApply.Size = new Size(200, 35);
        this.btnApply.Text = "Применить выбранный режим";
        this.btnApply.UseVisualStyleBackColor = true;
        this.btnApply.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnApply.Click += new EventHandler(this.BtnApply_Click);

        // btnSaveCurrent
        this.btnSaveCurrent = new Button();
        this.btnSaveCurrent.Location = new Point(230, 230);
        this.btnSaveCurrent.Name = "btnSaveCurrent";
        this.btnSaveCurrent.Size = new Size(150, 35);
        this.btnSaveCurrent.Text = "Сохранить текущий…";
        this.btnSaveCurrent.UseVisualStyleBackColor = true;
        this.btnSaveCurrent.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnSaveCurrent.Click += new EventHandler(this.BtnSaveCurrent_Click);

        // btnRefresh
        this.btnRefresh.Location = new Point(20, 280);
        this.btnRefresh.Name = "btnRefresh";
        this.btnRefresh.Size = new Size(100, 35);
        this.btnRefresh.Text = "Обновить";
        this.btnRefresh.UseVisualStyleBackColor = true;
        this.btnRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnRefresh.Click += new EventHandler(this.BtnRefresh_Click);

        // btnAbout
        this.btnAbout = new Button();
        this.btnAbout.Location = new Point(130, 280);
        this.btnAbout.Name = "btnAbout";
        this.btnAbout.Size = new Size(35, 35);
        this.btnAbout.Text = "?";
        this.btnAbout.Font = new Font("Segoe UI", 14F, FontStyle.Bold);
        this.btnAbout.UseVisualStyleBackColor = true;
        this.btnAbout.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnAbout.Click += new EventHandler(this.BtnAbout_Click);

        // btnExit
        this.btnExit.Location = new Point(280, 280);
        this.btnExit.Name = "btnExit";
        this.btnExit.Size = new Size(100, 35);
        this.btnExit.Text = "Выход";
        this.btnExit.UseVisualStyleBackColor = true;
        this.btnExit.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        this.btnExit.Click += new EventHandler(this.BtnExit_Click);

        // statusStrip
        this.statusStrip.Items.AddRange(new ToolStripItem[] { this.lblStatus });
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Text = "Готово";
        this.lblStatus.Spring = true;

        // trayMenu
        this.trayMenu = new ContextMenuStrip();

        // notifyIcon
        this.notifyIcon = new NotifyIcon(this.components);
        this.notifyIcon.Icon = new Icon(typeof(MainForm).Assembly.GetManifestResourceStream("ModeSwitcher.UI.AppIcon.ico") ?? throw new InvalidOperationException("Icon not found"));
        this.notifyIcon.Text = "Code Switcher";
        this.notifyIcon.Visible = true;
        this.notifyIcon.MouseClick += new MouseEventHandler(this.NotifyIcon_MouseClick);
        this.notifyIcon.DoubleClick += new EventHandler(this.NotifyIcon_DoubleClick);
        this.notifyIcon.ContextMenuStrip = this.trayMenu;

        // MainForm
        this.Icon = new Icon(typeof(MainForm).Assembly.GetManifestResourceStream("ModeSwitcher.UI.AppIcon.ico") ?? throw new InvalidOperationException("Icon not found"));
        this.ClientSize = new Size(400, 350);
        this.MinimumSize = new Size(360, 300);
        this.Controls.Add(this.lblCurrentMode);
        this.Controls.Add(this.pnlModes);
        this.Controls.Add(this.btnApply);
        this.Controls.Add(this.btnSaveCurrent);
        this.Controls.Add(this.btnRefresh);
        this.Controls.Add(this.btnAbout);
        this.Controls.Add(this.btnExit);
        this.Controls.Add(this.statusStrip);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = true;
        this.Name = "MainForm";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Text = "Code Switcher v1.0";
        this.statusStrip.ResumeLayout(false);
        this.statusStrip.PerformLayout();
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
