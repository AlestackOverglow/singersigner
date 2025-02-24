using System;
using System.Diagnostics;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace DriverSignTool.Services
{
    /// <summary>
    /// Service for signing driver files with certificates
    /// </summary>
    public class DriverSigningService
    {
        private static string? _cachedSignToolPath;
        private readonly string _signToolPath;

        public DriverSigningService()
        {
            _signToolPath = FindSignTool();
            if (string.IsNullOrEmpty(_signToolPath))
            {
                throw new Exception("SignTool.exe not found. Please install Windows SDK or WDK.");
            }
        }

        /// <summary>
        /// Signs a driver file with the specified certificate
        /// </summary>
        /// <param name="driverPath">Full path to the driver file</param>
        /// <param name="certificate">Certificate to sign with</param>
        public void SignDriver(string driverPath, X509Certificate2 certificate)
        {
            if (!File.Exists(driverPath))
            {
                throw new FileNotFoundException("Driver file not found", driverPath);
            }

            // Save certificate to temporary file
            string certPath = Path.GetTempFileName();
            string certPassword = Guid.NewGuid().ToString();
            File.WriteAllBytes(certPath, certificate.Export(X509ContentType.Pfx, certPassword));

            try
            {
                string arguments = $"sign /v /fd SHA256 " +
                                 $"/f \"{certPath}\" " +
                                 $"/p \"{certPassword}\" " +
                                 $"/tr http://timestamp.digicert.com " +
                                 $"/td SHA256 " +
                                 $"/as " +
                                 $"/du \"https://alestackoverglow.github.io/\" " +
                                 $"\"{driverPath}\"";

                var processInfo = new ProcessStartInfo(_signToolPath, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using var process = Process.Start(processInfo);
                if (process == null)
                {
                    throw new Exception("Failed to start SignTool process");
                }

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    throw new Exception($"Driver signing failed. Error: {error}\nOutput: {output}");
                }
            }
            finally
            {
                // Cleanup temporary certificate file
                if (File.Exists(certPath))
                {
                    File.Delete(certPath);
                }
            }
        }

        /// <summary>
        /// Finds SignTool.exe in Windows SDK or WDK installation
        /// </summary>
        private string FindSignTool()
        {
            // Return cached path if available
            if (!string.IsNullOrEmpty(_cachedSignToolPath) && File.Exists(_cachedSignToolPath))
            {
                return _cachedSignToolPath;
            }

            // Check predefined paths first
            string[] possiblePaths = {
                @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.22621.0\x64\signtool.exe",
                @"C:\Program Files (x86)\Windows Kits\10\bin\10.0.19041.0\x64\signtool.exe",
                @"C:\Program Files (x86)\Windows Kits\10\bin\x64\signtool.exe"
            };

            // Search in registry
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows Kits\Installed Roots");
            if (key != null)
            {
                if (key.GetValue("KitsRoot10") is string kitsRoot && !string.IsNullOrEmpty(kitsRoot))
                {
                    var binPath = Path.Combine(kitsRoot, "bin");
                    if (Directory.Exists(binPath))
                    {
                        foreach (var dir in Directory.GetDirectories(binPath))
                        {
                            var signToolPath = Path.Combine(dir, "x64", "signtool.exe");
                            if (File.Exists(signToolPath))
                            {
                                _cachedSignToolPath = signToolPath;
                                return signToolPath;
                            }
                        }
                    }
                }
            }

            // Check predefined paths if registry search failed
            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    _cachedSignToolPath = path;
                    return path;
                }
            }

            return string.Empty;
        }
    }
} 