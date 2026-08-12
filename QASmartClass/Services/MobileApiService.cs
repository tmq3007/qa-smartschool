using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QASmartClass.Data;

namespace QASmartClass.Services
{
    public class MobileApiService
    {
        private readonly AppDbContext _db;
        public static bool SimulateNetworkError { get; set; } = false;

        public MobileApiService(AppDbContext dbContext)
        {
            _db = dbContext;
        }

        public MobileToken RegisterDevice(int userId, string role, string deviceToken, string deviceName, string platform)
        {
            var autoApproveSetting = _db.SystemSettings.FirstOrDefault(s => s.Id == "IT_Mobile_AutoApprove");
            bool isAutoApprove = autoApproveSetting == null || string.Equals(autoApproveSetting.Value, "Enabled", StringComparison.OrdinalIgnoreCase);

            var existingToken = _db.MobileTokens.FirstOrDefault(t => t.DeviceToken == deviceToken);
            if (existingToken != null)
            {
                existingToken.UserId = userId;
                existingToken.Role = role;
                existingToken.DeviceName = deviceName;
                existingToken.Platform = platform;
                existingToken.LastActive = DateTime.Now;
                
                if (existingToken.ApprovalStatus == "Approved")
                {
                    existingToken.IsActive = true;
                }
                else if (existingToken.ApprovalStatus == "Rejected")
                {
                    existingToken.IsActive = false;
                }
                else // Pending
                {
                    if (isAutoApprove)
                    {
                        existingToken.ApprovalStatus = "Approved";
                        existingToken.IsActive = true;
                    }
                    else
                    {
                        existingToken.IsActive = false;
                    }
                }
                _db.SaveChanges();
                return existingToken;
            }

            var newToken = new MobileToken
            {
                UserId = userId,
                Role = role,
                DeviceToken = deviceToken,
                DeviceName = deviceName,
                Platform = platform,
                LastActive = DateTime.Now,
                ApprovalStatus = isAutoApprove ? "Approved" : "Pending",
                IsActive = isAutoApprove
            };

            _db.MobileTokens.Add(newToken);
            _db.SaveChanges();
            return newToken;
        }

        public List<MobileToken> GetActiveDevices()
        {
            return _db.MobileTokens.Where(t => t.IsActive && t.ApprovalStatus == "Approved").OrderByDescending(t => t.LastActive).ToList();
        }

        public async Task<List<MobileToken>> GetActiveDevicesAsync()
        {
            return await _db.MobileTokens.Where(t => t.IsActive && t.ApprovalStatus == "Approved").OrderByDescending(t => t.LastActive).ToListAsync();
        }

        public List<MobileToken> GetAllDevices()
        {
            return _db.MobileTokens.OrderByDescending(t => t.LastActive).ToList();
        }

        public async Task<List<MobileToken>> GetAllDevicesAsync()
        {
            return await _db.MobileTokens.OrderByDescending(t => t.LastActive).ToListAsync();
        }

        public bool DeactivateDevice(int tokenId)
        {
            var token = _db.MobileTokens.Find(tokenId);
            if (token != null)
            {
                token.IsActive = false;
                _db.SaveChanges();
                return true;
            }
            return false;
        }

        public async Task<bool> DeactivateDeviceAsync(int tokenId)
        {
            var token = await _db.MobileTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.IsActive = false;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> ApproveDeviceAsync(int tokenId)
        {
            var token = await _db.MobileTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.ApprovalStatus = "Approved";
                token.IsActive = true;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RejectDeviceAsync(int tokenId)
        {
            var token = await _db.MobileTokens.FindAsync(tokenId);
            if (token != null)
            {
                token.ApprovalStatus = "Rejected";
                token.IsActive = false;
                await _db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public PushMessageLog SendPushNotification(int recipientId, string role, string title, string body, string type = "General", string targetClass = "", string targetGrade = "")
        {
            // Note: In real app, this would call Firebase Cloud Messaging (FCM) API here.
            // For now, we mock the sending and log it to DB.

            var log = new PushMessageLog
            {
                RecipientId = recipientId,
                RecipientRole = role,
                Title = title,
                Body = body,
                Type = type,
                SentAt = DateTime.Now,
                Status = "Delivered", // Mock successful delivery
                TargetClass = targetClass,
                TargetGrade = targetGrade
            };

            _db.PushMessageLogs.Add(log);
            _db.SaveChanges();
            return log;
        }

        public async Task<PushMessageLog> SendPushNotificationAsync(int recipientId, string role, string title, string body, string type = "General", string targetClass = "", string targetGrade = "")
        {
            if (SimulateNetworkError)
            {
                throw new System.Net.Http.HttpRequestException("Network connection timeout simulating FCM error");
            }

            var log = new PushMessageLog
            {
                RecipientId = recipientId,
                RecipientRole = role,
                Title = title,
                Body = body,
                Type = type,
                SentAt = DateTime.Now,
                Status = "Delivered",
                TargetClass = targetClass,
                TargetGrade = targetGrade
            };

            _db.PushMessageLogs.Add(log);
            await _db.SaveChangesAsync();
            return log;
        }

        public List<PushMessageLog> GetPushMessageLogs(int recipientId, string role)
        {
            return _db.PushMessageLogs
                .Where(m => (m.RecipientId == recipientId || m.RecipientId == 0) && m.RecipientRole == role)
                .OrderByDescending(m => m.SentAt)
                .Take(50)
                .ToList();
        }
    }
}
