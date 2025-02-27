using System;
using System.Windows;
using System.Windows.Controls;
using DriverSignTool.Services;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;

namespace DriverSignTool;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private const string AuthorUrl = "https://alestackoverglow.github.io/";
    private readonly CertificateService _certificateService;
    private readonly WindowsModeService _windowsModeService;
    private readonly DriverSigningService _driverSigningService;
    private bool _testModeEnabled;
    private bool _driverSigningDisabled;
    private X509Certificate2? _currentCertificate;
    private string? _currentCertificateThumbprint;

    public MainWindow()
    {
        InitializeComponent();
        _certificateService = new CertificateService();
        _windowsModeService = new WindowsModeService();
        _driverSigningService = new DriverSigningService();
        LogMessage("Singer Signer started");
    }

    private void CreateCertificateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var certificateName = CertificateNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                MessageBox.Show("Please enter a certificate name", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _currentCertificate = _certificateService.CreateTestCertificate(certificateName);
            _currentCertificateThumbprint = _currentCertificate.Thumbprint;
            _certificateService.InstallCertificate(_currentCertificate);
            LogMessage($"Certificate created and installed: {certificateName}");
            LogMessage($"Certificate thumbprint: {_currentCertificateThumbprint}");
            
            
            var result = MessageBox.Show(
                "Certificate created successfully. Would you like to export it now for use on other computers?",
                "Export Certificate",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                ExportCurrentCertificate();
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error creating certificate: {ex.Message}");
            MessageBox.Show($"Error creating certificate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ExportCurrentCertificate()
    {
        try
        {
            if (string.IsNullOrEmpty(_currentCertificateThumbprint))
            {
                MessageBox.Show("No certificate available for export. Please create a certificate first.", 
                    "Export Certificate", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "PFX Files (*.pfx)|*.pfx|All files (*.*)|*.*",
                DefaultExt = ".pfx",
                Title = "Export Certificate",
                FileName = $"DriverCertificate_{DateTime.Now:yyyyMMdd}.pfx"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                if (_certificateService.ExportCertificate(_currentCertificateThumbprint, saveFileDialog.FileName))
                {
                    var passwordFile = Path.ChangeExtension(saveFileDialog.FileName, ".txt");
                    LogMessage($"Certificate exported successfully to: {saveFileDialog.FileName}");
                    LogMessage($"Password file saved to: {passwordFile}");
                    MessageBox.Show(
                        $"Certificate exported successfully!\n\n" +
                        $"Certificate: {saveFileDialog.FileName}\n" +
                        $"Password file: {passwordFile}\n\n" +
                        $"Keep these files to install the certificate on other computers.",
                        "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error exporting certificate: {ex.Message}");
            MessageBox.Show($"Error exporting certificate: {ex.Message}", "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void RemoveCertificateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var certificateName = CertificateNameTextBox.Text;
            if (string.IsNullOrWhiteSpace(certificateName))
            {
                MessageBox.Show("Please enter a certificate thumbprint", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _certificateService.RemoveCertificate(certificateName);
            LogMessage($"Certificate removed: {certificateName}");
            MessageBox.Show("Certificate removed successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Error removing certificate: {ex.Message}");
            MessageBox.Show($"Error removing certificate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void TestModeButton_Click(object? sender, RoutedEventArgs? e)
    {
        try
        {
            if (_testModeEnabled)
            {
                _windowsModeService.DisableTestMode();
                TestModeButton.Content = "Enable Test Mode";
                _testModeEnabled = false;
                LogMessage("Test mode disabled");
            }
            else
            {
                _windowsModeService.EnableTestMode();
                TestModeButton.Content = "Disable Test Mode";
                _testModeEnabled = true;
                LogMessage("Test mode enabled");
            }

            PromptForReboot("Test Mode settings have been changed.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error changing test mode: {ex.Message}");
            MessageBox.Show($"Error changing test mode: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void DriverSigningButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_driverSigningDisabled)
            {
                _windowsModeService.EnableDriverSignatureEnforcement();
                DriverSigningButton.Content = "Disable Driver Signing";
                _driverSigningDisabled = false;
                LogMessage("Driver signing enabled");
            }
            else
            {
                _windowsModeService.DisableDriverSignatureEnforcement();
                DriverSigningButton.Content = "Enable Driver Signing";
                _driverSigningDisabled = true;
                LogMessage("Driver signing disabled");
            }

            PromptForReboot("Driver Signing settings have been changed.");
        }
        catch (Exception ex)
        {
            LogMessage($"Error changing driver signing: {ex.Message}");
            MessageBox.Show($"Error changing driver signing: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void PromptForReboot(string message)
    {
        var result = MessageBox.Show(
            $"{message}\n\nYou need to restart your computer for the changes to take effect.\n\nWould you like to restart now?",
            "Restart Required",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                Process.Start(new ProcessStartInfo("shutdown", "/r /t 0")
                {
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    Verb = "runas"
                });
            }
            catch (Exception ex)
            {
                LogMessage($"Error initiating system restart: {ex.Message}");
                MessageBox.Show(
                    "Failed to initiate system restart. Please restart your computer manually.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    private void BrowseDriverButton_Click(object sender, RoutedEventArgs e)
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Driver Files (*.sys)|*.sys|All files (*.*)|*.*",
            Title = "Select Driver File"
        };

        if (openFileDialog.ShowDialog() == true)
        {
            DriverPathTextBox.Text = openFileDialog.FileName;
        }
    }

    private void SignDriverButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_currentCertificate == null)
            {
                MessageBox.Show("Please create or select a certificate first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var driverPath = DriverPathTextBox.Text;
            if (string.IsNullOrWhiteSpace(driverPath))
            {
                MessageBox.Show("Please select a driver file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _driverSigningService.SignDriver(driverPath, _currentCertificate);
            LogMessage($"Driver signed successfully: {driverPath}");
            MessageBox.Show("Driver signed successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Error signing driver: {ex.Message}");
            MessageBox.Show($"Error signing driver: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void AuthorLink_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AuthorUrl,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            LogMessage($"Error opening URL: {ex.Message}");
        }
    }

    private void LogMessage(string message)
    {
        LogTextBox.AppendText($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
        LogTextBox.ScrollToEnd();
        StatusTextBlock.Text = message;
    }

    private void ExportCertificateButton_Click(object sender, RoutedEventArgs e)
    {
        if (_currentCertificate != null)
        {
            ExportCurrentCertificate();
        }
        else
        {
            try
            {
                var thumbprint = CertificateNameTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(thumbprint))
                {
                    MessageBox.Show("Please enter a certificate thumbprint or create a new certificate.", 
                        "Export Certificate", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "PFX Files (*.pfx)|*.pfx|All files (*.*)|*.*",
                    DefaultExt = ".pfx",
                    Title = "Export Certificate",
                    FileName = $"DriverCertificate_{DateTime.Now:yyyyMMdd}.pfx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    if (_certificateService.ExportCertificate(thumbprint, saveFileDialog.FileName))
                    {
                        var passwordFile = Path.ChangeExtension(saveFileDialog.FileName, ".txt");
                        LogMessage($"Certificate exported successfully to: {saveFileDialog.FileName}");
                        LogMessage($"Password file saved to: {passwordFile}");
                        MessageBox.Show(
                            $"Certificate exported successfully!\n\n" +
                            $"Certificate: {saveFileDialog.FileName}\n" +
                            $"Password file: {passwordFile}\n\n" +
                            $"Keep these files to install the certificate on other computers.",
                            "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Certificate not found. Please check the thumbprint.", 
                            "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                LogMessage($"Error exporting certificate: {ex.Message}");
                MessageBox.Show($"Error exporting certificate: {ex.Message}", 
                    "Export Failed", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void ImportCertificateButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // choose pfx file
            var openFileDialog = new OpenFileDialog
            {
                Filter = "PFX Files (*.pfx)|*.pfx|All files (*.*)|*.*",
                Title = "Select Certificate File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                // check if password file exists
                var passwordFile = Path.ChangeExtension(openFileDialog.FileName, ".txt");
                string password;

                if (File.Exists(passwordFile))
                {
                    // read password from file
                    var passwordContent = File.ReadAllText(passwordFile);
                    password = passwordContent.Replace("Certificate Password: ", "").Trim();
                }
                else
                {
                    // if password file not found, ask user for password
                    var passwordInput = new PasswordBox();
                    var dialog = new Window
                    {
                        Title = "Enter Certificate Password",
                        Width = 300,
                        Height = 150,
                        WindowStartupLocation = WindowStartupLocation.CenterOwner,
                        Owner = this
                    };

                    var panel = new StackPanel { Margin = new Thickness(10) };
                    panel.Children.Add(new TextBlock 
                    { 
                        Text = "Enter the certificate password:", 
                        Margin = new Thickness(0, 0, 0, 10) 
                    });
                    panel.Children.Add(passwordInput);
                    
                    var okButton = new Button
                    {
                        Content = "OK",
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        IsDefault = true
                    };
                    okButton.Click += (s, args) => dialog.DialogResult = true;
                    panel.Children.Add(okButton);

                    dialog.Content = panel;

                    if (dialog.ShowDialog() != true)
                        return;

                    password = passwordInput.Password;
                }

                _certificateService.ImportCertificate(openFileDialog.FileName, password);
                
                LogMessage($"Certificate imported successfully from: {openFileDialog.FileName}");
                MessageBox.Show(
                    "Certificate has been imported successfully!\n\n" +
                    "The certificate has been installed in both:\n" +
                    "- Personal Certificates\n" +
                    "- Trusted Root Certification Authorities\n\n" +
                    "You can now install drivers signed with this certificate.",
                    "Import Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                // suggest to enable test mode if it is not enabled
                if (!_testModeEnabled)
                {
                    var result = MessageBox.Show(
                        "Would you like to enable Windows Test Mode now?\n\n" +
                        "Test Mode must be enabled to install test-signed drivers.",
                        "Enable Test Mode",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.Yes)
                    {
                        TestModeButton_Click(sender, e);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"Error importing certificate: {ex.Message}");
            MessageBox.Show($"Error importing certificate: {ex.Message}", 
                "Import Failed", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}