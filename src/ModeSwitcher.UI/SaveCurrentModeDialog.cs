using System.Text.RegularExpressions;
using ModeSwitcher.Core.Services;

namespace ModeSwitcher.UI;

public partial class SaveCurrentModeDialog : Form
{
    private static readonly Regex ValidFolder = new(@"^[A-Za-z0-9._\-]+$", RegexOptions.Compiled);

    private readonly HashSet<string> _existingNames;
    private readonly HashSet<string> _existingFolders;
    private bool _folderManuallyEdited;
    private bool _syncing;

    public string ModeName => txtName!.Text.Trim();
    public string FolderName => txtFolder!.Text.Trim();
    public IReadOnlyList<string> SelectedRelativePaths { get; private set; } = Array.Empty<string>();
    public bool OverwriteRequested { get; private set; }

    public SaveCurrentModeDialog(
        SaveCandidates candidates,
        string? suggestedName,
        string? currentModeDisplayName,
        IEnumerable<string> existingNames,
        IEnumerable<string> existingFolders)
    {
        InitializeComponent();

        _existingNames = new HashSet<string>(existingNames, StringComparer.OrdinalIgnoreCase);
        _existingFolders = new HashSet<string>(existingFolders, StringComparer.OrdinalIgnoreCase);

        lblCurrentMode!.Text = currentModeDisplayName is null
            ? "Активный мод: —"
            : $"Активный мод: {currentModeDisplayName}";

        txtName!.Text = suggestedName ?? "";
        txtFolder!.Text = ModeNameSuggester.ToFolderName(txtName.Text);

        foreach (var file in candidates.Files)
        {
            var label = file.InCurrentMode ? file.RelativePath : $"{file.RelativePath}    (новый)";
            clbFiles!.Items.Add(new FileItem(file.RelativePath, label), file.InCurrentMode);
        }
        clbFiles!.DisplayMember = nameof(FileItem.Label);

        txtName.TextChanged += (s, e) =>
        {
            if (_syncing) return;
            if (_folderManuallyEdited) return;
            _syncing = true;
            txtFolder.Text = ModeNameSuggester.ToFolderName(txtName.Text);
            _syncing = false;
        };

        txtFolder.TextChanged += (s, e) =>
        {
            if (_syncing) return;
            _folderManuallyEdited = true;
        };
    }

    private void BtnOk_Click(object? sender, EventArgs e)
    {
        lblError!.Text = "";

        if (string.IsNullOrWhiteSpace(ModeName))
        {
            lblError.Text = "Введите имя режима.";
            txtName!.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(FolderName))
        {
            lblError.Text = "Введите имя папки (только латиница, цифры, . _ -).";
            txtFolder!.Focus();
            return;
        }

        if (!ValidFolder.IsMatch(FolderName))
        {
            lblError.Text = "В имени папки разрешены только: A-Z a-z 0-9 . _ -";
            txtFolder!.Focus();
            return;
        }

        var selected = new List<string>();
        foreach (var item in clbFiles!.CheckedItems)
        {
            selected.Add(((FileItem)item).RelativePath);
        }
        if (selected.Count == 0)
        {
            lblError.Text = "Выберите хотя бы один файл.";
            return;
        }
        SelectedRelativePaths = selected;

        var nameConflict = _existingNames.Contains(ModeName);
        var folderConflict = _existingFolders.Contains(FolderName);

        if (nameConflict || folderConflict)
        {
            var msg = $"Режим \"{ModeName}\" или папка \"{FolderName}\" уже существует.\n\n" +
                      "Да — перезаписать\nНет — изменить имя\nОтмена — отменить";
            var choice = MessageBox.Show(this, msg, "Конфликт",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);

            switch (choice)
            {
                case DialogResult.Yes:
                    OverwriteRequested = true;
                    DialogResult = DialogResult.OK;
                    Close();
                    return;
                case DialogResult.No:
                    txtName!.Focus();
                    return;
                default:
                    DialogResult = DialogResult.Cancel;
                    Close();
                    return;
            }
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private record FileItem(string RelativePath, string Label);
}
