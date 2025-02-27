# Singer Signer

A program for creating test certificates and signing drivers with them. So it contains everything you need to manage certificates, switch Windows to test mode, and so on.

## Features

- Create and install test certificates for driver signing
- Remove test certificates from the system
- Sign drivers with test certificates
- Enable/disable Windows test mode
- Enable/disable driver signature enforcement
- Dark theme user interface
- Administrative privileges management
- Operation logging

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime
- Windows SDK or WDK (for SignTool.exe)
- Administrative privileges

## Installation

1. Install .NET 8.0 Runtime from https://dotnet.microsoft.com/download/dotnet/8.0
2. Install Windows SDK from https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/
3. Download the latest release or build from source
4. Run the application as administrator

## Usage

1. **Certificate Management**
   - Enter a name for the certificate
   - Click "Create Certificate" to create and install a new test certificate
   - Use the certificate thumbprint to remove it from the system

2. **Driver Signing**
   - Create a test certificate first
   - Click "Browse" to select a driver file
   - Click "Sign Driver" to sign the selected driver with the current certificate

3. **Windows Settings**
   - Use "Enable/Disable Test Mode" to toggle Windows test mode
   - Use "Enable/Disable Driver Signing" to toggle driver signature enforcement
   - Restart your computer after changing these settings

## Building from Source

### Prerequisites
- .NET 8.0 SDK
- Visual Studio 2022 or later (optional)
- Windows SDK 10.0.22621.0 or later

### Command Line Build
1. Clone the repository
2. Open terminal in the project directory
3. Run commands:
   ```powershell
   # Debug build
   dotnet build

   # Release build with single executable
   dotnet publish -c Release
   ```
4. Find the output in:
   - Debug: `bin\Debug\net8.0-windows`
   - Release: `bin\Release\net8.0-windows\win-x64\publish`

### Visual Studio Build
1. Open `DriverSignTool.csproj` in Visual Studio
2. Select build configuration (Debug/Release)
3. Build Solution (F6) or Publish (Right click on project -> Publish)

   
## Security Note

This tool requires administrative privileges to function properly as it modifies system security settings. The test certificates created by this tool are for development and testing purposes only. For production drivers, use an official certificate from a trusted certificate authority.

## Author

Created by [AlestackOverglow](https://alestackoverglow.github.io/)

## License

MIT License 
