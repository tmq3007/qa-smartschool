using Xunit;
using System;
using System.Threading;
using System.Windows;
using System.Runtime.InteropServices;
using QASmartClass.Services;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using QASmartClass.StudentClient.Services;
using QASmartClass.Utilities;

namespace QASmartClass.Tests
{
    public class V88TeacherAccessibilityTests
    {
        [Fact]
        public void ClassControlService_StatePersistence_Verify()
        {
            var control = ClassControlService.Instance;
            
            // Set initial state
            control.IsExitPinRequired = true;
            control.ExitPinCode = "123456";
            
            Assert.True(control.IsExitPinRequired);
            Assert.Equal("123456", control.ExitPinCode);
            
            // Reset state
            control.IsExitPinRequired = false;
            control.ExitPinCode = string.Empty;
            
            Assert.False(control.IsExitPinRequired);
            Assert.Equal(string.Empty, control.ExitPinCode);
        }

        [Fact]
        public void ComputeSha256Hash_Verify()
        {
            string pin = "2026";
            string salt = "10A3_MATH_2026";
            
            string hash1 = ClassControlService.ComputeSha256Hash(pin, salt);
            string hash2 = ClassControlService.ComputeSha256Hash(pin, salt);
            
            Assert.Equal(hash1, hash2);
            Assert.False(string.IsNullOrEmpty(hash1));
            Assert.Equal(64, hash1.Length); // SHA-256 hex string has 64 characters
            
            string differentHash = ClassControlService.ComputeSha256Hash(pin, "DIFFERENT_SALT");
            Assert.NotEqual(hash1, differentHash);
        }

        [Fact]
        public void ClassControlService_DynamicSessionSalt_Verify()
        {
            var control = ClassControlService.Instance;
            
            // Initial state
            control.Reset();
            Assert.Equal(string.Empty, control.SessionSalt);
            
            // Initialize first session
            control.InitializeNewSession();
            string salt1 = control.SessionSalt;
            Assert.False(string.IsNullOrEmpty(salt1));
            Assert.Equal(16, salt1.Length);
            
            // Initialize second session
            control.InitializeNewSession();
            string salt2 = control.SessionSalt;
            Assert.False(string.IsNullOrEmpty(salt2));
            Assert.Equal(16, salt2.Length);
            Assert.NotEqual(salt1, salt2); // Must be different GUID substrings
            
            // Verify PIN băm behaves dynamically with different session salts
            string pin = "888888";
            string hashWithSalt1 = ClassControlService.ComputeSha256Hash(pin, salt1);
            string hashWithSalt2 = ClassControlService.ComputeSha256Hash(pin, salt2);
            
            Assert.NotEqual(hashWithSalt1, hashWithSalt2);
        }

