using System;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;

namespace DriverSignTool.Services
{
    /// <summary>
    /// Service for managing test certificates for driver signing
    /// </summary>
    public class CertificateService
    {
        private const string CertificateStoreName = "MY";
        private const string CertificateStoreLocation = "LocalMachine";
        private static readonly Regex ValidNamePattern = new(@"^[a-zA-Z0-9\s\-_\.]+$", RegexOptions.Compiled);

        /// <summary>
        /// Validates the certificate name for potentially dangerous characters
        /// </summary>
        /// <param name="name">Certificate name to validate</param>
        /// <returns>True if the name is valid, false otherwise</returns>
        public bool ValidateCertificateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (name.Length > 64)
                return false;

            return ValidNamePattern.IsMatch(name);
        }

        /// <summary>
        /// Creates a new test certificate for driver signing
        /// </summary>
        /// <param name="subjectName">Certificate subject name</param>
        /// <returns>Created certificate</returns>
        public X509Certificate2 CreateTestCertificate(string subjectName)
        {
            if (!ValidateCertificateName(subjectName))
            {
                throw new ArgumentException("Invalid certificate name. Use only letters, numbers, spaces, and the following characters: -_.", nameof(subjectName));
            }

            using (var rsa = RSA.Create(4096))
            {
                var req = new CertificateRequest(
                    $"CN={subjectName}",
                    rsa,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1);

                // Add enhanced key usage for code signing
                req.CertificateExtensions.Add(
                    new X509EnhancedKeyUsageExtension(
                        new OidCollection {
                            new Oid("1.3.6.1.5.5.7.3.3")  // Code Signing
                        },
                        true));

                // Add key usage
                req.CertificateExtensions.Add(
                    new X509KeyUsageExtension(
                        X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.KeyCertSign,
                        true));

                // Add basic constraints
                req.CertificateExtensions.Add(
                    new X509BasicConstraintsExtension(true, false, 0, true));

                var certificate = req.CreateSelfSigned(
                    DateTimeOffset.Now.AddDays(-1),  // Start time is yesterday to avoid timing issues
                    DateTimeOffset.Now.AddYears(5)); // Valid for 5 years

                // Create a temporary password for the PFX
                var password = Guid.NewGuid().ToString();

                // Export with private key to PFX and reimport to ensure proper private key handling
                var pfxData = certificate.Export(X509ContentType.Pfx, password);
                return new X509Certificate2(pfxData, password, X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
            }
        }

        /// <summary>
        /// Installs certificate to the specified store
        /// </summary>
        /// <param name="certificate">Certificate to install</param>
        public void InstallCertificate(X509Certificate2 certificate)
        {
            using (var store = new X509Store(CertificateStoreName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
        }

        /// <summary>
        /// Removes certificate from the store
        /// </summary>
        /// <param name="thumbprint">Certificate thumbprint</param>
        public void RemoveCertificate(string thumbprint)
        {
            if (!ValidateCertificateName(thumbprint))
            {
                throw new ArgumentException("Invalid certificate thumbprint. Use only letters, numbers, spaces, and the following characters: -_.", nameof(thumbprint));
            }

            using (var store = new X509Store(CertificateStoreName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                var cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (cert.Count > 0)
                {
                    store.Remove(cert[0]);
                }
            }
        }

        /// <summary>
        /// Exports certificate from the store to a PFX file
        /// </summary>
        /// <param name="thumbprint">Certificate thumbprint</param>
        /// <param name="exportPath">Path where to save the PFX file</param>
        /// <returns>True if certificate was exported successfully, false if certificate was not found</returns>
        public bool ExportCertificate(string thumbprint, string exportPath)
        {
            if (!ValidateCertificateName(thumbprint))
            {
                throw new ArgumentException("Invalid certificate thumbprint. Use only letters, numbers, spaces, and the following characters: -_.", nameof(thumbprint));
            }

            using (var store = new X509Store(CertificateStoreName, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadOnly);
                var cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, false);
                if (cert.Count > 0)
                {
                    var password = Guid.NewGuid().ToString();
                    var pfxData = cert[0].Export(X509ContentType.Pfx, password);
                    File.WriteAllBytes(exportPath, pfxData);
                    
                    // Write password to a separate file
                    var passwordFile = Path.ChangeExtension(exportPath, ".txt");
                    File.WriteAllText(passwordFile, $"Certificate Password: {password}");
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Imports a certificate from a PFX file and installs it to both MY and ROOT stores
        /// </summary>
        /// <param name="pfxPath">Path to the PFX file</param>
        /// <param name="password">Password for the PFX file</param>
        public void ImportCertificate(string pfxPath, string password)
        {
            // load certificate from file
            var certificate = new X509Certificate2(pfxPath, password, 
                X509KeyStorageFlags.Exportable | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);

            // install in MY (Personal)
            using (var store = new X509Store(StoreName.My, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }

            // install in ROOT (Trusted Root Certification Authorities)
            using (var store = new X509Store(StoreName.Root, StoreLocation.LocalMachine))
            {
                store.Open(OpenFlags.ReadWrite);
                store.Add(certificate);
            }
        }
    }
} 