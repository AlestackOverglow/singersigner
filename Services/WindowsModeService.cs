using System;
using System.Diagnostics;

namespace DriverSignTool.Services
{
    /// <summary>
    /// Service for managing Windows test mode and driver signing enforcement
    /// </summary>
    public class WindowsModeService
    {
        /// <summary>
        /// Enables Windows test mode
        /// </summary>
        public void EnableTestMode()
        {
            ExecuteCommand("bcdedit /set testsigning on");
        }

        /// <summary>
        /// Disables Windows test mode
        /// </summary>
        public void DisableTestMode()
        {
            ExecuteCommand("bcdedit /set testsigning off");
        }

        /// <summary>
        /// Enables driver signature enforcement
        /// </summary>
        public void EnableDriverSignatureEnforcement()
        {
            ExecuteCommand("bcdedit /set nointegritychecks off");
        }

        /// <summary>
        /// Disables driver signature enforcement
        /// </summary>
        public void DisableDriverSignatureEnforcement()
        {
            ExecuteCommand("bcdedit /set nointegritychecks on");
        }

        private void ExecuteCommand(string command)
        {
            var processInfo = new ProcessStartInfo("cmd.exe", $"/c {command}")
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                Verb = "runas"
            };

            var process = Process.Start(processInfo);
            process?.WaitForExit();

            if (process?.ExitCode != 0)
            {
                throw new Exception($"Command execution failed: {command}");
            }
        }
    }
} 