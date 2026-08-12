using System;
using System.Threading.Tasks;
using Xunit;
using QASmartClass.Services;

namespace QASmartClass.Tests.Services
{
    public class LessonStateServiceTests
    {
        [Fact]
        public void Instance_Should_Be_Singleton()
        {
            var a = LessonStateService.Instance;
            var b = LessonStateService.Instance;
            Assert.Same(a, b);
        }

        [Fact]
        public void Default_LessonId_Is_Zero()
        {
            var svc = new LessonStateService();
            Assert.Equal(0, svc.ActiveLessonId);
        }

        [Fact]
        public void Default_IsActive_Is_False()
        {
            var svc = new LessonStateService();
            Assert.False(svc.IsLessonActive);
        }

        [Fact]
        public void SetAndGet_LessonId()
        {
            var svc = new LessonStateService();
            svc.ActiveLessonId = 42;
            Assert.Equal(42, svc.ActiveLessonId);
        }

        [Fact]
        public void SetAndGet_LastTeacherCommand_ThreadSafe()
        {
            var svc = new LessonStateService();
            svc.LastTeacherCommand = "START_LESSON";
            Assert.Equal("START_LESSON", svc.LastTeacherCommand);
        }

        [Fact]
        public void Reset_Clears_All()
        {
            var svc = new LessonStateService();
            svc.ActiveLessonId = 99;
            svc.ActiveLessonStage = 3;
            svc.IsLessonActive = true;
            svc.LastTeacherCommand = "CMD";

            svc.Reset();

            Assert.Equal(0, svc.ActiveLessonId);
            Assert.Equal(0, svc.ActiveLessonStage);
            Assert.False(svc.IsLessonActive);
            Assert.Equal(string.Empty, svc.LastTeacherCommand);
        }

        [Fact]
        public void StateChanged_Event_Fires()
        {
            var svc = new LessonStateService();
            bool fired = false;
            svc.StateChanged += (s, e) => fired = true;
            svc.NotifyChanged();
            Assert.True(fired);
        }
    }

    public class BroadcastStateServiceTests
    {
        [Fact]
        public void Instance_Should_Be_Singleton()
        {
            Assert.Same(BroadcastStateService.Instance, BroadcastStateService.Instance);
        }

        [Fact]
        public void Default_IsActive_Is_False()
        {
            var svc = new BroadcastStateService();
            Assert.False(svc.IsScreenBroadcastActive);
            Assert.False(svc.IsBroadcastCasting);
        }

        [Fact]
        public void SetAndGet_ScreenCapturePath()
        {
            var svc = new BroadcastStateService();
            svc.ScreenCapturePath = @"C:\temp\capture.png";
            Assert.Equal(@"C:\temp\capture.png", svc.ScreenCapturePath);
        }

        [Fact]
        public void Reset_Clears_All()
        {
            var svc = new BroadcastStateService();
            svc.ScreenCapturePath = "test.png";
            svc.IsScreenBroadcastActive = true;
            svc.IsBroadcastCasting = true;

            svc.Reset();

            Assert.Equal(string.Empty, svc.ScreenCapturePath);
            Assert.False(svc.IsScreenBroadcastActive);
            Assert.False(svc.IsBroadcastCasting);
        }
    }

    public class ClassControlServiceTests
    {
        [Fact]
        public void Instance_Should_Be_Singleton()
        {
            Assert.Same(ClassControlService.Instance, ClassControlService.Instance);
        }

        [Fact]
        public void Default_Lock_Is_Off()
        {
            var svc = new ClassControlService();
            Assert.False(svc.IsScreenLocked);
            Assert.False(svc.IsSilenceActive);
        }

        [Fact]
        public void SetAndGet_ScreenLockType()
        {
            var svc = new ClassControlService();
            svc.ScreenLockType = "BLACK";
            Assert.Equal("BLACK", svc.ScreenLockType);
        }

        [Fact]
        public void SetAndGet_ToolFocusId()
        {
            var svc = new ClassControlService();
            svc.ActiveToolFocusId = "calculator_001";
            Assert.Equal("calculator_001", svc.ActiveToolFocusId);
        }

        [Fact]
        public void Reset_Clears_All()
        {
            var svc = new ClassControlService();
            svc.IsScreenLocked = true;
            svc.ScreenLockType = "LOCK";
            svc.IsSilenceActive = true;
            svc.ActiveToolFocusId = "tool1";

            svc.Reset();

            Assert.False(svc.IsScreenLocked);
            Assert.Equal(string.Empty, svc.ScreenLockType);
            Assert.False(svc.IsSilenceActive);
            Assert.Equal(string.Empty, svc.ActiveToolFocusId);
        }
    }

    public class AssessmentStateServiceTests
    {
        [Fact]
        public void Instance_Should_Be_Singleton()
        {
            Assert.Same(AssessmentStateService.Instance, AssessmentStateService.Instance);
        }

        [Fact]
        public void SetAndGet_SurveyQuestion()
        {
            var svc = new AssessmentStateService();
            svc.ActiveSurveyQuestion = "Bạn có hiểu bài không?";
            Assert.Equal("Bạn có hiểu bài không?", svc.ActiveSurveyQuestion);
        }

        [Fact]
        public void SetAndGet_PollOptions()
        {
            var svc = new AssessmentStateService();
            var options = new[] { "A", "B", "C", "D" };
            svc.ActivePollOptions = options;
            Assert.Equal(options, svc.ActivePollOptions);
        }

        [Fact]
        public void SetAndGet_Assignment()
        {
            var svc = new AssessmentStateService();
            svc.AssignmentDescription = "Làm bài tập trang 42";
            svc.AssignmentDeadline = new DateTime(2026, 5, 20);
            Assert.Equal("Làm bài tập trang 42", svc.AssignmentDescription);
            Assert.Equal(new DateTime(2026, 5, 20), svc.AssignmentDeadline);
        }

        [Fact]
        public void Reset_Clears_All()
        {
            var svc = new AssessmentStateService();
            svc.ActiveSurveyQuestion = "test?";
            svc.ActivePollId = "poll_123";
            svc.AssignmentDescription = "homework";
            svc.SurveyAnswered = true;

            svc.Reset();

            Assert.Equal(string.Empty, svc.ActiveSurveyQuestion);
            Assert.Equal(string.Empty, svc.ActivePollId);
            Assert.Equal(string.Empty, svc.AssignmentDescription);
            Assert.False(svc.SurveyAnswered);
        }
    }

    public class FocusStateServiceTests
    {
        [Fact]
        public void Instance_Should_Be_Singleton()
        {
            Assert.Same(FocusStateService.Instance, FocusStateService.Instance);
        }

        [Fact]
        public void SetAndGet_FocusSort()
        {
            var svc = new FocusStateService();
            svc.ActiveFocusSort = 5;
            Assert.Equal(5, svc.ActiveFocusSort);
        }

        [Fact]
        public void SetAndGet_FocusType()
        {
            var svc = new FocusStateService();
            svc.ActiveFocusType = "ImageBlock";
            Assert.Equal("ImageBlock", svc.ActiveFocusType);
        }

        [Fact]
        public void Reset_Sets_Defaults()
        {
            var svc = new FocusStateService();
            svc.ActiveFocusSort = 10;
            svc.ActiveFocusType = "Video";

            svc.Reset();

            Assert.Equal(-1, svc.ActiveFocusSort);
            Assert.Equal(string.Empty, svc.ActiveFocusType);
        }
    }
}
