using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace QASmartClass.TeacherHub.Views
{
    public partial class TeacherHubWindow : Window
    {
        private readonly Dictionary<string, object> _viewCache = new();

        public TeacherHubWindow()
        {
            InitializeComponent();
            FeatureFrame.Visibility = Visibility.Collapsed;
        }

        private void ShowView<T>() where T : new()
        {
            FeatureFrame.Visibility = Visibility.Visible;
            var typeName = typeof(T).Name;
            if (!_viewCache.TryGetValue(typeName, out var view))
            {
                view = new T();
                _viewCache[typeName] = view;
            }
            if (view is Page p) FeatureFrame.Navigate(p);
            else FeatureFrame.Content = view;
        }

        private void ShowView(object view)
        {
            FeatureFrame.Visibility = Visibility.Visible;
            var typeName = view.GetType().Name;
            if (!_viewCache.ContainsKey(typeName))
            {
                _viewCache[typeName] = view;
            }
            if (_viewCache[typeName] is Page p) FeatureFrame.Navigate(p);
            else FeatureFrame.Content = _viewCache[typeName];
        }

        private void BtnClearFrame_Click(object sender, RoutedEventArgs e)
        {
            FeatureFrame.Visibility = Visibility.Collapsed;
            FeatureFrame.Content = null;
        }

        private void BtnNavLessonPlan_Click(object sender, RoutedEventArgs e)
        {
            ShowView<LessonPlanPage>();
        }

        private void BtnNavTasks_Click(object sender, RoutedEventArgs e)
        {
            ShowView<MyTasksView>();
        }

        private void BtnNavHomeroom_Click(object sender, RoutedEventArgs e)
        {
            ShowView<HomeroomDiaryPage>();
        }

        private void BtnNavEmulation_Click(object sender, RoutedEventArgs e)
        {
            ShowView(new QASmartClass.Leadership.Views.EmulationBoardPage(new Data.AppDbContext()));
        }

        private void BtnNavBulletin_Click(object sender, RoutedEventArgs e)
        {
            ShowView<BulletinBoardPage>();
        }

        private void BtnNavAi_Click(object sender, RoutedEventArgs e)
        {
            ShowView<AiAssistantPage>();
        }

        // P1-04: Ngân hàng câu hỏi
        private void BtnNavQuestionBank_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QuestionBankView>();
        }

        // P2-04: Sinh hoat To CM
        private void BtnNavDeptMeeting_Click(object sender, RoutedEventArgs e)
        {
            ShowView<DeptMeetingView>();
        }

        // V7 M1.2: Kiểm định đề thi
        private void BtnNavDeptHeadReview_Click(object sender, RoutedEventArgs e)
        {
            ShowView<DeptHeadReviewView>();
        }

        // P3-05: Youth Union
        private void BtnNavYouthMembers_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.MemberListView>();
        }

        private void BtnNavYouthActivities_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.ActivityManagementView>();
        }

        // P3-05: Youth Union Dashboard
        private void BtnNavYouthDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.YouthUnionDashboard>();
        }

        // Phase 2: Youth Union sub-views
        private void BtnNavYouthAttendance_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.AttendanceView>();
        }
        private void BtnNavYouthEmulation_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.EmulationScoreView>();
        }
        private void BtnNavYouthFee_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.FeeTrackerView>();
        }
        private void BtnNavYouthEventReg_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.EventRegistrationView>();
        }

        // Phase 3: Youth Union sub-views
        private void BtnNavYouthRecruitment_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.RecruitmentView>();
        }
        private void BtnNavYouthAward_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.AwardProposalView>();
        }
        private void BtnNavYouthVoting_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.VotingView>();
        }
        private void BtnNavYouthDocument_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.DocumentView>();
        }
        private void BtnNavYouthPlanBudget_Click(object sender, RoutedEventArgs e)
        {
            ShowView<QASmartClass.YouthUnion.Views.PlanBudgetView>();
        }

        // P3-09: SKKN
        private void BtnNavSkkn_Click(object sender, RoutedEventArgs e)
        {
            ShowView<SkknView>();
        }

        // P3-12: Remedial Plan
        private void BtnNavRemedial_Click(object sender, RoutedEventArgs e)
        {
            ShowView<RemedialPlanView>();
        }

        // F3.2: Ký duyệt điện tử Phụ huynh
        private void BtnNavParentApproval_Click(object sender, RoutedEventArgs e)
        {
            ShowView<ParentApprovalView>();
        }
    
        private void BtnNavClassMIDashboard_Click(object sender, RoutedEventArgs e)
        {
            ShowView<ClassMIDashboardView>();
        }
    }
}

