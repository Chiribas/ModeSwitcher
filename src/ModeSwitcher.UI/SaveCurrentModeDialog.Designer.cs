#nullable disable
namespace ModeSwitcher.UI;

partial class SaveCurrentModeDialog
{
    private System.ComponentModel.IContainer components = null;
    private Label lblCurrentMode;
    private Label lblName;
    private TextBox txtName;
    private Label lblFolder;
    private TextBox txtFolder;
    private Label lblFiles;
    private CheckedListBox clbFiles;
    private Label lblLegend;
    private Label lblError;
    private Button btnOk;
    private Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.lblCurrentMode = new Label();
        this.lblName = new Label();
        this.txtName = new TextBox();
        this.lblFolder = new Label();
        this.txtFolder = new TextBox();
        this.lblFiles = new Label();
        this.clbFiles = new CheckedListBox();
        this.lblLegend = new Label();
        this.lblError = new Label();
        this.btnOk = new Button();
        this.btnCancel = new Button();
        this.SuspendLayout();

        // lblCurrentMode
        this.lblCurrentMode.AutoSize = true;
        this.lblCurrentMode.Location = new Point(15, 15);
        this.lblCurrentMode.Text = "Активный мод: —";

        // lblName
        this.lblName.AutoSize = true;
        this.lblName.Location = new Point(15, 50);
        this.lblName.Text = "Имя:";

        // txtName
        this.txtName.Location = new Point(80, 47);
        this.txtName.Size = new Size(385, 23);
        this.txtName.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // lblFolder
        this.lblFolder.AutoSize = true;
        this.lblFolder.Location = new Point(15, 80);
        this.lblFolder.Text = "Папка:";

        // txtFolder
        this.txtFolder.Location = new Point(80, 77);
        this.txtFolder.Size = new Size(385, 23);
        this.txtFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        // lblFiles
        this.lblFiles.AutoSize = true;
        this.lblFiles.Location = new Point(15, 115);
        this.lblFiles.Text = "Файлы:";

        // clbFiles
        this.clbFiles.Location = new Point(15, 135);
        this.clbFiles.Size = new Size(450, 180);
        this.clbFiles.CheckOnClick = true;
        this.clbFiles.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;

        // lblLegend
        this.lblLegend.AutoSize = true;
        this.lblLegend.Location = new Point(15, 320);
        this.lblLegend.Text = "✓ — уже входит в активный мод";
        this.lblLegend.ForeColor = Color.Gray;
        this.lblLegend.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        // lblError
        this.lblError.AutoSize = true;
        this.lblError.Location = new Point(15, 345);
        this.lblError.Text = "";
        this.lblError.ForeColor = Color.Red;
        this.lblError.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;

        // btnOk
        this.btnOk.Location = new Point(290, 370);
        this.btnOk.Size = new Size(85, 30);
        this.btnOk.Text = "Сохранить";
        this.btnOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnOk.Click += new EventHandler(this.BtnOk_Click);

        // btnCancel
        this.btnCancel.Location = new Point(385, 370);
        this.btnCancel.Size = new Size(80, 30);
        this.btnCancel.Text = "Отмена";
        this.btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
        this.btnCancel.DialogResult = DialogResult.Cancel;

        // dialog
        this.ClientSize = new Size(480, 415);
        this.MinimumSize = new Size(460, 400);
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.ShowInTaskbar = false;
        this.StartPosition = FormStartPosition.CenterParent;
        this.Text = "Сохранить текущий режим";
        this.AcceptButton = this.btnOk;
        this.CancelButton = this.btnCancel;
        this.Controls.Add(this.lblCurrentMode);
        this.Controls.Add(this.lblName);
        this.Controls.Add(this.txtName);
        this.Controls.Add(this.lblFolder);
        this.Controls.Add(this.txtFolder);
        this.Controls.Add(this.lblFiles);
        this.Controls.Add(this.clbFiles);
        this.Controls.Add(this.lblLegend);
        this.Controls.Add(this.lblError);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.btnCancel);
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