        [Fact]
        public void KeyboardHookHelper_VirtualDesktopAndWindowsKeys_Blocked()
        {
            var method = typeof(QASmartClass.StudentClient.Services.KeyboardHookHelper).GetMethod("HookCallback", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            
            Assert.NotNull(method);

            // Prepare lParam containing vkCode
            IntPtr lParam = Marshal.AllocHGlobal(sizeof(int));
            try
            {
                // Test vkCode = 91 (Left Win Key)
                Marshal.WriteInt32(lParam, 91);
                var result91 = method.Invoke(null, new object[] { 0, (IntPtr)0x0100, lParam }); // WM_KEYDOWN = 0x0100
                Assert.Equal(new IntPtr(1), result91);

                // Test vkCode = 92 (Right Win Key)
                Marshal.WriteInt32(lParam, 92);
                var result92 = method.Invoke(null, new object[] { 0, (IntPtr)0x0100, lParam });
                Assert.Equal(new IntPtr(1), result92);

                // Test vkCode = 65 (Key 'A', should not block)
                Marshal.WriteInt32(lParam, 65);
                var resultA = method.Invoke(null, new object[] { 0, (IntPtr)0x0100, lParam });
                Assert.NotEqual(new IntPtr(1), resultA);
            }
            finally
            {
                Marshal.FreeHGlobal(lParam);
            }
        }

        [Fact]
        public void PolicyPage_PresetSerialization_Verify()
        {
            // Find the nested PresetData type
            var policyPageType = typeof(QASmartClass.Classroom.Views.PolicyPage);
            var presetDataType = policyPageType.GetNestedType("PresetData", System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(presetDataType);

            // Create an instance of PresetData
            var presetInstance = Activator.CreateInstance(presetDataType);
            Assert.NotNull(presetInstance);

            // Set properties
            var blockInternetProp = presetDataType.GetProperty("BlockInternet");
            var exitPinProp = presetDataType.GetProperty("ExitPin");
            
            Assert.NotNull(blockInternetProp);
            Assert.NotNull(exitPinProp);

            blockInternetProp.SetValue(presetInstance, true);
            exitPinProp.SetValue(presetInstance, "123456");

            // Serialize to JSON
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(presetInstance);
            Assert.Contains("\"BlockInternet\":true", json);
            Assert.Contains("\"ExitPin\":\"123456\"", json);

            // Deserialize from JSON
            var deserializedInstance = Newtonsoft.Json.JsonConvert.DeserializeObject(json, presetDataType);
            Assert.NotNull(deserializedInstance);

            bool blockValue = (bool)blockInternetProp.GetValue(deserializedInstance);
            string pinValue = (string)exitPinProp.GetValue(deserializedInstance);

            Assert.True(blockValue);
            Assert.Equal("123456", pinValue);
        }

        [Fact]
        public void PolicyPage_CorruptPresetFallback_Verify()
        {
            var policyPageType = typeof(QASmartClass.Classroom.Views.PolicyPage);
            var presetDataType = policyPageType.GetNestedType("PresetData", System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(presetDataType);

            // Corrupted JSON string (missing closing brackets and property values)
            string corruptJson = "{BlockInternet: true, ExitPin: ";

            object? deserialized = null;
            bool caught = false;
            try
            {
                deserialized = Newtonsoft.Json.JsonConvert.DeserializeObject(corruptJson, presetDataType);
            }
            catch (Newtonsoft.Json.JsonException)
            {
                caught = true;
            }

            Assert.True(caught);
            Assert.Null(deserialized);
        }

        [Fact]
        public void NetworkCryptoHelper_EncryptDecrypt_Verify()
        {
            string originalCommand = "POLICY|internet=False|social=True|id=105";
            string sessionSalt = Guid.NewGuid().ToString("N");

            // Encrypt using helper
            string encrypted = QASmartClass.Utilities.NetworkCryptoHelper.EncryptCommand(originalCommand, sessionSalt);
            Assert.StartsWith("ENC_CMD|", encrypted);

            // Decrypt using helper
            string decrypted = QASmartClass.Utilities.NetworkCryptoHelper.DecryptCommand(encrypted, sessionSalt);
            Assert.Equal(originalCommand, decrypted);

            // Test fallback empty session salt encryption/decryption
            string emptySessionSalt = string.Empty;
            string encryptedFallback = QASmartClass.Utilities.NetworkCryptoHelper.EncryptCommand(originalCommand, emptySessionSalt);
            Assert.StartsWith("ENC_CMD|", encryptedFallback);

            string decryptedFallback = QASmartClass.Utilities.NetworkCryptoHelper.DecryptCommand(encryptedFallback, emptySessionSalt);
            Assert.Equal(originalCommand, decryptedFallback);

            // Test decryption failure with incorrect salt
            string wrongSessionSalt = "wrong_salt_value";
            string decryptedWrong = QASmartClass.Utilities.NetworkCryptoHelper.DecryptCommand(encrypted, wrongSessionSalt);
            Assert.NotEqual(originalCommand, decryptedWrong);
        }

        [Fact]
        public void Preset_ImportExport_Structures_Verify()
        {
            var policyPageType = typeof(QASmartClass.Classroom.Views.PolicyPage);
            var presetDataType = policyPageType.GetNestedType("PresetData", System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(presetDataType);

            // Test custom JSON structure with AppWhitelist
            string sampleJson = @"{
                ""BlockInternet"": true,
                ""EduOnly"": false,
                ""BlockSocial"": true,
                ""BlockGames"": true,
                ""LockDesktop"": false,
                ""ShowTeacherScreen"": false,
                ""QuietMode"": true,
                ""DisableTaskbar"": false,
                ""BlockUsb"": true,
                ""BlockApps"": true,
                ""WhitelistOnly"": true,
                ""BlockPrint"": false,
                ""RequireExitPin"": true,
                ""ExitPin"": ""999999"",
                ""AppWhitelist"": [""notepad.exe"", ""cmd.exe""]
            }";

            var data = Newtonsoft.Json.JsonConvert.DeserializeObject(sampleJson, presetDataType);
            Assert.NotNull(data);

            var blockInternetProp = presetDataType.GetProperty("BlockInternet");
            var quietModeProp = presetDataType.GetProperty("QuietMode");
            var exitPinProp = presetDataType.GetProperty("ExitPin");
            var appWhitelistProp = presetDataType.GetProperty("AppWhitelist");

            Assert.NotNull(blockInternetProp);
            Assert.NotNull(quietModeProp);
            Assert.NotNull(exitPinProp);
            Assert.NotNull(appWhitelistProp);

            Assert.True((bool)blockInternetProp.GetValue(data));
            Assert.True((bool)quietModeProp.GetValue(data));
            Assert.Equal("999999", (string)exitPinProp.GetValue(data));

            var whitelist = (System.Collections.Generic.List<string>)appWhitelistProp.GetValue(data);
            Assert.NotNull(whitelist);
            Assert.Equal(2, whitelist.Count);
            Assert.Contains("notepad.exe", whitelist);
            Assert.Contains("cmd.exe", whitelist);
        }

        [Fact]
        public async Task StudentNetworkClient_PingCommand_RepliesPong()
        {
            // Start a local TCP listener on a free port
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;

            // Start accepting a client in a background task
            var acceptTask = Task.Run(async () =>
            {
                using var serverSocket = await listener.AcceptTcpClientAsync();
                using var stream = serverSocket.GetStream();

                // Read JOIN message
                var buffer = new byte[2048];
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                string joinMsg = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                Assert.Contains("JOIN|", joinMsg);

                // Send Handshake ACK back
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                string sessionKey = "testSessionKey123";
                string sessionSalt = "testSessionSalt45";
                string ackMessage = $"OK|guid|{currentTimestamp}|{sessionKey}|TEST_CLASS|{sessionSalt}";
                byte[] ackBytes = Encoding.UTF8.GetBytes(ackMessage);
                await stream.WriteAsync(ackBytes, 0, ackBytes.Length);

                // Construct a signed CMD|PING command
                string cmdId = "ping999";
                string commandPayload = $"CMD|PING|id={cmdId}|{currentTimestamp}";

                // Compute HMAC-SHA256 signature using sessionSalt
                string signature;
                using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(sessionSalt)))
                {
                    byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(commandPayload));
                    signature = Convert.ToBase64String(hash);
                }

                string signedPing = $"{commandPayload}|{signature}\n";
                byte[] pingBytes = Encoding.UTF8.GetBytes(signedPing);
                await stream.WriteAsync(pingBytes, 0, pingBytes.Length);

                // Read the response from client
                byte[] responseBuffer = new byte[2048];
                int responseBytesRead = await stream.ReadAsync(responseBuffer, 0, responseBuffer.Length);
                string responseStr = Encoding.UTF8.GetString(responseBuffer, 0, responseBytesRead).Trim();

                // Decrypt the response using CryptoHelper
                string decryptedResponse = CryptoHelper.Decrypt(responseStr, sessionKey);
                
                // Assert it matches expected PONG format
                Assert.Contains($"ACK|{cmdId}|", decryptedResponse);
                Assert.Contains("|SUCCESS|PONG", decryptedResponse);
            });

            // Start the StudentNetworkClient and connect to the local listener
            using (var client = new StudentNetworkClient())
            {
                client.StudentName = "TestStudent";
                client.StudentCode = "TS001";

                await client.ConnectDirectAsync("127.0.0.1", port);

                // Wait for the server verification task to complete
                await acceptTask;
                client.Stop();
            }

            listener.Stop();
        }
    }
}
