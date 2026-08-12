using System;
using System.IO;
using Xunit;
using QASmartTouch.Services;

namespace QASmartClass.Tests
{
    public class V84TeacherLoginUpgradeTests : IDisposable
    {
        public V84TeacherLoginUpgradeTests()
        {
            // Reset state before each test
            BruteForceGuard.ResetLockoutState();
        }

        public void Dispose()
        {
            // Reset state after each test
            BruteForceGuard.ResetLockoutState();
        }

        [Fact]
        public void BruteForceGuard_IncrementFailedAttempts_ShouldAccumulate()
        {
            // Initial state
            Assert.False(BruteForceGuard.IsCurrentlyLocked(out _));

            // Increment failed attempts
            int attempts1 = BruteForceGuard.IncrementFailedAttempts();
            Assert.Equal(1, attempts1);

            int attempts2 = BruteForceGuard.IncrementFailedAttempts();
            Assert.Equal(2, attempts2);

            Assert.False(BruteForceGuard.IsCurrentlyLocked(out _));
        }

        [Fact]
        public void BruteForceGuard_SetLockoutExpiration_ShouldLockoutCorrectly()
        {
            // Lockout for 5 seconds
            BruteForceGuard.SetLockoutExpiration(5);

            bool isLocked = BruteForceGuard.IsCurrentlyLocked(out int remainingSeconds);
            Assert.True(isLocked);
            Assert.True(remainingSeconds > 0 && remainingSeconds <= 5);
        }

        [Fact]
        public void BruteForceGuard_ResetLockoutState_ShouldClearExistingLock()
        {
            // Set lockout
            BruteForceGuard.SetLockoutExpiration(10);
            Assert.True(BruteForceGuard.IsCurrentlyLocked(out _));

            // Reset
            BruteForceGuard.ResetLockoutState();
            Assert.False(BruteForceGuard.IsCurrentlyLocked(out _));
        }
    }
}
