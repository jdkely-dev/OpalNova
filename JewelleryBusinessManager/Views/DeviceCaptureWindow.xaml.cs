using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Imaging;
using JewelleryBusinessManager.Data;
using JewelleryBusinessManager.Models;
using JewelleryBusinessManager.Services;
using Microsoft.Win32;
using MessageBox = System.Windows.MessageBox;
using MessageBoxButton = System.Windows.MessageBoxButton;
using MessageBoxImage = System.Windows.MessageBoxImage;
using MessageBoxResult = System.Windows.MessageBoxResult;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace JewelleryBusinessManager.Views;

public partial class DeviceCaptureWindow : Window
{
    private readonly object? _selectedRecord;
    private SerialPort? _serialPort;
    private string? _lastImportedPath;

    public DeviceCaptureWindow(object? selectedRecord)
    {
        InitializeComponent();
        _selectedRecord = selectedRecord;
        SelectedRecordText.Text = selectedRecord is null ? "No record selected. Select a material, stone, jewellery item, opal parcel or job before opening this tool for direct capture." : $"Selected: {selectedRecord}";
        RefreshPorts();
    }

    protected override void OnClosed(EventArgs e)
    {
        try { _serialPort?.Close(); _serialPort?.Dispose(); } catch { }
        base.OnClosed(e);
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RefreshPorts();

    private void RefreshPorts()
    {
        PortBox.Items.Clear();
        foreach (var port in SerialPort.GetPortNames().OrderBy(p => p)) PortBox.Items.Add(port);
        if (PortBox.Items.Count > 0) PortBox.SelectedIndex = 0;
        else PortBox.Items.Add("No COM ports found");
    }

    private void ConnectScale_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var portName = PortBox.SelectedItem?.ToString();
            if (string.IsNullOrWhiteSpace(portName) || portName.StartsWith("No COM"))
            {
                MessageBox.Show("No serial scale port is selected. If your scale acts like a keyboard wedge, click in the reading box and press the scale print/send button.", "Scale", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            var baud = int.TryParse((BaudBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(), out var parsed) ? parsed : 9600;
            _serialPort?.Close();
            _serialPort = new SerialPort(portName, baud)
            {
                ReadTimeout = 500,
                NewLine = "\r\n"
            };
            _serialPort.DataReceived += SerialPort_DataReceived;
            _serialPort.Open();
            ScaleStatusText.Text = $"Connected to {portName} at {baud} baud. Press/send a reading from the scale.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not connect to the scale.\n\n{ex.Message}", "Scale", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DisconnectScale_Click(object sender, RoutedEventArgs e)
    {
        try { _serialPort?.Close(); } catch { }
        ScaleStatusText.Text = "Scale disconnected.";
    }

    private void SerialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
    {
        try
        {
            var text = _serialPort?.ReadExisting() ?? string.Empty;
            var match = Regex.Match(text, @"[-+]?\d+(?:\.\d+)?");
            if (match.Success)
            {
                Dispatcher.Invoke(() =>
                {
                    WeightBox.Text = match.Value;
                    ScaleStatusText.Text = $"Reading received: {match.Value}";
                });
            }
        }
        catch { }
    }

    private void ApplyWeight_Click(object sender, RoutedEventArgs e)
    {
        if (!decimal.TryParse(WeightBox.Text, NumberStyles.Any, CultureInfo.CurrentCulture, out var weight))
        {
            MessageBox.Show("Enter or capture a valid numeric weight first.", "Weight", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var unit = (UnitBox.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString() ?? "grams";
        using var db = new AppDbContext();
        switch (_selectedRecord)
        {
            case Material materialRecord:
                var material = db.Materials.Find(materialRecord.Id);
                if (material is null) break;
                var oldQuantity = material.CurrentQuantity;
                material.CurrentQuantity = weight;
                material.UnitType = unit == "carats" ? UnitType.Carats : UnitType.Grams;
                db.MaterialTransactions.Add(new MaterialTransaction
                {
                    MaterialId = material.Id,
                    TransactionDate = DateTime.Now,
                    QuantityChange = weight - oldQuantity,
                    Reason = "Scale Capture",
                    Notes = $"Scale capture set quantity from {oldQuantity:0.###} to {weight:0.###} {material.UnitType}."
                });
                db.SaveChanges();
                StatusText.Text = $"Updated material quantity: {oldQuantity:0.###} → {material.CurrentQuantity:0.###} {material.UnitType}.";
                break;
            case Stone stoneRecord:
                var stone = db.Stones.Find(stoneRecord.Id);
                if (stone is null) break;
                var carats = unit == "grams" ? weight * 5m : weight;
                stone.WeightCarats = carats;
                stone.Notes = AppendNote(stone.Notes, $"Scale capture: {weight:0.###} {unit} = {carats:0.###} ct.");
                db.SaveChanges();
                StatusText.Text = $"Updated stone weight to {stone.WeightCarats:0.###} ct.";
                break;
            default:
                MessageBox.Show("Weight capture currently applies directly to selected Materials or Stones. Select one of those records and reopen the tool.", "Weight", MessageBoxButton.OK, MessageBoxImage.Information);
                break;
        }
    }

    private static string AppendNote(string? existing, string note)
    {
        var line = $"[{DateTime.Now:g}] {note}";
        return string.IsNullOrWhiteSpace(existing) ? line : existing + Environment.NewLine + line;
    }

    private void OpenCamera_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("microsoft.windows.camera:") { UseShellExecute = true });
            StatusText.Text = "Windows Camera opened. Capture an image, then use Import Latest Camera Roll Photo.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not open Windows Camera.\n\n{ex.Message}", "Camera", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ImportPhoto_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.webp;*.bmp|All files|*.*"
        };
        if (dialog.ShowDialog() == true) ImportPhoto(dialog.FileName);
    }

    private void ImportLatestCameraRoll_Click(object sender, RoutedEventArgs e)
    {
        var cameraRoll = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "Camera Roll");
        if (!Directory.Exists(cameraRoll))
        {
            MessageBox.Show($"Camera Roll folder was not found.\n\n{cameraRoll}", "Camera", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var latest = Directory.GetFiles(cameraRoll)
            .Where(f => new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(Path.GetExtension(f).ToLowerInvariant()))
            .OrderByDescending(File.GetLastWriteTime)
            .FirstOrDefault();
        if (latest is null)
        {
            MessageBox.Show("No image files were found in Camera Roll.", "Camera", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        ImportPhoto(latest);
    }

    private void ImportPhoto(string sourcePath)
    {
        try
        {
            if (_selectedRecord is null)
            {
                MessageBox.Show("Select a record before opening Device Capture to link photos directly.", "Camera", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var recordType = _selectedRecord.GetType();
            var idProperty = recordType.GetProperty("Id");
            if (idProperty?.GetValue(_selectedRecord) is not int id || id <= 0)
            {
                MessageBox.Show("The selected record does not have a saved Id yet. Save it first, then import the photo.", "Camera", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var storedPath = PhotoStorageService.CopyPhotoToAppFolder(sourcePath, recordType.Name, id);
            using var db = new AppDbContext();
            db.PhotoRecords.Add(new PhotoRecord
            {
                EntityType = recordType.Name,
                EntityId = id,
                FilePath = storedPath,
                Caption = $"Device capture photo for {recordType.Name} #{id}"
            });
            db.SaveChanges();

            _lastImportedPath = storedPath;
            PreviewImage.Source = new BitmapImage(new Uri(storedPath));
            NoImageText.Visibility = Visibility.Collapsed;
            StatusText.Text = $"Photo imported and linked: {Path.GetFileName(storedPath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Could not import the photo.\n\n{ex.Message}", "Camera", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
