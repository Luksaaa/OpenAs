using OpenAs.Models;
using OpenAs.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace OpenAs;

public partial class MainWindow : Window
{
    private readonly AssociationRegistryService registryService = new();
    private readonly IconStorageService iconStorageService = new();
    private readonly FileSignatureService signatureService = new();
    private readonly InstalledFileFormatService installedFileFormatService = new();
    private readonly List<FileFormat> availableFormats;
    private readonly ObservableCollection<AssociationRow> associations = new();
    private TextBox ExtensionInput => FindRequiredName<TextBox>("ExtensionTextBox");
    private ComboBox FormatSelector => FindRequiredName<ComboBox>("FormatComboBox");
    private DataGrid AssociationsTable => FindRequiredName<DataGrid>("AssociationsGrid");
    private TextBlock StatusText => FindRequiredName<TextBlock>("StatusTextBlock");
    private TextBox IconPathInput => FindRequiredName<TextBox>("IconPathTextBox");
    private TextBox FormatSearchInput => FindRequiredName<TextBox>("FormatSearchTextBox");
    private TextBlock TestResultText => FindRequiredName<TextBlock>("TestResultTextBlock");
    private RadioButton DefaultIconRadio => FindRequiredName<RadioButton>("DefaultIconRadioButton");
    private RadioButton CustomIconRadio => FindRequiredName<RadioButton>("CustomIconRadioButton");
    private Button ChooseIconButtonControl => FindRequiredName<Button>("ChooseIconButton");

    public MainWindow()
    {
        InitializeComponent();
        availableFormats = installedFileFormatService.GetAvailableFormats().ToList();
        FormatSelector.ItemsSource = availableFormats;
        FormatSelector.SelectedIndex = 0;
        CollectionViewSource.GetDefaultView(FormatSelector.ItemsSource).Filter = FilterFormat;
        AssociationsTable.ItemsSource = associations;
        LoadAssociations();
    }

    private void AddMapping_Click(object sender, RoutedEventArgs e)
    {
        if (FormatSelector.SelectedItem is not FileFormat format)
        {
            SetStatus("Choose the real file type first.", isError: true);
            return;
        }

        try
        {
            var extension = ExtensionRules.Normalize(ExtensionInput.Text);
            var customIconPath = SaveCustomIconIfSelected(extension);
            registryService.SaveAssociation(extension, format, customIconPath);
            ExtensionInput.Clear();
            IconPathInput.Clear();
            DefaultIconRadio.IsChecked = true;
            LoadAssociations();
            SetStatus($"{extension} is now mapped to {format.DisplayName}. Rename a real {format.Extension} file to {extension}, then double-click it.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void RemoveMapping_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not FileAssociation association)
        {
            return;
        }

        try
        {
            registryService.RemoveAssociation(association);
            LoadAssociations();
            SetStatus($"{association.Extension} mapping was removed.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadAssociations();

    private void FormatSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var view = CollectionViewSource.GetDefaultView(FormatSelector.ItemsSource);
        view.Refresh();

        if (FormatSelector.SelectedItem is not FileFormat selectedFormat || !FilterFormat(selectedFormat))
        {
            FormatSelector.SelectedItem = view.Cast<FileFormat>().FirstOrDefault();
        }
    }

    private async void TestFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose a file to test",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            var detector = new FileTypeDetectionService(signatureService);
            var detectedFormat = await detector.DetectAsync(dialog.FileName);
            if (detectedFormat is null)
            {
                TestResultText.Text = "OpenAs could not match this file to a supported signature.";
                SetStatus("File test finished. No supported type was detected.", isError: true);
                return;
            }

            var fileName = System.IO.Path.GetFileName(dialog.FileName);
            TestResultText.Text = $"{fileName} looks like {detectedFormat.DisplayName}. You can map a custom extension to {detectedFormat.Extension}.";
            FormatSelector.SelectedValue = detectedFormat.Id;
            SetStatus($"Detected {detectedFormat.DisplayName}.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void ResetAll_Click(object sender, RoutedEventArgs e)
    {
        if (associations.Count == 0)
        {
            SetStatus("There are no mappings to reset.", isError: false);
            return;
        }

        var result = MessageBox.Show(
            "Remove all mappings created by OpenAs for this Windows user account?",
            "Reset all mappings",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            registryService.RemoveAllAssociations();
            LoadAssociations();
            SetStatus("All OpenAs mappings were removed.", isError: false);
        }
        catch (Exception ex)
        {
            SetStatus(ex.Message, isError: true);
        }
    }

    private void ChooseIcon_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = "Choose file icon",
            Filter = "Icon files (*.ico)|*.ico",
            CheckFileExists = true,
            Multiselect = false
        };

        if (dialog.ShowDialog(this) == true)
        {
            IconPathInput.Text = dialog.FileName;
            CustomIconRadio.IsChecked = true;
        }
    }

    private void DefaultIcon_Checked(object sender, RoutedEventArgs e)
    {
        if (FindName("IconPathTextBox") is not TextBox iconPathTextBox)
        {
            return;
        }

        iconPathTextBox.Clear();
        iconPathTextBox.IsEnabled = false;
        if (FindName("ChooseIconButton") is Button chooseIconButton)
        {
            chooseIconButton.IsEnabled = false;
        }
    }

    private void CustomIcon_Checked(object sender, RoutedEventArgs e)
    {
        if (FindName("IconPathTextBox") is not TextBox iconPathTextBox)
        {
            return;
        }

        iconPathTextBox.IsEnabled = true;
        if (FindName("ChooseIconButton") is Button chooseIconButton)
        {
            chooseIconButton.IsEnabled = true;
        }
    }

    private void LoadAssociations()
    {
        associations.Clear();
        foreach (var association in registryService.GetAssociations())
        {
            associations.Add(new AssociationRow(association));
        }

        if (associations.Count == 0)
        {
            SetStatus("No mappings yet. Add a custom extension like .finger and choose what it should open as.", isError: false);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        StatusText.Text = message;
        StatusText.Foreground = isError
            ? Brushes.Firebrick
            : Brushes.DarkSlateGray;
    }

    private bool FilterFormat(object item)
    {
        if (item is not FileFormat format)
        {
            return false;
        }

        var query = FormatSearchInput.Text.Trim();
        if (string.IsNullOrWhiteSpace(query))
        {
            return true;
        }

        return format.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase)
            || format.Extension.Contains(query, StringComparison.OrdinalIgnoreCase)
            || format.MimeType.Contains(query, StringComparison.OrdinalIgnoreCase)
            || format.PerceivedType.Contains(query, StringComparison.OrdinalIgnoreCase);
    }

    private string? SaveCustomIconIfSelected(string extension)
    {
        if (CustomIconRadio.IsChecked != true)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(IconPathInput.Text))
        {
            throw new InvalidOperationException("Choose a .ico file or switch back to the default type icon.");
        }

        return iconStorageService.SaveIconForExtension(extension, IconPathInput.Text);
    }

    private T FindRequiredName<T>(string name) where T : FrameworkElement =>
        FindName(name) as T
        ?? throw new InvalidOperationException($"Could not find required UI element '{name}'.");
}
