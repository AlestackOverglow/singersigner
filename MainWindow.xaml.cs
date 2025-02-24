using System;
using System.Windows;
using DriverSignTool.Services;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32;
using System.Security.Cryptography;
using System.Diagnostics;

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
            _certificateService.InstallCertificate(_currentCertificate);
            LogMessage($"Certificate created and installed: {certificateName}");
            MessageBox.Show("Certificate created and installed successfully", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            LogMessage($"Error creating certificate: {ex.Message}");
            MessageBox.Show($"Error creating certificate: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

    private void TestModeButton_Click(object sender, RoutedEventArgs e)
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
}