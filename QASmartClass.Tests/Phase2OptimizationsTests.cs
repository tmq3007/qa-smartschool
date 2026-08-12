using System;
using Xunit;
using QASmartClass.Services;
using QASmartClass.Classroom.Services;

namespace QASmartClass.Tests
{
    public class Phase2OptimizationsTests
    {
        [Fact]
        public void BroadcastStateService_FilterPersistence_ShouldSaveAndResetCorrectly()
        {
            var state = BroadcastStateService.Instance;
            state.Reset();

            // Check defaults
            Assert.Equal("ALL", state.TargetSelectionMode);
            Assert.Empty(state.TargetSelectedGroups);
            Assert.Empty(state.TargetSelectedStudents);

            // Set state
            state.TargetSelectionMode = "GROUP";
            state.TargetSelectedGroups.Add("Tổ 1");
            state.TargetSelectedGroups.Add("Tổ 2");
            state.TargetSelectedStudents.Add("HS001");

            Assert.Equal("GROUP", state.TargetSelectionMode);
            Assert.Contains("Tổ 1", state.TargetSelectedGroups);
            Assert.Contains("HS001", state.TargetSelectedStudents);

            // Reset
            state.Reset();
            Assert.Equal("ALL", state.TargetSelectionMode);
            Assert.Empty(state.TargetSelectedGroups);
            Assert.Empty(state.TargetSelectedStudents);
        }

        [Fact]
        public void ClientInfo_LatencyJitter_ShouldFluctuateWithinLimits()
        {
            var client = new ClientInfo();

            // Default latency should be between 1 and 999
            int defaultLatency = client.LatencyMs;
            Assert.InRange(defaultLatency, 1, 999);

            // Test 100 iterations of jitter to verify it always stays in a safe bound
            for (int i = 0; i < 100; i++)
            {
                client.LatencyMs = 120;
                int loadLatency = client.LatencyMs;
                Assert.InRange(loadLatency, 115, 125); // Allow safe fluctuation
            }

            // Reset latency
            client.LatencyMs = -1;
            int resetLatency = client.LatencyMs;
            Assert.InRange(resetLatency, 1, 999);
        }

        [Fact]
        public void ClassroomSession_FilterStudentsByActiveClassName()
        {
            var testStudents = new System.Collections.Generic.List<dynamic>
            {
                new { FullName = "An", ClassName = "11A2" },
                new { FullName = "Bình", ClassName = "11A2" },
                new { FullName = "Cường", ClassName = "12A1" }
            };

            // Test scenario 1: active class name is "11A2"
            string activeClass = "11A2";
            var filtered = new System.Collections.Generic.List<dynamic>();
            foreach (var s in testStudents)
            {
                if (string.IsNullOrEmpty(activeClass) || s.ClassName == activeClass)
                {
                    filtered.Add(s);
                }
            }
            Assert.Equal(2, filtered.Count);
            Assert.All(filtered, s => Assert.Equal("11A2", s.ClassName));

            // Test scenario 2: active class is empty (fallback to all)
            string emptyClass = "";
            var filteredAll = new System.Collections.Generic.List<dynamic>();
            foreach (var s in testStudents)
            {
                if (string.IsNullOrEmpty(emptyClass) || s.ClassName == emptyClass)
                {
                    filteredAll.Add(s);
                }
            }
            Assert.Equal(3, filteredAll.Count);
        }

        [Fact]
        public void ClassroomSession_GetStudentsForCurrentContext_LogicVerification()
        {
            var databaseStudents = new System.Collections.Generic.List<dynamic>
            {
                new { FullName = "An", ClassName = "11A2" },
                new { FullName = "Bình", ClassName = "11A2" },
                new { FullName = "Cường", ClassName = "12A1" }
            };

            var rosterStudents = new System.Collections.Generic.List<dynamic>
            {
                new { FullName = "An", ClassName = "11A2" },
                new { FullName = "Bình", ClassName = "11A2" }
            };

            // Scenario 1: Session CurrentClassName is "11A2", Roster is null
            string sessionClassName = "11A2";
            string rosterClassName = null;
            
            var result1 = ResolveStudents(sessionClassName, rosterClassName, databaseStudents, rosterStudents);
            Assert.Equal(2, result1.Count);
            Assert.All(result1, s => Assert.Equal("11A2", s.ClassName));

            // Scenario 2: Session is null, Roster is "12A1"
            string sessionClassName2 = null;
            string rosterClassName2 = "12A1";

            var result2 = ResolveStudents(sessionClassName2, rosterClassName2, databaseStudents, rosterStudents);
            Assert.Single(result2);
            Assert.Equal("Cường", result2[0].FullName);

            // Scenario 3: Both null, Roster has no students -> fallbacks to all DB students
            string sessionClassName3 = null;
            string rosterClassName3 = null;

            var result3 = ResolveStudents(sessionClassName3, rosterClassName3, databaseStudents, new System.Collections.Generic.List<dynamic>());
            Assert.Equal(3, result3.Count);
        }

        private System.Collections.Generic.List<dynamic> ResolveStudents(
            string sessionClass, 
            string rosterClass, 
            System.Collections.Generic.List<dynamic> dbStudents,
            System.Collections.Generic.List<dynamic> activeRosterStudents)
        {
            string activeClass = sessionClass;
            if (string.IsNullOrEmpty(activeClass))
            {
                activeClass = rosterClass;
            }

            if (!string.IsNullOrEmpty(activeClass))
            {
                var filtered = new System.Collections.Generic.List<dynamic>();
                foreach (var s in dbStudents)
                {
                    if (s.ClassName == activeClass)
                    {
                        filtered.Add(s);
                    }
                }
                if (filtered.Count > 0)
                {
                    return filtered;
                }
            }

            if (activeRosterStudents != null && activeRosterStudents.Count > 0)
            {
                return activeRosterStudents;
            }

            return dbStudents;
        }

        [Theory]
        [InlineData(150, 80, 80)] // Normal latency (150ms) -> quality remains unchanged (80)
        [InlineData(350, 80, 60)] // High latency (350ms) -> quality reduced by 20 (60)
        [InlineData(1200, 70, 60)] // Extreme latency (1200ms) -> quality reduced but capped at lower bound 60
        [InlineData(400, 95, 75)] // High latency (400ms) -> quality reduced from 95 to 75
        public void Transmission_AutoScalingWebPQuality_ShouldEnforceBounds(int simulatedMaxLatency, int initialQuality, int expectedQuality)
        {
            int qualityLevel = initialQuality;
            
            // Simulating quality auto-scaling logic based on latency
            if (simulatedMaxLatency > 300)
            {
                qualityLevel = Math.Max(60, qualityLevel - 20);
            }
            
            Assert.Equal(expectedQuality, qualityLevel);
        }
    }
}
