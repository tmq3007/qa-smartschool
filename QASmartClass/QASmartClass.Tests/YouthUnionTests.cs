using QASmartClass.Data;
using QASmartClass.Staff.Services;
using System;
using System.Linq;
using Xunit;

namespace QASmartClass.Tests
{
    public class YouthUnionTests
    {
        [Fact]
        public void SeedYouthUnionData_ShouldNotSeedIfMembersExist()
        {
            using var db = TestDbFactory.Create();

            // Setup basic student first (since seeder looks for students)
            var student = new Student { FullName = "Nguyen Van A", StudentCode = "HS001", ClassName = "10A1" };
            db.Students.Add(student);
            db.SaveChanges();

            // Run seeder once
            StaffDataSeeder.SeedAll(db);
            var initialCount = db.YouthMembers.Count();
            Assert.True(initialCount > 0);

            // Run seeder second time
            StaffDataSeeder.SeedAll(db);
            var secondCount = db.YouthMembers.Count();
            Assert.Equal(initialCount, secondCount);
        }

        [Fact]
        public void YouthEmulationScore_NullableActivityId_Works()
        {
            using var db = TestDbFactory.Create();
            var score = new YouthEmulationScore
            {
                MemberId = 1,
                Score = 10,
                Category = "Activity",
                Reason = "Participated in Event",
                AwardedBy = "GV001",
                ActivityId = null // Test nullable FK
            };

            db.YouthEmulationScores.Add(score);
            db.SaveChanges();

            var savedScore = db.YouthEmulationScores.First();
            Assert.Null(savedScore.ActivityId);
        }

        [Fact]
        public void YouthRecruitment_NullableStudentId_Works()
        {
            using var db = TestDbFactory.Create();
            var recruitment = new YouthRecruitment
            {
                StudentName = "New Candidate",
                ClassName = "10A2",
                Status = "Applied",
                Reason = "Wants to join",
                StudentId = null // Test nullable FK
            };

            db.YouthRecruitments.Add(recruitment);
            db.SaveChanges();

            var savedRecruitment = db.YouthRecruitments.First();
            Assert.Null(savedRecruitment.StudentId);
        }
    }
}

