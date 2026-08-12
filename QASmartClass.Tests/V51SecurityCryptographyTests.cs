using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;
using QASmartClass.Services;
using QASmartClass.Utilities;
using Xunit;

namespace QASmartClass.Tests
{
    public class V51SecurityCryptographyTests : IDisposable
    {
        private readonly string _tempDbPath;
        private readonly string _tempVersionPath;

        public V51SecurityCryptographyTests()
        {
            QASmartClass.Staff.Services.ServiceRegistration.Initialize();
            if (System.Windows.Application.Current == null)
            {
                try
                {
                    new System.Windows.Application();
                }
                catch { }
            }

            string guid = Guid.NewGuid().ToString("N");
            _tempDbPath = Path.Combine(AppPaths.RootDir, $"smartclass_test_{guid}.db");
            _tempVersionPath = Path.Combine(AppPaths.RootDir, $"db_version_{guid}.txt");

            AppPaths.DatabaseFile = _tempDbPath;
            AppPaths.DbVersionFile = _tempVersionPath;
            AppPaths.EnsureDirectories();

            using var db = new AppDbContext();
            DbMigrator.Migrate(db, "5.36.0");
        }

        public void Dispose()
        {
            try
            {
                if (File.Exists(_tempDbPath)) File.Delete(_tempDbPath);
                if (File.Exists(_tempVersionPath)) File.Delete(_tempVersionPath);
            }
            catch { }
        }

        [Fact]
        public void CryptoHelper_AesEncryptDecrypt_ReturnsOriginalText()
        {
            // Arrange
            string originalText = "Hello world! This is a sensitive TCP message.";
            string key = "DynamicSessionKeyForTCPTransmission";

            // Act
            string cipherText = CryptoHelper.Encrypt(originalText, key);
            string decryptedText = CryptoHelper.Decrypt(cipherText, key);

            // Assert
            Assert.NotEqual(originalText, cipherText);
            Assert.Equal(originalText, decryptedText);
        }

        [Fact]
        public void CryptoHelper_AesDecrypt_HandlesMalformedInputGracefully()
        {
            // Arrange
            string malformedBase64 = "InvalidBase64Characters!!!";
            string key = "DynamicSessionKeyForTCPTransmission";

            // Act
            string decryptedText = CryptoHelper.Decrypt(malformedBase64, key);

            // Assert
            Assert.Equal(string.Empty, decryptedText);
        }

        [Fact]
        public void CryptoHelper_AesDecrypt_HandlesEmptyOrNullInputGracefully()
        {
            // Arrange
            string key = "DynamicSessionKeyForTCPTransmission";

            // Act & Assert
            Assert.Equal(string.Empty, CryptoHelper.Decrypt(null!, key));
            Assert.Equal(string.Empty, CryptoHelper.Decrypt(string.Empty, key));
        }

        [Fact]
        public void CryptoHelper_ComputeHMAC_ReturnsConsistentHexLowercase()
        {
            // Arrange
            string input = "HS001" + 15000.00m.ToString("F2");

            // Act
            string hmac1 = CryptoHelper.ComputeHMAC(input);
            string hmac2 = CryptoHelper.ComputeHMAC(input);

            // Assert
            Assert.Equal(hmac1, hmac2);
            Assert.True(hmac1.All(c => char.IsDigit(c) || (c >= 'a' && c <= 'f')), "HMAC should be lowercase hex");
        }

        [Fact]
        public void AppDbContext_StudentWallet_AutoGeneratesChecksumOnSave()
        {
            using (var db = new AppDbContext())
            {
                // Arrange
                string uniqueCode = "HS_V51_T1_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var student = new Student
                {
                    FullName = "Crypto Test Student",
                    StudentCode = uniqueCode,
                    ClassName = "11A5",
                    WalletBalance = 25000.50m,
                    Status = "Active"
                };

                // Act
                db.Students.Add(student);
                db.SaveChanges();

                // Reload to verify it was generated and stored
                var reloadedStudent = db.Students.First(s => s.StudentCode == uniqueCode);
                string expectedChecksum = CryptoHelper.ComputeHMAC(
                    reloadedStudent.StudentCode + reloadedStudent.WalletBalance.ToString("F2")
                );

                // Assert
                Assert.False(string.IsNullOrEmpty(reloadedStudent.WalletChecksum));
                Assert.Equal(expectedChecksum, reloadedStudent.WalletChecksum);
            }
        }

        [Fact]
        public void AppDbContext_StudentWallet_DetectsTamperingAndLocksWallet()
        {
            using (var db = new AppDbContext())
            {
                // Arrange
                string uniqueCode = "HS_V51_T2_" + Guid.NewGuid().ToString("N").Substring(0, 8);
                var student = new Student
                {
                    FullName = "Crypto Mismatch Student",
                    StudentCode = uniqueCode,
                    ClassName = "11A5",
                    WalletBalance = 50000.00m,
                    Status = "Active"
                };
                db.Students.Add(student);
                db.SaveChanges();

                // We modify the checksum to be invalid directly in the database using SQL command,
                // simulating database tampering outside EF Core's Tracked/Modified change detection.
                db.Database.ExecuteSqlRaw(
                    "UPDATE Students SET WalletBalance = 999999.00, WalletChecksum = 'TAMPERED_CHECKSUM' WHERE StudentCode = {0}",
                    uniqueCode
                );

                // Act - Create a new context and load the student to trigger tracked checksum check
                using (var newDb = new AppDbContext())
                {
                    var reloadedStudent = newDb.Students.First(s => s.StudentCode == uniqueCode);

                    // Assert
                    Assert.Equal(0.0m, reloadedStudent.WalletBalance);
                    Assert.True(reloadedStudent.IsAtRisk);
                    Assert.Contains("Phát hiện sửa đổi số dư trái phép", reloadedStudent.RiskReason);
                }
            }
        }

        [Fact]
        public void Security_RsaHandshakeAndSessionKeyExchange_Succeeds()
        {
            // Client side: Generate RSA key pair
            using var clientRsa = System.Security.Cryptography.RSA.Create(2048);
            string clientPubKeyBase64 = Convert.ToBase64String(clientRsa.ExportRSAPublicKey());

            // Server side: Receive client public key
            Assert.False(string.IsNullOrEmpty(clientPubKeyBase64));
            
            // Server side: Generate dynamic SessionKey using CSPRNG
            byte[] keyBytes = new byte[16];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(keyBytes);
            }
            string serverSessionKey = Convert.ToHexString(keyBytes);
            Assert.Equal(32, serverSessionKey.Length);

            // Server side: Encrypt SessionKey using client's public key
            using var serverRsa = System.Security.Cryptography.RSA.Create();
            serverRsa.ImportRSAPublicKey(Convert.FromBase64String(clientPubKeyBase64), out _);
            byte[] encryptedData = serverRsa.Encrypt(
                System.Text.Encoding.UTF8.GetBytes(serverSessionKey), 
                System.Security.Cryptography.RSAEncryptionPadding.Pkcs1
            );
            string encryptedSessionKeyBase64 = Convert.ToBase64String(encryptedData);

            // Client side: Receive encrypted SessionKey, decrypt it using client's private key
            byte[] encryptedBytes = Convert.FromBase64String(encryptedSessionKeyBase64);
            byte[] decryptedBytes = clientRsa.Decrypt(
                encryptedBytes, 
                System.Security.Cryptography.RSAEncryptionPadding.Pkcs1
            );
            string clientDecryptedSessionKey = System.Text.Encoding.UTF8.GetString(decryptedBytes);

            // Assert
            Assert.Equal(serverSessionKey, clientDecryptedSessionKey);
        }
    }
}
