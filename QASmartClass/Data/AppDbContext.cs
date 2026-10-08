using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace QASmartClass.Data
{
    /// <summary>
    /// EF Core Database Context cho QA Smart Class
    /// SQLite database l�f�?��,°u tr�f¡�,»�,¯ b�f�'�, i gi�f¡�,º�,£ng, l�f¡�,»â�,�ºp h�f¡�,»�,c, quiz, k�f¡�,º�,¿t qu�f¡�,º�,£
    /// </summary>
    public class AppDbContext : DbContext
    {
        public static readonly System.Threading.SemaphoreSlim BackgroundDbSemaphore = new System.Threading.SemaphoreSlim(1, 1);
        public static Microsoft.Data.Sqlite.SqliteConnection? FallbackInMemoryConnection { get; set; }
        public static bool IsSeedingOrMigrating { get; set; } = false;

        public class SqliteWalInterceptor : Microsoft.EntityFrameworkCore.Diagnostics.DbConnectionInterceptor
        {
            public override void ConnectionOpened(System.Data.Common.DbConnection connection, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData)
            {
                // First, establish busy timeout immediately so any queries we make can wait if needed!
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA busy_timeout=5000;";
                        cmd.ExecuteNonQuery();
                    }
                }
                catch { }

                string journalMode = "WAL";
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Value FROM SystemSettings WHERE Id = 'IT_Database_WALMode';";
                        var val = command.ExecuteScalar();
                        if (val != null && val.ToString() == "Disabled")
                        {
                            journalMode = "DELETE";
                        }
                    }
                }
                catch
                {
                    journalMode = "WAL"; // Fallback on missing tables during migration
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"PRAGMA journal_mode={journalMode}; PRAGMA synchronous = NORMAL;";
                        command.ExecuteNonQuery();
                    }
                }
                catch { }

                                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "ALTER TABLE RemedialPlans ADD COLUMN Status TEXT DEFAULT 'Approved';";
                        command.ExecuteNonQuery();
                    }
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "ALTER TABLE Skkns ADD COLUMN TeacherCode TEXT DEFAULT '';";
                        command.ExecuteNonQuery();
                    }
                }
                catch { }
                }
                catch { }

                base.ConnectionOpened(connection, eventData);
            }

            public override async System.Threading.Tasks.Task ConnectionOpenedAsync(System.Data.Common.DbConnection connection, Microsoft.EntityFrameworkCore.Diagnostics.ConnectionEndEventData eventData, System.Threading.CancellationToken cancellationToken = default)
            {
                // First, establish busy timeout immediately so any queries we make can wait if needed!
                try
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "PRAGMA busy_timeout=5000;";
                        await cmd.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch { }

                string journalMode = "WAL";
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT Value FROM SystemSettings WHERE Id = 'IT_Database_WALMode';";
                        var val = await command.ExecuteScalarAsync(cancellationToken);
                        if (val != null && val.ToString() == "Disabled")
                        {
                            journalMode = "DELETE";
                        }
                    }
                }
                catch
                {
                    journalMode = "WAL"; // Fallback on missing tables during migration
                }

                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"PRAGMA journal_mode={journalMode}; PRAGMA synchronous = NORMAL;";
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch { }

                                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "ALTER TABLE RemedialPlans ADD COLUMN Status TEXT DEFAULT 'Approved';";
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                try
                {
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = "ALTER TABLE Skkns ADD COLUMN TeacherCode TEXT DEFAULT '';";
                        await command.ExecuteNonQueryAsync(cancellationToken);
                    }
                }
                catch { }
                }
                catch { }

                await base.ConnectionOpenedAsync(connection, eventData, cancellationToken);
            }
        }

        public DbSet<Lesson> Lessons { get; set; } = null!;
        public DbSet<LessonContent> LessonContents { get; set; } = null!;
        public DbSet<LessonHistory> LessonHistories { get; set; } = null!;
        public DbSet<Classroom> Classrooms { get; set; } = null!;
        public DbSet<Student> Students { get; set; } = null!;
        public DbSet<StudentConductHistory> StudentConductHistories { get; set; } = null!;
        public DbSet<Quiz> Quizzes { get; set; } = null!;
        public DbSet<Question> Questions { get; set; } = null!;
        public DbSet<QuizResult> QuizResults { get; set; } = null!;
        public DbSet<FileTransferRecord> FileTransfers { get; set; } = null!;
        public DbSet<EventLog> EventLogs { get; set; } = null!;
        public DbSet<Homework> Homeworks { get; set; } = null!;
        public DbSet<QuestionBankCategory> QuestionBankCategories { get; set; } = null!;
        public DbSet<QuestionBankItem> QuestionBankItems { get; set; } = null!;
        public DbSet<TeacherProfile> TeacherProfiles { get; set; } = null!;
        public DbSet<RememberedDevice> RememberedDevices { get; set; } = null!;
        public DbSet<ClassRoster> ClassRosters { get; set; } = null!;
        public DbSet<ClassRosterStudent> ClassRosterStudents { get; set; } = null!;
        public DbSet<GradeTypeMaster> GradeTypeMasters { get; set; } = null!;
        public DbSet<StudentGrade> StudentGrades { get; set; } = null!;
        public DbSet<MathQuizHistory> MathQuizHistories { get; set; } = null!;
        public DbSet<LiteratureQuizHistory> LiteratureQuizHistories { get; set; } = null!;
        public DbSet<AttendanceRecord> AttendanceRecords { get; set; } = null!;
        public DbSet<UsageLog> UsageLogs { get; set; } = null!;
        public DbSet<Survey> Surveys { get; set; } = null!;
        public DbSet<SurveyResponse> SurveyResponses { get; set; } = null!;
        public DbSet<SurveyTemplate> SurveyTemplates { get; set; } = null!;
        public DbSet<GrammarTense> GrammarTenses { get; set; } = null!;
        public DbSet<GrammarQuestion> GrammarQuestions { get; set; } = null!;
        public DbSet<GrammarQuizHistory> GrammarQuizHistories { get; set; } = null!;
        public DbSet<OfflineSyncItem> OfflineSyncItems { get; set; } = null!;


        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 4: Education Features �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<LearningAnalytics> LearningAnalytics { get; set; } = null!;
        public DbSet<StudentAchievement> StudentAchievements { get; set; } = null!;
        public DbSet<Rubric> Rubrics { get; set; } = null!;
        public DbSet<RubricCriteria> RubricCriteria { get; set; } = null!;
        public DbSet<RubricGrade> RubricGrades { get; set; } = null!;
        public DbSet<Assignment> Assignments { get; set; } = null!;
        public DbSet<ConductRecord> ConductRecords { get; set; } = null!;
        public DbSet<InboxMessage> InboxMessages { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 5: Daily School Dashboard �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<SchoolEvent> SchoolEvents { get; set; } = null!;
        public DbSet<DailyTask> DailyTasks { get; set; } = null!;
        public DbSet<TimetableEntry> TimetableEntries { get; set; } = null!;
        public DbSet<Subject> Subjects { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 6: Task Management Hub �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<TaskItem> TaskItems { get; set; } = null!;
        public DbSet<TaskComment> TaskComments { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 7: B�f�'�,¡o c�f�'�,¡o & KPI �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<EvaluationRecord> EvaluationRecords { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 8: H�f¡�,»�,c sinh n�f�'�,¢ng cao �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<PortfolioItem> PortfolioItems { get; set; } = null!;
        public DbSet<LearningDiary> LearningDiaries { get; set; } = null!;
        public DbSet<SelfEvaluation> SelfEvaluations { get; set; } = null!;

        public DbSet<LessonPlan> LessonPlans { get; set; } = null!;
        public DbSet<LessonPlanDraft> LessonPlanDrafts { get; set; } = null!;
        public DbSet<LessonPlanVersion> LessonPlanVersions { get; set; } = null!;
        public DbSet<HomeroomDiary> HomeroomDiaries { get; set; } = null!;
        public DbSet<PeriodLogbook> PeriodLogbooks { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 9: Gi�f�'�,¡o vi�f�'�,ªn n�f�'�,¢ng cao �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬


        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ Sprint 10: Thi �f�?zâ�,��oua & Gamification �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<Badge> Badges { get; set; } = null!;
        public DbSet<UserBadge> UserBadges { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P1-01: Game Hub �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<GameSession> GameSessions { get; set; } = null!;
        public DbSet<StudentMIProfile> StudentMIProfiles { get; set; } = null!;
        public DbSet<MiniGameRecord> MiniGameRecords { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-04: Dept Meeting �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<DeptMeeting> DeptMeetings { get; set; } = null!;
        public DbSet<DeptMeetingAction> DeptMeetingActions { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-09: Student Goal �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<StudentGoal> StudentGoals { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-05: Class Observation �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<ClassObservation> ClassObservations { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-08: Discipline Record �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<DisciplineRecord> DisciplineRecords { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-01: Counseling �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<CounselingProfile> CounselingProfiles { get; set; } = null!;
        public DbSet<CounselingSession> CounselingSessions { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-03: Health Record �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<HealthRecord> HealthRecords { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-04: Epidemic Monitor �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<EpidemicCase> EpidemicCases { get; set; } = null!;
        public DbSet<FoodSafetyRecord> FoodSafetyRecords { get; set; } = null!;
        public DbSet<BookLoan> BookLoans { get; set; } = null!;
        public DbSet<PatrolLog> PatrolLogs { get; set; } = null!;
        public DbSet<VaccinationRecord> VaccinationRecords { get; set; } = null!;
        public DbSet<GatePickupLog> GatePickupLogs { get; set; } = null!;
        public DbSet<MealFeedback> MealFeedbacks { get; set; } = null!;
        public DbSet<TuitionReconciliationLog> TuitionReconciliationLogs { get; set; } = null!;
        public DbSet<AssetAuditLog> AssetAuditLogs { get; set; } = null!;
        public DbSet<FoodWasteLog> FoodWasteLogs { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-05: Youth Union �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<YouthMember> YouthMembers { get; set; } = null!;
        public DbSet<YouthActivity> YouthActivities { get; set; } = null!;
        public DbSet<YouthAttendance> YouthAttendances { get; set; } = null!;
        public DbSet<YouthRecruitment> YouthRecruitments { get; set; } = null!;
        public DbSet<YouthFee> YouthFees { get; set; } = null!;
        public DbSet<YouthPlan> YouthPlans { get; set; } = null!;
        public DbSet<YouthEventRegistration> YouthEventRegistrations { get; set; } = null!;
        public DbSet<YouthEmulationScore> YouthEmulationScores { get; set; } = null!;
        public DbSet<YouthAwardProposal> YouthAwardProposals { get; set; } = null!;
        public DbSet<YouthAwardRecord> YouthAwardRecords { get; set; } = null!;
        public DbSet<YouthVoting> YouthVotings { get; set; } = null!;
        public DbSet<YouthVote> YouthVotes { get; set; } = null!;
        public DbSet<YouthVoterRegistry> YouthVoterRegistries { get; set; } = null!;
        public DbSet<YouthDocument> YouthDocuments { get; set; } = null!;
        public DbSet<YouthBudget> YouthBudgets { get; set; } = null!;
        public DbSet<YouthBchMessage> YouthBchMessages { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-08 to P3-12: M�f¡�,»�.¸ r�f¡�,»â�?z¢ng �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<AnonymousQuery> AnonymousQueries { get; set; } = null!;
        public DbSet<Skkn> Skkns { get; set; } = null!;
        public DbSet<FamilyGameSession> FamilyGameSessions { get; set; } = null!;
        public DbSet<RemedialPlan> RemedialPlans { get; set; } = null!;
        public DbSet<RemedialSession> RemedialSessions { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-02: Career Test �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<CareerTestResult> CareerTestResults { get; set; } = null!;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P2-07: Club Management �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        public DbSet<Club> Clubs { get; set; } = null!;
        public DbSet<ClubMember> ClubMembers { get; set; } = null!;
        public DbSet<ClubActivity> ClubActivities { get; set; } = null!;

        // �f¢â�,�� â�?s¬�f¢â�,�� â�?s¬�f¢â�,�� â�?s¬ P3-01: HR Management �f¢â�,�� â�?s¬�f¢â�,�� â�?s¬�f¢â�,�� â�?s¬
        public DbSet<StaffProfile> StaffProfiles { get; set; } = null!;
        public DbSet<Contract> Contracts { get; set; } = null!;
        public DbSet<LeaveRequest> LeaveRequests { get; set; } = null!;
        public DbSet<StaffAttendance> StaffAttendances { get; set; } = null!;

        // f¢â, â?s¬f¢â, â?s¬f¢â, â?s¬ P3-02: Asset Management f¢â, â?s¬f¢â, â?s¬f¢â, â?s¬
        public DbSet<Asset> Assets { get; set; } = null!;
        public DbSet<AssetTransfer> AssetTransfers { get; set; } = null!;

        // --- P4-WI04: Bằng cấp / Chứng chỉ GV ---
        public DbSet<TeacherQualification> TeacherQualifications { get; set; } = null!;
        public DbSet<TrainingHistory> TrainingHistories { get; set; } = null!;

        // --- P4-WI05: T? Chuyn Mn ---
        public DbSet<Department> Departments { get; set; } = null!;
        public DbSet<DepartmentMember> DepartmentMembers { get; set; } = null!;
        public DbSet<AwardRecord> AwardRecords { get; set; } = null!;

        // --- P4-WI10: S? Li�n L?c Di?n T? ---
        public DbSet<ContactBookEntry> ContactBookEntries { get; set; } = null!;
        // --- P4-WI12: Lịch dự giờ Tổ CM ---
        public DbSet<ObservationSchedule> ObservationSchedules { get; set; } = null!;
        // --- P4-WI13: Qu?n l� chuy�n d? ---
        public DbSet<ProfessionalTopic> ProfessionalTopics { get; set; } = null!;

    

        // �f¢â�,�� â�?s¬�f¢â�,�� â�?s¬�f¢â�,�� â�?s¬ P3-03: Tuition Fee �f¢â�,�� â�?s¬�f¢â�,�� â�?s¬�f¢â�,�� â�?s¬
        public DbSet<TuitionRecord> TuitionRecords { get; set; } = null!;

        // f¢â, â?s¬f¢â, â?s¬f¢â, â?s¬ Sprint 11: Bf¡,º,£ng tin & AI f¢â, â?s¬f¢â, â?s¬f¢â, â?s¬
        public DbSet<Bulletin> Bulletins { get; set; } = null!;
        public DbSet<BulletinTemplate> BulletinTemplates { get; set; } = null!;
        public DbSet<BulletinComment> BulletinComments { get; set; } = null!;
        public DbSet<BulletinReadReceipt> BulletinReadReceipts { get; set; } = null!;
        public DbSet<BulletinPollOption> BulletinPollOptions { get; set; } = null!;
        public DbSet<BulletinPollVote> BulletinPollVotes { get; set; } = null!;
        public DbSet<BulletinApprovalRequest> BulletinApprovalRequests { get; set; } = null!;

        // f¢â, â?s¬f¢â, â?s¬f¢â, â?s¬ P4: PHA 4 MODELS f¢â, â?s¬f¢â, â?s¬f¢â, â?s¬
        public DbSet<OfficialDocument> OfficialDocuments { get; set; } = null!;

        /// <summary>Production constructor f¢â?s¬â,  uses SQLite file</summary>
        public AppDbContext()
        {
            this.ChangeTracker.Tracked += OnEntityTracked;
        }

        /// <summary>Test constructor f¢â?s¬â,  accepts in-memory or custom options</summary>
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
            this.ChangeTracker.Tracked += OnEntityTracked;
        }

        private void OnEntityTracked(object? sender, Microsoft.EntityFrameworkCore.ChangeTracking.EntityTrackedEventArgs e)
        {
            if (e.FromQuery && e.Entry.Entity is Student student)
            {
                if (string.IsNullOrEmpty(student.WalletChecksum) && student.WalletBalance == 0.0m)
                {
                    return;
                }

                var expectedChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                    student.StudentCode + student.WalletBalance.ToString("F2")
                );

                if (student.WalletChecksum != expectedChecksum)
                {
                    Serilog.Log.Warning("Fraud warning: Checksum mismatch for student {StudentCode}. Expected: {Expected}, Actual: {Actual}", 
                        student.StudentCode, expectedChecksum, student.WalletChecksum);
                    
                    student.WalletBalance = 0.0m;
                    student.IsAtRisk = true;
                    student.RiskReason = "Cảnh báo ví: Phát hiện sửa đổi số dư trái phép!";
                }
            }
            else if (e.FromQuery && e.Entry.Entity is TuitionRecord tuition)
            {
                if (string.IsNullOrEmpty(tuition.TuitionChecksum) && tuition.PaidAmount == 0.0)
                {
                    return;
                }

                var expectedChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                    tuition.StudentId + "_" + tuition.Amount.ToString("F2") + "_" + tuition.PaidAmount.ToString("F2") + "_" + tuition.Status
                );

                if (tuition.TuitionChecksum != expectedChecksum)
                {
                    Serilog.Log.Warning("Tuition Fraud Warning: Checksum mismatch for tuition ID {TuitionId}. Expected: {Expected}, Actual: {Actual}", 
                        tuition.Id, expectedChecksum, tuition.TuitionChecksum);
                    
                    tuition.PaidAmount = 0.0;
                    tuition.Status = "FraudDetected";
                }
            }
        }

        public DbSet<PayrollRecord> PayrollRecords { get; set; } = null!;
        public DbSet<SecurityLog> SecurityLogs { get; set; } = null!;
        public DbSet<CleaningTask> CleaningTasks { get; set; } = null!;
        public DbSet<SchoolMenu> SchoolMenus { get; set; } = null!;
        public DbSet<FoodAllergy> FoodAllergies { get; set; } = null!;
        public DbSet<MedicalSupply> MedicalSupplies { get; set; } = null!;
        public DbSet<EmergencyLog> EmergencyLogs { get; set; } = null!;


        // â? â?  V7 M2: Tf¢m lf½ há» c "?~?°á» ng (SEL) â? â? 
        public DbSet<StudentMentalHealthRecord> StudentMentalHealthRecords { get; set; } = null!;

        // V7 P3.4: Phan hoi hoc sinh ve giao vien
        public DbSet<TeacherFeedback> TeacherFeedbacks { get; set; } = null!;

        public DbSet<SocialPost> SocialPosts { get; set; } = null!;
        public DbSet<SocialComment> SocialComments { get; set; } = null!;
        public DbSet<MobileToken> MobileTokens { get; set; } = null!;
        public DbSet<PushMessageLog> PushMessageLogs { get; set; } = null!;
        public DbSet<TaskCategory> TaskCategories { get; set; } = null!;
        public DbSet<DocumentRoute> DocumentRoutes { get; set; } = null!;
        public DbSet<StudentLeaveRequest> StudentLeaveRequests { get; set; } = null!;
        
        // Phase 7
        public DbSet<SystemSetting> SystemSettings { get; set; } = null!;
        public DbSet<BackupLog> BackupLogs { get; set; } = null!;
        public DbSet<AuditLog> AuditLogs { get; set; } = null!;

        // �f¢â�,�¢�,�f¢â�,�¢�, V6 AI & Smart Pedagogy �f¢â�,�¢�,�f¢â�,�¢�,
        public DbSet<SoftSkillRecord> SoftSkillRecords { get; set; } = null!;
        public DbSet<SchoolAsset> SchoolAssets { get; set; } = null!;
        public DbSet<ParentApproval> ParentApprovals { get; set; } = null!;
        public DbSet<SchoolAssetBooking> AssetBookings { get; set; } = null!;
        public DbSet<RolePermission> RolePermissions { get; set; } = null!;
        public DbSet<StudentMindmap> StudentMindmaps { get; set; } = null!;
        public DbSet<LibraryBook> LibraryBooks { get; set; } = null!;
        public DbSet<BookReservation> BookReservations { get; set; } = null!;

        // Phase 4 Workflows
        public DbSet<StudentPhysicalRestriction> StudentPhysicalRestrictions { get; set; } = null!;
        public DbSet<MedicalDisposalProposal> MedicalDisposalProposals { get; set; } = null!;
        public DbSet<TrafficCongestionLog> TrafficCongestionLogs { get; set; } = null!;
        public DbSet<FoodSafetyInspectionLog> FoodSafetyInspectionLogs { get; set; } = null!;
        public DbSet<IntrusionAlarm> IntrusionAlarms { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<StudentDeviceLicense> StudentDeviceLicenses { get; set; } = null!;
        public DbSet<StudentDeviceStatus> StudentDeviceStatuses { get; set; } = null!;
        public DbSet<SystemDiagnosticLog> SystemDiagnosticLogs { get; set; } = null!;
        public DbSet<PrincipalOverrideLog> PrincipalOverrideLogs { get; set; } = null!;
        public DbSet<SensitiveDataAccessAudit> SensitiveDataAccessAudits { get; set; } = null!;
        public DbSet<StudyLoadAdjustment> StudyLoadAdjustments { get; set; } = null!;
        public DbSet<CampusBeacon> CampusBeacons { get; set; } = null!;
        public DbSet<StudentLocationHistory> StudentLocationHistories { get; set; } = null!;
        public DbSet<GateOfflineKey> GateOfflineKeys { get; set; } = null!;
        public DbSet<GateBarrierLog> GateBarrierLogs { get; set; } = null!;
        public DbSet<SubstituteAssignment> SubstituteAssignments { get; set; } = null!;
        public DbSet<ChemicalRequisition> ChemicalRequisitions { get; set; } = null!;
        public DbSet<StaffPayrollLedger> StaffPayrollLedgers { get; set; } = null!;
        public DbSet<MaintenanceTicket> MaintenanceTickets { get; set; } = null!;
        public DbSet<SecurityLockdownLog> SecurityLockdownLogs { get; set; } = null!;
        public DbSet<KitchenColdStorageLog> KitchenColdStorageLogs { get; set; } = null!;
        public DbSet<StudentCarePlan> StudentCarePlans { get; set; } = null!;
        public DbSet<ResourceWasteLedger> ResourceWasteLedgers { get; set; } = null!;




        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            if (options.IsConfigured) return; // Skip if already configured (test mode)

            if (FallbackInMemoryConnection != null)
            {
                options.UseSqlite(FallbackInMemoryConnection)
                       .AddInterceptors(new SqliteWalInterceptor());
                return;
            }

            var dbPath = QASmartClass.Services.AppPaths.DatabaseFile;

            System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath)!);

            var keyBytes = DbEncryptionKeyManager.GetOrInitializeKey();
            var hexKey = Convert.ToHexString(keyBytes);

            options.UseSqlite($"Data Source={dbPath};Password={hexKey};Foreign Keys=True;Default Timeout=5;Pooling=False")
                   .AddInterceptors(new SqliteWalInterceptor());
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Cấu hình Value Converter mã hóa cho StudentMentalHealthRecord
            var encryptionKey = "QA_MentalHealth_Secure_Key_2026";
            var encryptConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<string, string>(
                v => QASmartClass.Utilities.CryptoHelper.Encrypt(v, encryptionKey),
                v => QASmartClass.Utilities.CryptoHelper.Decrypt(v, encryptionKey)
            );

            modelBuilder.Entity<StudentMentalHealthRecord>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Notes).HasConversion(encryptConverter);
                e.Property(x => x.DetectedKeywords).HasConversion(encryptConverter);
            });

            modelBuilder.Entity<LibraryBook>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.BookCode).IsUnique();
            });

            modelBuilder.Entity<BookReservation>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.BookCode);
                e.HasIndex(x => x.StudentId);
            });

            modelBuilder.Entity<StudentPhysicalRestriction>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentId);
            });

            modelBuilder.Entity<MedicalDisposalProposal>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.MedicalSupplyId);
            });

            modelBuilder.Entity<TrafficCongestionLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Timestamp);
            });

            modelBuilder.Entity<FoodSafetyInspectionLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.SupplierId);
            });

            modelBuilder.Entity<IntrusionAlarm>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.DetectionTime);
            });

            modelBuilder.Entity<Supplier>(e =>
            {
                e.HasKey(x => x.Id);
            });

            modelBuilder.Entity<StudentDeviceLicense>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentId);
                e.HasIndex(x => x.DeviceCode);
            });

            modelBuilder.Entity<StudentDeviceStatus>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentId);
                e.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<SystemDiagnosticLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentId);
                e.HasIndex(x => x.Timestamp);
                e.HasIndex(x => x.IsResolved);
            });

            modelBuilder.Entity<PrincipalOverrideLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.PrincipalId);
                e.HasIndex(x => x.Timestamp);
            });

            modelBuilder.Entity<SensitiveDataAccessAudit>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.AccessorId);
                e.HasIndex(x => x.Timestamp);
                e.HasIndex(x => x.IsAnomalous);
            });

            modelBuilder.Entity<StudyLoadAdjustment>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentId);
                e.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<CampusBeacon>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsActive);
            });

            modelBuilder.Entity<StudentLocationHistory>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentCode);
                e.HasIndex(x => x.Timestamp);
                e.HasIndex(x => x.NearbyBeaconId);
            });

            modelBuilder.Entity<GateOfflineKey>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.IsActive);
            });

            modelBuilder.Entity<GateBarrierLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentCode);
                e.HasIndex(x => x.Timestamp);
            });

            modelBuilder.Entity<SubstituteAssignment>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.SubstituteTeacherCode);
                e.HasIndex(x => x.Date);
            });

            modelBuilder.Entity<ChemicalRequisition>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.AssetBookingId);
                e.HasIndex(x => x.ChemicalName);
            });

            modelBuilder.Entity<StaffPayrollLedger>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StaffCode);
                e.HasIndex(x => x.MonthYear);
            });

            modelBuilder.Entity<MaintenanceTicket>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Status);
                e.HasIndex(x => x.Severity);
            });

            modelBuilder.Entity<SecurityLockdownLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.TriggeredByStaffCode);
                e.HasIndex(x => x.Status);
            });

            modelBuilder.Entity<KitchenColdStorageLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.FridgeId);
                e.HasIndex(x => x.Timestamp);
            });

            modelBuilder.Entity<StudentCarePlan>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentCode);
                e.HasIndex(x => x.IsActive);
            });

            modelBuilder.Entity<ResourceWasteLedger>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Date);
                e.HasIndex(x => x.JanitorStaffCode);
            });

            // Lesson
            modelBuilder.Entity<Lesson>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(200).IsRequired();
                e.HasMany(x => x.Contents).WithOne().HasForeignKey(x => x.LessonId);
            });

            // Classroom
            modelBuilder.Entity<Classroom>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            });

            // Quiz
            modelBuilder.Entity<Quiz>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasMany(x => x.Questions).WithOne().HasForeignKey(x => x.QuizId);
            });

            // Question
            modelBuilder.Entity<Question>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Content).IsRequired();
            });

            // Survey
            modelBuilder.Entity<Survey>(e =>
            {
                e.HasKey(x => x.Id);
            });

            // SurveyResponse
            modelBuilder.Entity<SurveyResponse>(e =>
            {
                e.HasKey(x => x.Id);
            });

            // SurveyTemplate
            modelBuilder.Entity<SurveyTemplate>(e =>
            {
                e.HasKey(x => x.Id);
            });

            // QuestionBank
            modelBuilder.Entity<QuestionBankCategory>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Name).HasMaxLength(200).IsRequired();
                e.HasMany(x => x.Items).WithOne().HasForeignKey(x => x.CategoryId);
            });

            modelBuilder.Entity<QuestionBankItem>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Content).IsRequired();
                e.HasIndex(x => new { x.Subject, x.Difficulty, x.ApprovalStatus, x.CategoryId });
            });

            // ClassRoster f¢â?s¬â,  danh sf',¡ch lf¡,»â,ºp hf¡,», c (phf',²ng STEM df',¹ng chung)
            modelBuilder.Entity<ClassRoster>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.ClassName).HasMaxLength(50).IsRequired();
                e.HasMany(x => x.Students).WithOne().HasForeignKey(x => x.RosterId);
            });

            modelBuilder.Entity<ClassRosterStudent>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.RosterId, x.StudentId }).IsUnique();
            });

            // TeacherProfile �f¢â�?s¬â�,� unique TeacherCode
            modelBuilder.Entity<TeacherProfile>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.TeacherCode).IsUnique();
            });

            // Student �f¢â�?s¬â�,� unique StudentCode
            modelBuilder.Entity<Student>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentCode).IsUnique();
                e.Property(x => x.LowBalanceThreshold).HasDefaultValue(30000m);
            });

            // GradeTypeMaster �f¢â�?s¬â�,� danh m�f¡�,»�,¥c lo�f¡�,º�,¡i �f�?zâ�,��oi�f¡�,»�?�?Tm (c�f¡�,º�,¥u h�f�'�,¬nh t�f¡�,»�,« Settings)
            modelBuilder.Entity<GradeTypeMaster>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Code).IsUnique();
                e.Property(x => x.Code).HasMaxLength(50).IsRequired();
                e.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
            });

            // StudentGrade �f¢â�?s¬â�,� �f�?zâ�,��oi�f¡�,»�?�?Tm s�f¡�,»â�,��o h�f¡�,»�,c sinh
            modelBuilder.Entity<StudentGrade>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.StudentId, x.RosterId, x.GradeTypeId, x.Attempt }).IsUnique();
                e.HasIndex(x => x.StudentId);
            });

            // MathQuizHistory
            modelBuilder.Entity<MathQuizHistory>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentCode);
            });

            // LiteratureQuizHistory
            modelBuilder.Entity<LiteratureQuizHistory>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.StudentCode);
            });

            // SchoolAssetBooking Index
            modelBuilder.Entity<SchoolAssetBooking>(e =>
            {
                e.HasIndex(x => x.AssetId);
            });

            // AttendanceRecord Index
            modelBuilder.Entity<AttendanceRecord>(e =>
            {
                e.HasIndex(x => x.StudentId);
                e.HasOne<Student>()
                    .WithMany()
                    .HasForeignKey(x => x.StudentId)
                    .OnDelete(DeleteBehavior.ClientNoAction);
            });

            // QuizResult Index for fast student lookup and chronological ordering
            modelBuilder.Entity<QuizResult>(e =>
            {
                e.HasIndex(x => new { x.StudentId, x.SubmittedAt });
            });

            // LearningAnalytics Index for fast analytics and timeline rendering
            modelBuilder.Entity<LearningAnalytics>(e =>
            {
                e.HasIndex(x => new { x.StudentId, x.RecordedAt });
            });

            // --- BCH00: SEED CATEGORIES FOR FOREIGN KEY INTEGRITY ---
            modelBuilder.Entity<QuestionBankCategory>().HasData(
                new QuestionBankCategory { Id = 1, Name = "Toán học", Subject = "Toán", Grade = "10", Description = "Danh mục Toán học" },
                new QuestionBankCategory { Id = 2, Name = "Vật lý", Subject = "Vật lý", Grade = "10", Description = "Danh mục Vật lý" },
                new QuestionBankCategory { Id = 3, Name = "Hóa học", Subject = "Hóa học", Grade = "10", Description = "Danh mục Hóa học" },
                new QuestionBankCategory { Id = 4, Name = "Ngữ văn", Subject = "Ngữ văn", Grade = "10", Description = "Danh mục Ngữ văn" },
                new QuestionBankCategory { Id = 5, Name = "Tiếng Anh", Subject = "Tiếng Anh", Grade = "10", Description = "Danh mục Tiếng Anh" }
            );

            /*
            // --- S1-05: SEED 20 C�U H?I M?U ---
            modelBuilder.Entity<QuestionBankItem>().HasData(
                new QuestionBankItem { Id = 1, CategoryId = 1, Subject = "To�n", Content = "2 + 3 = ?", CorrectAnswer = "5", Difficulty = "Easy", QuestionType = "MCQ", OptionsJson = "[\"3\",\"4\",\"5\",\"6\"]", Points = 5, TimeLimitSeconds = 30, Grade = "10", Tags = "so-hoc" },
                new QuestionBankItem { Id = 2, CategoryId = 1, Subject = "To�n", Content = "Giỏi phuong tr�nh: x� - 5x + 6 = 0", CorrectAnswer = "x=2 ho?c x=3", Difficulty = "Medium", QuestionType = "SHORT", OptionsJson = "[]", Points = 10, TimeLimitSeconds = 60, Grade = "10", Tags = "pt-bac-2" },
                new QuestionBankItem { Id = 3, CategoryId = 1, Subject = "To�n", Content = "T�m d?o h�m: f(x) = x� + 2x� - 5x + 1", CorrectAnswer = "3x�+4x-5", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 15, TimeLimitSeconds = 90, Grade = "12", Tags = "dao-ham" },
                new QuestionBankItem { Id = 1, CategoryId = 1, Subject = "Ton", Content = "2 + 3 = ?", CorrectAnswer = "5", Difficulty = "Easy", QuestionType = "MCQ", OptionsJson = "[\"3\",\"4\",\"5\",\"6\"]", Points = 5, TimeLimitSeconds = 30, Grade = "10", Tags = "so-hoc" },
                new QuestionBankItem { Id = 2, CategoryId = 1, Subject = "Ton", Content = "Giỏi phuong trnh: x - 5x + 6 = 0", CorrectAnswer = "x=2 ho?c x=3", Difficulty = "Medium", QuestionType = "SHORT", OptionsJson = "[]", Points = 10, TimeLimitSeconds = 60, Grade = "10", Tags = "pt-bac-2" },
                new QuestionBankItem { Id = 3, CategoryId = 1, Subject = "Ton", Content = "Tm d?o hm: f(x) = x + 2x - 5x + 1", CorrectAnswer = "3x+4x-5", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 15, TimeLimitSeconds = 90, Grade = "12", Tags = "dao-ham" },
                new QuestionBankItem { Id = 4, CategoryId = 1, Subject = "Ton", Content = "Tnh tch phn: ?(0?1) x dx", CorrectAnswer = "1/3", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 20, TimeLimitSeconds = 120, Grade = "12", Tags = "tich-phan" },
                new QuestionBankItem { Id = 5, CategoryId = 2, Subject = "V?t l", Content = "Don v? c?a l?c l g?", CorrectAnswer = "Newton", Difficulty = "Easy", QuestionType = "MCQ", OptionsJson = "[\"Joule\",\"Newton\",\"Watt\",\"Pascal\"]", Points = 5, TimeLimitSeconds = 30, Grade = "10", Tags = "co-hoc" },
                new QuestionBankItem { Id = 6, CategoryId = 2, Subject = "V?t l", Content = "Cng th?c d?nh lu?t II Newton?", CorrectAnswer = "F=ma", Difficulty = "Medium", QuestionType = "MCQ", OptionsJson = "[\"F=mv\",\"F=ma\",\"F=mg\",\"F=m/a\"]", Points = 10, TimeLimitSeconds = 45, Grade = "10", Tags = "dong-luc-hoc" },
                new QuestionBankItem { Id = 7, CategoryId = 2, Subject = "V?t l", Content = "Tnh gia t?c v?t m=2kg ch?u l?c F=10N", CorrectAnswer = "5 m/s", Difficulty = "Medium", QuestionType = "SHORT", OptionsJson = "[]", Points = 10, TimeLimitSeconds = 60, Grade = "10", Tags = "dong-luc-hoc" },
                new QuestionBankItem { Id = 8, CategoryId = 2, Subject = "Vật lý", Content = "Electron có điện tích bằng bao nhiêu?", CorrectAnswer = "-1.6×10⁻¹⁹ C", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 15, TimeLimitSeconds = 60, Grade = "11", Tags = "dien-hoc" },
                new QuestionBankItem { Id = 1, CategoryId = 1, Subject = "Ton", Content = "2 + 3 = ?", CorrectAnswer = "5", Difficulty = "Easy", QuestionType = "MCQ", OptionsJson = "[\"3\",\"4\",\"5\",\"6\"]", Points = 5, TimeLimitSeconds = 30, Grade = "10", Tags = "so-hoc" },
                new QuestionBankItem { Id = 2, CategoryId = 1, Subject = "Ton", Content = "Giỏi phuong trnh: x - 5x + 6 = 0", CorrectAnswer = "x=2 ho?c x=3", Difficulty = "Medium", QuestionType = "SHORT", OptionsJson = "[]", Points = 10, TimeLimitSeconds = 60, Grade = "10", Tags = "pt-bac-2" },
                new QuestionBankItem { Id = 3, CategoryId = 1, Subject = "Ton", Content = "Tm d?o hm: f(x) = x + 2x - 5x + 1", CorrectAnswer = "3x+4x-5", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 15, TimeLimitSeconds = 90, Grade = "12", Tags = "dao-ham" },
                new QuestionBankItem { Id = 4, CategoryId = 1, Subject = "Ton", Content = "Tnh tch phn: ?(0?1) x dx", CorrectAnswer = "1/3", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 20, TimeLimitSeconds = 120, Grade = "12", Tags = "tich-phan" },
                new QuestionBankItem { Id = 5, CategoryId = 2, Subject = "V?t l", Content = "Don v? c?a l?c l g?", CorrectAnswer = "Newton", Difficulty = "Easy", QuestionType = "MCQ", OptionsJson = "[\"Joule\",\"Newton\",\"Watt\",\"Pascal\"]", Points = 5, TimeLimitSeconds = 30, Grade = "10", Tags = "co-hoc" },
                new QuestionBankItem { Id = 6, CategoryId = 2, Subject = "V?t l", Content = "Cng th?c d?nh lu?t II Newton?", CorrectAnswer = "F=ma", Difficulty = "Medium", QuestionType = "MCQ", OptionsJson = "[\"F=mv\",\"F=ma\",\"F=mg\",\"F=m/a\"]", Points = 10, TimeLimitSeconds = 45, Grade = "10", Tags = "dong-luc-hoc" },
                new QuestionBankItem { Id = 7, CategoryId = 2, Subject = "V?t l", Content = "Tnh gia t?c v?t m=2kg ch?u l?c F=10N", CorrectAnswer = "5 m/s", Difficulty = "Medium", QuestionType = "SHORT", OptionsJson = "[]", Points = 10, TimeLimitSeconds = 60, Grade = "10", Tags = "dong-luc-hoc" },
                new QuestionBankItem { Id = 8, CategoryId = 2, Subject = "Vật lý", Content = "Electron có điện tích bằng bao nhiêu?", CorrectAnswer = "-1.6×10⁻¹⁹ C", Difficulty = "Hard", QuestionType = "SHORT", OptionsJson = "[]", Points = 15, TimeLimitSeconds = 60, Grade = "11", Tags = "dien-hoc" },
                ... (omitted) ...
            );
            */

            // --- S1-02: SEED 10 HUY HIỆU MẪU ---
            modelBuilder.Entity<Badge>().HasData(
                new Badge { Id = 1, Name = "⭐ Ngôi sao lớp", Description = "Đạt điểm cao nhất lớp 3 lần liên tiếp", IconPath = "star.png", RequiredPoints = 100 },
                new Badge { Id = 2, Name = "📚 Mọt sách", Description = "Hoàn thành 50 bài tập đúng hạn", IconPath = "book.png", RequiredPoints = 200 },
                new Badge { Id = 3, Name = "🏆 Quán quân Quiz", Description = "Top 1 quiz 5 lần", IconPath = "trophy.png", RequiredPoints = 150 },
                new Badge { Id = 4, Name = "🎯 Bắn trúng đích", Description = "Đạt 100% câu đúng trong 1 quiz", IconPath = "target.png", RequiredPoints = 50 },
                new Badge { Id = 5, Name = "🤝 Bạn tốt", Description = "Giúp đỡ bạn bè qua 10 câu hỏi trong Chat", IconPath = "handshake.png", RequiredPoints = 80 },
                new Badge { Id = 6, Name = "🔥 Streak 7 ngày", Description = "Tham gia học 7 ngày liên tiếp", IconPath = "fire.png", RequiredPoints = 70 },
                new Badge { Id = 7, Name = "💡 Sáng tạo", Description = "Nộp portfolio STEM xuất sắc", IconPath = "bulb.png", RequiredPoints = 120 },
                new Badge { Id = 8, Name = "📝 Nhật ký siêng năng", Description = "Viết nhật ký học tập 30 ngày", IconPath = "diary.png", RequiredPoints = 90 },
                new Badge { Id = 9, Name = "🎮 Game Master", Description = "Hoàn thành tất cả game trong Game Hub", IconPath = "game.png", RequiredPoints = 180 },
                new Badge { Id = 10, Name = "🎓 Cử nhân Mini", Description = "Đạt tổng 1000 XP", IconPath = "graduate.png", RequiredPoints = 1000 }
            );

            modelBuilder.Entity<Bulletin>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.Category);
            });

            modelBuilder.Entity<BulletinApprovalRequest>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.BulletinId);
                e.HasOne(x => x.Bulletin)
                 .WithMany()
                 .HasForeignKey(x => x.BulletinId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<BulletinReadReceipt>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.BulletinId, x.UserId });
            });

            modelBuilder.Entity<BulletinPollOption>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.BulletinId);
            });

            modelBuilder.Entity<BulletinPollVote>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => new { x.OptionId, x.UserId }).IsUnique();
            });

            modelBuilder.Entity<DailyTask>(e =>
            {
                e.HasIndex(x => x.AssignedTo);
                e.HasIndex(x => x.DueDate);
            });
        }

        private static readonly System.Threading.SemaphoreSlim DbWriteSemaphore = new System.Threading.SemaphoreSlim(1, 1);

        public override int SaveChanges()
        {
            bool isUIThread = false;
            try
            {
                isUIThread = System.Windows.Application.Current?.Dispatcher?.CheckAccess() ?? false;
            }
            catch { }

            var pending = PreparePendingJournals();
            int result;

            if (isUIThread)
            {
                bool acquired = DbWriteSemaphore.Wait(100);
                if (acquired)
                {
                    try
                    {
                        UpdateWalletChecksums();
                        result = base.SaveChanges();
                    }
                    finally
                    {
                        DbWriteSemaphore.Release();
                    }
                }
                else
                {
                    Serilog.Log.Warning("DbWriteSemaphore: UI thread bypassed write semaphore to prevent deadlock.");
                    UpdateWalletChecksums();
                    result = base.SaveChanges();
                }
            }
            else
            {
                DbWriteSemaphore.Wait();
                try
                {
                    UpdateWalletChecksums();
                    result = base.SaveChanges();
                }
                finally
                {
                    DbWriteSemaphore.Release();
                }
            }

            WritePendingJournals(pending);
            return result;
        }

        public override async System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
        {
            var pending = PreparePendingJournals();
            await DbWriteSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                UpdateWalletChecksums();
                int result = await base.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                WritePendingJournals(pending);
                return result;
            }
            finally
            {
                DbWriteSemaphore.Release();
            }
        }

        private void UpdateWalletChecksums()
        {
            foreach (var entry in ChangeTracker.Entries<TuitionRecord>())
            {
                if (entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                {
                    var isLockedOriginal = entry.Property(x => x.IsLocked).OriginalValue;
                    if (isLockedOriginal)
                    {
                        throw new InvalidOperationException("Hóa đơn học phí đã khóa sổ, không thể chỉnh sửa.");
                    }
                }
            }

            foreach (var entry in ChangeTracker.Entries<Student>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    var student = entry.Entity;
                    student.WalletChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                        student.StudentCode + student.WalletBalance.ToString("F2")
                    );
                }
            }
            foreach (var entry in ChangeTracker.Entries<TuitionRecord>())
            {
                if (entry.State == EntityState.Added || entry.State == EntityState.Modified)
                {
                    var tuition = entry.Entity;
                    tuition.TuitionChecksum = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(
                        tuition.StudentId + "_" + tuition.Amount.ToString("F2") + "_" + tuition.PaidAmount.ToString("F2") + "_" + tuition.Status
                    );
                }
            }
        }

        public class JournalEntry
        {
            public string EntityType { get; set; } = "";
            public string Action { get; set; } = ""; // "Added", "Modified", "Deleted"
            public System.Collections.Generic.Dictionary<string, object> KeyValues { get; set; } = new();
            public System.Collections.Generic.Dictionary<string, object> PropertyValues { get; set; } = new();
        }

        private class PendingJournal
        {
            public Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry Entry { get; set; } = null!;
            public EntityState State { get; set; }
            public string EntityTypeName { get; set; } = "";
        }

        private List<PendingJournal> PreparePendingJournals()
        {
            var list = new List<PendingJournal>();
            if (IsSeedingOrMigrating || FallbackInMemoryConnection == null) return list;

            try
            {
                foreach (var entry in ChangeTracker.Entries())
                {
                    if (entry.State == EntityState.Added || entry.State == EntityState.Modified || entry.State == EntityState.Deleted)
                    {
                        var entityType = entry.Entity.GetType();
                        var typeName = entityType.FullName ?? entityType.Name;
                        if (typeName.Contains("Castle.Proxies"))
                        {
                            typeName = entityType.BaseType?.FullName ?? typeName;
                        }

                        list.Add(new PendingJournal
                        {
                            Entry = entry,
                            State = entry.State,
                            EntityTypeName = typeName
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Error preparing database pending journals.");
            }
            return list;
        }

        private void WritePendingJournals(List<PendingJournal> pending)
        {
            if (pending == null || pending.Count == 0) return;
            try
            {
                var journalEntries = new List<JournalEntry>();
                foreach (var item in pending)
                {
                    var journal = new JournalEntry
                    {
                        EntityType = item.EntityTypeName,
                        Action = item.State.ToString()
                    };

                    var keyProperties = item.Entry.Metadata.FindPrimaryKey()?.Properties;
                    if (keyProperties != null)
                    {
                        foreach (var prop in keyProperties)
                        {
                            var val = item.Entry.Property(prop.Name).CurrentValue;
                            if (val != null) journal.KeyValues[prop.Name] = val;
                        }
                    }

                    if (item.State == EntityState.Added || item.State == EntityState.Modified)
                    {
                        foreach (var prop in item.Entry.Metadata.GetProperties())
                        {
                            if (prop.IsPrimaryKey()) continue;
                            var val = item.Entry.Property(prop.Name).CurrentValue;
                            if (val != null) journal.PropertyValues[prop.Name] = val;
                        }
                    }

                    CleanValuesForJson(journal.KeyValues);
                    CleanValuesForJson(journal.PropertyValues);
                    journalEntries.Add(journal);
                }

                if (journalEntries.Count > 0)
                {
                    var options = new System.Text.Json.JsonSerializerOptions 
                    { 
                        WriteIndented = false,
                        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                    };

                    var jsonLine = System.Text.Json.JsonSerializer.Serialize(journalEntries, options);
                    System.IO.File.AppendAllText(QASmartClass.Services.AppPaths.TextJournalFile, jsonLine + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Failed to write database changes to text journal file.");
            }
        }

        private static void CleanValuesForJson(System.Collections.Generic.Dictionary<string, object> dict)
        {
            var keys = new List<string>(dict.Keys);
            foreach (var key in keys)
            {
                var val = dict[key];
                if (val == null || val == DBNull.Value)
                {
                    dict.Remove(key);
                }
                else if (val is byte[] bytes)
                {
                    dict[key] = Convert.ToBase64String(bytes);
                }
                else if (val is DateTime dt)
                {
                    dict[key] = dt.ToString("o");
                }
                else if (val is DateTimeOffset dto)
                {
                    dict[key] = dto.ToString("o");
                }
                else if (val is Guid g)
                {
                    dict[key] = g.ToString();
                }
                else if (val.GetType().IsClass && !(val is string))
                {
                    dict[key] = val.ToString() ?? "";
                }
            }
        }
    }

    // ======================= ENTITIES =======================
    public partial class Lesson
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsFavorite { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public List<LessonContent> Contents { get; set; } = new();

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬ v4.0: Status & Workflow �f¢â�,�â�?s¬�f¢â�,�â�?s¬
        /// <summary>Draft | PendingApproval | Approved | Taught</summary>
        public string Status { get; set; } = "Draft";
        /// <summary>Normal | STEAM | Workshop | Exam</summary>
        public string LessonType { get; set; } = "Normal";
        /// <summary>Th�f¡�,»�,i l�f�?��,°�f¡�,»�,£ng d�f¡�,»�,± ki�f¡�,º�,¿n (ph�f�'�,ºt): 45 ho�f¡�,º�,·c 90</summary>
        public int DurationMinutes { get; set; } = 45;
        /// <summary>L�f¡�,º�,§n d�f¡�,º�,¡y g�f¡�,º�,§n nh�f¡�,º�,¥t</summary>
        public DateTime? LastTaughtAt { get; set; }
        /// <summary>S�f¡�,»â�,��o l�f¡�,º�,§n �f�?zâ�,��o�f�'�,£ d�f¡�,º�,¡y</summary>
        public int UseCount { get; set; } = 0;
        /// <summary>Ti�f¡�,º�,¿t d�f¡�,º�,¡y �f�?zâ�,��o�f�?��,°�f¡�,»�,£c l�f�'�,ªn l�f¡�,»â�,�¹ch</summary>
        public DateTime? ScheduledFor { get; set; }
        /// <summary>Tags ph�f�'�,¢n lo�f¡�,º�,¡i: "Ch�f�?��,°�f�?��,¡gn1,L�f�'�,½ thuy�f¡�,º�,¿t,C�f�'�,³ game"</summary>
        public string Tags { get; set; } = string.Empty;
        /// <summary>Feedback t�f¡�,»�,« AI / t�f¡�,»â�,�¢ tr�f�?��,°�f¡�,»�.¸ng sau khi d�f¡�,º�,¡y</summary>
        public string ReviewNotes { get; set; } = string.Empty;
        /// <summary>C�f�'�,³ homework template kh�f�'�,´ng</summary>
        public bool HasHomework { get; set; } = false;
        /// <summary>N�f¡�,»â�?z¢i dung homework</summary>
        public string HomeworkText { get; set; } = string.Empty;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬ v4.2: Timetable & Teaching Assignment �f¢â�,�â�?s¬�f¢â�,�â�?s¬
        /// <summary>Ti�f¡�,º�,¿t h�f¡�,»�,c (1-5 s�f�'�,¡ng, 6-10 chi�f¡�,»�,u)</summary>
        public int Period { get; set; } = 0;
        /// <summary>Th�f¡�,»�,© trong tu�f¡�,º�,§n: "Th�f¡�,»�,© 2" ... "Th�f¡�,»�,© 7"</summary>
        public string DayOfWeek { get; set; } = string.Empty;
        /// <summary>Tu�f¡�,º�,§n h�f¡�,»�,c (1-35)</summary>
        public int WeekNumber { get; set; } = 0;
        /// <summary>H�f¡�,»�,c k�f¡�,»�,³: "HK1" / "HK2"</summary>
        public string Semester { get; set; } = "HK1";
        /// <summary>L�f¡�,»â�,�ºp gi�f¡�,º�,£ng d�f¡�,º�,¡y: "10A", "10B",...</summary>
        public string ClassName { get; set; } = string.Empty;
        /// <summary>GV ch�f�'�,­nh ph�f¡�,»�,¥ tr�f�'�,¡ch</summary>
        public string TeacherName { get; set; } = string.Empty;
        /// <summary>GV d�f¡�,º�,¡y thay (n�f¡�,º�,¿u c�f�'�,³)</summary>
        public string SubstituteTeacher { get; set; } = string.Empty;
        /// <summary>L�f�'�,½ do d�f¡�,º�,¡y thay</summary>
        public string SubstituteReason { get; set; } = string.Empty;
    }

    public class LessonContent
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }

    /// <summary>Version history snapshot �f¢â�?s¬â�,� l�f�?��,°u m�f¡�,»â�,��?�i khi Lesson �f�?zâ�,��o�f�?��,°�f¡�,»�,£c l�f�?��,°u</summary>
    public class LessonHistory
    {
        public int Id { get; set; }
        public int LessonId { get; set; }
        /// <summary>Snapshot ti�f�'�,ªu �f�?zâ�,��o�f¡�,»�, t�f¡�,º�,¡i th�f¡�,»�,i �f�?zâ�,��oi�f¡�,»�?�?Tm l�f�?��,°u</summary>
        public string TitleSnapshot { get; set; } = string.Empty;
        /// <summary>Tr�f¡�,º�,¡ng th�f�'�,¡i t�f¡�,º�,¡i th�f¡�,»�,i �f�?zâ�,��oi�f¡�,»�?�?Tm l�f�?��,°u</summary>
        public string StatusSnapshot { get; set; } = string.Empty;
        /// <summary>JSON snapshot to�f�'�, n b�f¡�,»â�?z¢ contents</summary>
        public string ContentsJson { get; set; } = "[]";
        /// <summary>Ai th�f¡�,»�,±c hi�f¡�,»â�,�¡n thay �f�?zâ�,��o�f¡�,»â�,�¢i</summary>
        public string ChangedBy { get; set; } = "GV";
        /// <summary>Ghi ch�f�'�,º thay �f�?zâ�,��o�f¡�,»â�,�¢i</summary>
        public string ChangeNote { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.Now;
    }

    public class Classroom
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string ClassCode { get; set; } = string.Empty;
        public int MaxStudents { get; set; } = 40;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Quiz
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string QuizType { get; set; } = "Competition"; // Competition, Test, Survey
        public int TimeLimitSeconds { get; set; } = 300;
        public int LessonId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<Question> Questions { get; set; } = new();
    }

    public class Question
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string QuestionType { get; set; } = "MultipleChoice"; // MultipleChoice, TrueFalse, FillBlank, ShortAnswer, Matching, Ordering
        public string OptionsJson { get; set; } = "[]"; // JSON array of options
        public string CorrectAnswer { get; set; } = string.Empty;
        public int Points { get; set; } = 10;
        public string Difficulty { get; set; } = "Medium"; // Easy, Medium, Hard
        public int SortOrder { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public string? VideoUrl { get; set; }
    }

    public class QuizResult
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int StudentId { get; set; }
        public int Score { get; set; }
        public int TotalPoints { get; set; }
        public int CorrectCount { get; set; }
        public int TotalQuestions { get; set; }
        public double TimeSpentSeconds { get; set; }
        public string AnswersJson { get; set; } = "{}";
        public DateTime SubmittedAt { get; set; } = DateTime.Now;
    }

    public class FileTransferRecord
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public string Direction { get; set; } = "TeacherToStudent"; // TeacherToStudent, StudentToTeacher
        public int StudentId { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Transferring, Completed, Failed
        public double ProgressPercent { get; set; }
        public int? AssignmentId { get; set; } // Map to Assignment
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7);
        public int RosterId { get; set; } // L�f¡�,»â�,�ºp h�f¡�,»�,c
        /// <summary>Cho ph�f�'�,©p HS n�f¡�,»â�?z¢p l�f¡�,º�,¡i b�f�'�, i (GV b�f¡�,º�,­t/t�f¡�,º�,¯t)</summary>
        public bool AllowResubmit { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class EventLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string EventType { get; set; } = string.Empty; // Login, Logout, Broadcast, Quiz, FileTransfer, etc.
        public string Actor { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        
        // Thuộc tính mới bổ sung
        public string? MacAddress { get; set; }
        public string? ClientIP { get; set; }
    }

    public class Homework
    {
        public int Id { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Deadline { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string AttachmentPath { get; set; } = string.Empty;
        public string ClassId { get; set; } = string.Empty;
    }

    public class Survey
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SurveyType { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string OptionsJson { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int IsAnonymous { get; set; } = 1;
        public string TargetClasses { get; set; } = string.Empty;
        public string CreatedByTeacher { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class SurveyResponse
    {
        public string Id { get; set; } = string.Empty;
        public string SurveyId { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int SelectedOptionIndex { get; set; }
        public string SelectedOptionText { get; set; } = string.Empty;
        public int ResponseTimeSeconds { get; set; }
        public string SubmittedAt { get; set; } = string.Empty;
        public int IsSynced { get; set; } = 1;
    }

    public class SurveyTemplate
    {
        public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SurveyType { get; set; } = string.Empty;
        public string QuestionText { get; set; } = string.Empty;
        public string OptionsJson { get; set; } = string.Empty;
        public string CreatedAt { get; set; } = string.Empty;
    }




    public class ConductRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int RosterId { get; set; }
        public int PointsDelta { get; set; } // e.g. +5 or -10
        public string Reason { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class InboxMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty; // M�f�'�,£ GV, M�f�'�,£ Ph�f¡�,»�,¥ huynh, v.v.
        public string SenderName { get; set; } = string.Empty;
        public string ReceiverId { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        // C�f�'�,³ th�f¡�,»�?�?T m�f¡�,»�.¸ r�f¡�,»â�?z¢ng th�f�'�, nh ThreadId �f�?zâ�,��o�f¡�,»�?�?T gom nh�f�'�,³m tin nh�f¡�,º�,¯n
        public string ThreadId { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, QUESTION BANK �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    /// <summary>Danh m�f¡�,»�,¥c (th�f�?��,° m�f¡�,»�,¥c) ng�f�'�,¢n h�f�'�, ng c�f�'�,¢u h�f¡�,»�,i</summary>
    public class QuestionBankCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<QuestionBankItem> Items { get; set; } = new();
    }

    /// <summary>C�f�'�,¢u h�f¡�,»�,i trong ng�f�'�,¢n h�f�'�, ng</summary>
    public partial class QuestionBankItem
    {
        public int Id { get; set; }
        public int CategoryId { get; set; }
        /// <summary>MCQ, TF, FIB, MATCH, SHORT, ORDER</summary>
        public string QuestionType { get; set; } = "MCQ";
        public string Content { get; set; } = string.Empty;
        /// <summary>JSON array: options/pairs/steps</summary>
        public string OptionsJson { get; set; } = "[]";
        public string CorrectAnswer { get; set; } = string.Empty;
        public string Explanation { get; set; } = string.Empty;
        public int Points { get; set; } = 10;
        /// <summary>Easy, Medium, Hard</summary>
        public string Difficulty { get; set; } = "Easy";
        public int TimeLimitSeconds { get; set; } = 60;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Tags { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // �f¢â�,�¢�,�f¢â�,�¢�, V6 Shared Question Bank & Analytics �f¢â�,�¢�,�f¢â�,�¢�,
        /// <summary>F1.1: Cho ph�f�'�,©p chia s�f¡�,º�,» c�f�'�,¢u h�f¡�,»�,i l�f�'�,ªn Ng�f�'�,¢n h�f�'�, ng d�f�'�,¹ng chung c�f¡�,»�,§a T�f¡�,»â�,�¢ b�f¡�,»â�?z¢ m�f�'�,´n</summary>
        public bool IsPublicToDepartment { get; set; } = false;
        /// <summary>F1.1: Ph�f�'�,¢n t�f�'�,­ch �f�?zâ�,��o�f¡�,»â�?z¢ kh�f�'�,³ - S�f¡�,»â�,��o h�f¡�,»�,c sinh l�f�'�, m sai</summary>
        public int WrongCount { get; set; } = 0;
        /// <summary>F1.1: Ph�f�'�,¢n t�f�'�,­ch �f�?zâ�,��o�f¡�,»â�?z¢ kh�f�'�,³ - T�f¡�,»â�,�¢ng s�f¡�,»â�,��o l�f�?��,°�f¡�,»�,£t l�f�'�, m b�f�'�, i</summary>
        public int TotalAttempts { get; set; } = 0;

        // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ V7 M1.1: KI�f¡�,»â�,�šM �f�?z�,�f¡�,»�. NH �f�?z�,�f¡�,»â�?s¬ THI T�f¡�,»â�,� CHUY�f�'�. N M�f�'â�,�N (PEER REVIEW) �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
        /// <summary>Tr�f¡�,º�,¡ng th�f�'�,¡i duy�f¡�,»â�,�¡t: Pending, Approved, Rejected</summary>
        public string ApprovalStatus { get; set; } = "Pending";
        
        /// <summary>Ng�f�?��,°�f¡�,»�,i duy�f¡�,»â�,�¡t (T�f¡�,»â�,�¢ tr�f�?��,°�f¡�,»�.¸ng m�f�'�,´n h�f¡�,»�,c)</summary>
        public string ApprovedBy { get; set; } = string.Empty;
        
        /// <summary>Nh�f¡�,º�,­n x�f�'�,©t, g�f�'�,³p �f�'�,½ t�f¡�,»�,« ng�f�?��,°�f¡�,»�,i duy�f¡�,»â�,�¡t</summary>
        public string ReviewComments { get; set; } = string.Empty;
        
        /// <summary>V7 P1.5: Truy vet nguoi tao cau hoi</summary>
        public string CreatedBy { get; set; } = string.Empty;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, REMEMBERED DEVICES �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    /// <summary>
    /// Thi�f¡�,º�,¿t b�f¡�,»â�,�¹ �f�?zâ�,��o�f�'�,£ ghi nh�f¡�,»â�,�º trong l�f¡�,»â�,�ºp h�f¡�,»�,c �f¢â�?s¬â�,� Gi�f�'�,ºp k�f¡�,º�,¿t n�f¡�,»â�,��oi l�f¡�,º�,¡i nhanh
    /// Khi HS k�f¡�,º�,¿t n�f¡�,»â�,��oi l�f¡�,º�,§n �f�?zâ�,��o�f¡�,º�,§u, h�f¡�,»â�,�¡ th�f¡�,»â�,��ong t�f¡�,»�,± ghi nh�f¡�,»â�,�º PCName + IP.
    /// L�f¡�,º�,§n sau c�f�'�,¹ng m�f�'�,¡y k�f¡�,º�,¿t n�f¡�,»â�,��oi l�f¡�,º�,¡i �f¢â�,� â�,��"� t�f¡�,»�,± nh�f¡�,º�,­n di�f¡�,»â�,�¡n HS, kh�f�'�,´ng c�f¡�,º�,§n setup l�f¡�,º�,¡i.
    /// </summary>
    public class RememberedDevice
    {
        public int Id { get; set; }

        /// <summary>T�f�'�,ªn m�f�'�,¡y t�f�'�,­nh (hostname) �f¢â�?s¬â�,� �f�?zâ�,��o�f¡�,»â�,�¹nh danh ch�f�'�,­nh</summary>
        public string PCName { get; set; } = string.Empty;

        /// <summary>Device fingerprint (PCName + MAC) �f�?zâ�,��o�f¡�,»�?�?T nh�f¡�,º�,­n di�f¡�,»â�,�¡n ch�f�'�,­nh x�f�'�,¡c h�f�?��,¡n</summary>
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>�f�?z�,�f¡�,»â�,�¹a ch�f¡�,»â�,�° IP l�f¡�,º�,§n k�f¡�,º�,¿t n�f¡�,»â�,��oi g�f¡�,º�,§n nh�f¡�,º�,¥t</summary>
        public string LastKnownIP { get; set; } = string.Empty;

        /// <summary>�f�?z�,�f¡�,»â�,�¹a ch�f¡�,»â�,�° MAC (n�f¡�,º�,¿u c�f�'�,³)</summary>
        public string MACAddress { get; set; } = string.Empty;

        /// <summary>M�f�'�,£ h�f¡�,»�,c sinh �f�?zâ�,��o�f�?��,°�f¡�,»�,£c g�f�'�,¡n cho m�f�'�,¡y n�f�'�, y</summary>
        public string StudentCode { get; set; } = string.Empty;

        /// <summary>T�f�'�,ªn h�f¡�,»�,c sinh l�f¡�,º�,§n k�f¡�,º�,¿t n�f¡�,»â�,��oi g�f¡�,º�,§n nh�f¡�,º�,¥t</summary>
        public string StudentName { get; set; } = string.Empty;

        /// <summary>Phi�f�'�,ªn b�f¡�,º�,£n �f¡�,»�,©ng d�f¡�,»�,¥ng client</summary>
        public string ClientVersion { get; set; } = string.Empty;

        /// <summary>L�f¡�,»â�,�ºp h�f¡�,»�,c m�f�'�,  thi�f¡�,º�,¿t b�f¡�,»â�,�¹ thu�f¡�,»â�?z¢c v�f¡�,»�, (0 = Global)</summary>
        public int ClassroomId { get; set; }

        /// <summary>L�f¡�,º�,§n k�f¡�,º�,¿t n�f¡�,»â�,��oi �f�?zâ�,��o�f¡�,º�,§u ti�f�'�,ªn</summary>
        public DateTime FirstSeen { get; set; } = DateTime.Now;

        /// <summary>L�f¡�,º�,§n k�f¡�,º�,¿t n�f¡�,»â�,��oi g�f¡�,º�,§n nh�f¡�,º�,¥t</summary>
        public DateTime LastConnected { get; set; } = DateTime.Now;

        /// <summary>T�f¡�,»â�,�¢ng s�f¡�,»â�,��o l�f¡�,º�,§n k�f¡�,º�,¿t n�f¡�,»â�,��oi</summary>
        public int ConnectionCount { get; set; } = 1;

        /// <summary>Thi�f¡�,º�,¿t b�f¡�,»â�,�¹ c�f�'�,²n �f�?zâ�,��o�f�?��,°�f¡�,»�,£c s�f¡�,»�,­ d�f¡�,»�,¥ng kh�f�'�,´ng (GV c�f�'�,³ th�f¡�,»�?�?T �f¡�,º�,©n/x�f�'�,³a)</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Ghi ch�f�'�,º t�f�'�,¹y ch�f¡�,»�,n (VD: "M�f�'�,¡y b�f�'�, n 5", "Ph�f�'�,²ng Lab 2")</summary>
        public string Notes { get; set; } = string.Empty;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, CLASS ROSTER (Ph�f�'�,²ng STEM d�f�'�,¹ng chung) �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    /// <summary>
    /// Danh s�f�'�,¡ch l�f¡�,»â�,�ºp h�f¡�,»�,c �f¢â�?s¬â�,� m�f¡�,»â�,��?�i l�f¡�,»â�,�ºp c�f�'�,³ danh s�f�'�,¡ch HS ri�f�'�,ªng.
    /// Cho ph�f�'�,©p GV chuy�f¡�,»�?�?Tn �f�?zâ�,��o�f¡�,»â�,�¢i nhanh gi�f¡�,»�,¯a c�f�'�,¡c l�f¡�,»â�,�ºp trong 1 ph�f�'�,²ng h�f¡�,»�,c chung.
    /// VD: 10A3 (To�f�'�,¡n, GV H�f�'�,¹ng) �f¢â�,� â�,��"� 11B (L�f�'�,½, GV Mai) �f¢â�,� â�,��"� 12C (H�f�'�,³a, GV Tu�f¡�,º�,¥n)
    /// </summary>
    public partial class ClassRoster
    {
        public int Id { get; set; }

        /// <summary>T�f�'�,ªn l�f¡�,»â�,�ºp: "10A3", "11B", "12C1"...</summary>
        public string ClassName { get; set; } = string.Empty;

        /// <summary>Kh�f¡�,»â�,��oi: "10", "11", "12"</summary>
        public string GradeLevel { get; set; } = string.Empty;

        /// <summary>N�f�?z�?�?Tm h�f¡�,»�,c: "2025-2026"</summary>
        public string SchoolYear { get; set; } = "2025-2026";

        /// <summary>H�f¡�,»�,c k�f¡�,»�,³: "HK1", "HK2"</summary>
        public string Semester { get; set; } = "HK2";

        /// <summary>Gi�f�'�,¡o vi�f�'�,ªn ph�f¡�,»�,¥ tr�f�'�,¡ch</summary>
        public string TeacherName { get; set; } = string.Empty;

        /// <summary>M�f�'�,´n h�f¡�,»�,c: "To�f�'�,¡n", "L�f�'�,½", "H�f�'�,³a"...</summary>
        public string Subject { get; set; } = string.Empty;

        /// <summary>S�f�?z�,© s�f¡�,»â�,��o (t�f¡�,»�,± t�f�'�,­nh t�f¡�,»�,« Students.Count)</summary>
        public int StudentCount { get; set; }

        /// <summary>�f�?z�,ang ho�f¡�,º�,¡t �f�?zâ�,��o�f¡�,»â�?z¢ng (hi�f¡�,»â�,�¡n trong dropdown)</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Ghi ch�f�'�,º t�f�'�,¹y ch�f¡�,»�,n</summary>
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastUsedAt { get; set; } = DateTime.Now;

        /// <summary>Danh s�f�'�,¡ch HS thu�f¡�,»â�?z¢c l�f¡�,»â�,�ºp n�f�'�, y</summary>
        public List<ClassRosterStudent> Students { get; set; } = new();

        /// <summary>T�f�'�,ªn hi�f¡�,»�?�?Tn th�f¡�,»â�,�¹ ng�f¡�,º�,¯n g�f¡�,»�,n cho dropdown</summary>
        public string DisplayName => string.IsNullOrWhiteSpace(Subject)
            ? ClassName
            : $"{ClassName} - {Subject}";
    }

    /// <summary>
    /// Quan h�f¡�,»â�,�¡ N-N gi�f¡�,»�,¯a ClassRoster v�f�'�,  Student.
    /// M�f¡�,»â�?z¢t HS c�f�'�,³ th�f¡�,»�?�?T thu�f¡�,»â�?z¢c nhi�f¡�,»�,u roster (VD: HS l�f¡�,»â�,�ºp 10A h�f¡�,»�,c c�f¡�,º�,£ To�f�'�,¡n l�f¡�,º�,«n L�f�'�,½).
    /// </summary>
    public class ClassRosterStudent
    {
        public int Id { get; set; }

        /// <summary>FK �f¢â�,� â�,��"� ClassRoster</summary>
        public int RosterId { get; set; }

        /// <summary>FK �f¢â�,� â�,��"� Student</summary>
        public int StudentId { get; set; }

        /// <summary>S�f¡�,»â�,��o th�f¡�,»�,© t�f¡�,»�,± / v�f¡�,»â�,�¹ tr�f�'�,­ ng�f¡�,»â�,��"i (1-50)</summary>
        public int SeatNumber { get; set; }

        /// <summary>Ghi ch�f�'�,º: "L�f¡�,»â�,�ºp tr�f�?��,°�f¡�,»�.¸ng", "HS gi�f¡�,»�,i"...</summary>
        public string Notes { get; set; } = string.Empty;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, GRADE MANAGEMENT (Master + Student Grades) �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    /// <summary>
    /// Danh m�f¡�,»�,¥c lo�f¡�,º�,¡i �f�?zâ�,��oi�f¡�,»�?�?Tm �f¢â�?s¬â�,� GV c�f¡�,º�,¥u h�f�'�,¬nh t�f¡�,»�,« Settings.
    /// M�f¡�,»â�,��?�i record = 1 lo�f¡�,º�,¡i �f�?zâ�,��oi�f¡�,»�?�?Tm (VD: "Mi�f¡�,»â�,�¡ng", "KT 15 ph�f�'�,ºt", "KT 1 ti�f¡�,º�,¿t", "Thi HK")
    /// GV c�f�'�,³ th�f¡�,»�?�?T th�f�'�,ªm/s�f¡�,»�,­a/x�f�'�,³a lo�f¡�,º�,¡i �f�?zâ�,��oi�f¡�,»�?�?Tm, thay �f�?zâ�,��o�f¡�,»â�,�¢i h�f¡�,»â�,�¡ s�f¡�,»â�,��o v�f�'�,  s�f¡�,»â�,��o l�f¡�,º�,§n nh�f¡�,º�,­p.
    /// </summary>
    public class GradeTypeMaster
    {
        public int Id { get; set; }

        /// <summary>M�f�'�,£ lo�f¡�,º�,¡i (unique key): "Mieng", "KT15p", "KT1Tiet", "HocKy"</summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>T�f�'�,ªn hi�f¡�,»�?�?Tn th�f¡�,»â�,�¹: "Ki�f¡�,»�?�?Tm tra mi�f¡�,»â�,�¡ng", "KT 15 ph�f�'�,ºt"...</summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>T�f�'�,ªn vi�f¡�,º�,¿t t�f¡�,º�,¯t (header c�f¡�,»â�?z¢t b�f¡�,º�,£ng �f�?zâ�,��oi�f¡�,»�?�?Tm): "M", "15p", "1T", "HK"</summary>
        public string ShortName { get; set; } = string.Empty;

        /// <summary>H�f¡�,»â�,�¡ s�f¡�,»â�,��o: 1, 2, 3 (GV t�f¡�,»�,± c�f¡�,º�,¥u h�f�'�,¬nh)</summary>
        public int Weight { get; set; } = 1;

        /// <summary>S�f¡�,»â�,��o l�f¡�,º�,§n nh�f¡�,º�,­p t�f¡�,»â�,��oi �f�?zâ�,��oa: VD KT15p=2 (l�f¡�,º�,§n 1, l�f¡�,º�,§n 2), HK=1</summary>
        public int MaxAttempts { get; set; } = 1;

        /// <summary>Th�f¡�,»�,© t�f¡�,»�,± hi�f¡�,»�?�?Tn th�f¡�,»â�,�¹ (c�f¡�,»â�?z¢t n�f�'�, o tr�f�?��,°�f¡�,»â�,�ºc)</summary>
        public int SortOrder { get; set; }

        /// <summary>C�f�'�,³ �f�?zâ�,��oang s�f¡�,»�,­ d�f¡�,»�,¥ng kh�f�'�,´ng (soft delete)</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Ghi ch�f�'�,º</summary>
        public string Notes { get; set; } = string.Empty;
    }

    /// <summary>
    /// �f�?z�,i�f¡�,»�?�?Tm s�f¡�,»â�,��o h�f¡�,»�,c sinh �f¢â�?s¬â�,� m�f¡�,»â�,��?�i record = 1 �f�?zâ�,��oi�f¡�,»�?�?Tm c�f¡�,»�,¥ th�f¡�,»�?�?T.
    /// VD: Nguy�f¡�,»â�,�¦n V�f�?z�?�?Tn A, l�f¡�,»â�,�ºp 10A3 (Roster #5), KT 15p l�f¡�,º�,§n 1, �f�?zâ�,��oi�f¡�,»�?�?Tm 8.5
    /// </summary>
    public partial class StudentGrade
    {
        public int Id { get; set; }

        /// <summary>FK �f¢â�,� â�,��"� Student</summary>
        public int StudentId { get; set; }

        /// <summary>FK �f¢â�,� â�,��"� ClassRoster (x�f�'�,¡c �f�?zâ�,��o�f¡�,»â�,�¹nh l�f¡�,»â�,�ºp + m�f�'�,´n)</summary>
        public int RosterId { get; set; }

        /// <summary>FK �f¢â�,� â�,��"� GradeTypeMaster</summary>
        public int GradeTypeId { get; set; }

        /// <summary>L�f¡�,º�,§n th�f¡�,»�,© m�f¡�,º�,¥y (1, 2, 3...) �f¢â�?s¬â�,� kh�f�'�,´ng v�f�?��,°�f¡�,»�,£t qu�f�'�,¡ Master.MaxAttempts</summary>
        public int Attempt { get; set; } = 1;

        /// <summary>�f�?z�,i�f¡�,»�?�?Tm s�f¡�,»â�,��o (0.0 �f¢â�,� â�,��"� 10.0)</summary>
        public double Score { get; set; }

        /// <summary>Ghi ch�f�'�,º: "V�f¡�,º�,¯ng", "Mi�f¡�,»â�,�¦n", "B�f¡�,º�,£o l�f�?��,°u"</summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>GV nh�f¡�,º�,­p �f�?zâ�,��oi�f¡�,»�?�?Tm (m�f�'�,£ GV ho�f¡�,º�,·c t�f�'�,ªn)</summary>
        public string EnteredBy { get; set; } = string.Empty;

        /// <summary>Th�f¡�,»�,i gian nh�f¡�,º�,­p/c�f¡�,º�,­p nh�f¡�,º�,­t</summary>
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    
        /// <summary>Trạng thái duyệt điểm số (Phase 5)</summary>
        public bool IsConfirmed { get; set; } = true;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, MATH QUIZ HISTORY �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    /// <summary>
    /// L�f¡�,»â�,�¹ch s�f¡�,»�,­ l�f�'�, m b�f�'�, i Practice Mode v�f�'�,  Custom Quiz c�f¡�,»�,§a h�f¡�,»�,c sinh trong m�f�'�,´n To�f�'�,¡n
    /// </summary>
    public class MathQuizHistory
    {
        public int Id { get; set; }
        
        /// <summary>Mã học sinh</summary>
        public string StudentCode { get; set; } = string.Empty;
        
        /// <summary>Tên học sinh</summary>
        public string StudentName { get; set; } = string.Empty;
        
        /// <summary>Lớp học</summary>
        public string Grade { get; set; } = string.Empty;
        
        /// <summary>ID chương học</summary>
        public string ChapterId { get; set; } = string.Empty;
        
        /// <summary>Tên chương học</summary>
        public string ChapterName { get; set; } = string.Empty;
        
        /// <summary>Độ khó (Easy, Medium, Hard, Mix)</summary>
        public string Difficulty { get; set; } = "Mix";
        
        /// <summary>Tổng số câu hỏi</summary>
        public int TotalQuestions { get; set; }
        
        /// <summary>Số câu trả lời đúng</summary>
        public int CorrectAnswers { get; set; }
        
        /// <summary>Điểm số hệ 10</summary>
        public double Score => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions * 10.0 : 0;
        
        /// <summary>Thời gian làm bài (giây)</summary>
        public double TimeSpentSeconds { get; set; }
        
        /// <summary>Thời điểm nộp bài</summary>
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        
        /// <summary>Loại bài (Practice, CustomQuiz, MockExam)</summary>
        public string QuizType { get; set; } = "Practice";
        
        /// <summary>Chủ đề (Topic) để phân tích (tùy chọn)</summary>
        public string Topic { get; set; } = string.Empty;

        /// <summary>Chi tiết từng câu hỏi (JSON serialized list of MathProblemResult)</summary>
        public string ProblemResultsJson { get; set; } = "[]";
    }

    /// <summary>
    /// DTO l�f�?��,°u k�f¡�,º�,¿t qu�f¡�,º�,£ chi ti�f¡�,º�,¿t t�f¡�,»�,«ng c�f�'�,¢u h�f¡�,»�,i trong MathQuizHistory
    /// </summary>
    public class MathProblemResult
    {
        public string Question { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Solution { get; set; }
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, TELEMETRY �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public class UsageLog
    {
        public int Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public string EventData { get; set; } = string.Empty;
        public string UserRole { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public long DurationMs { get; set; }
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 5: DAILY SCHOOL DASHBOARD �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public class SchoolEvent
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now.AddHours(1);
        public string Location { get; set; } = string.Empty;
        public string Organizer { get; set; } = string.Empty;
        public string Department { get; set; } = "All";
        public string Status { get; set; } = "Planned";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class DailyTask
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
        public DateTime DueDate { get; set; } = DateTime.Now.Date;
        public string Status { get; set; } = "Pending";
        public string Notes { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int? EventId { get; set; }
        public string TimeFrame { get; set; } = string.Empty; // e.g. "7h00 - 7h30"
        public string Collaborators { get; set; } = string.Empty; // e.g. "PHT - GVCN"
        public string CollaboratorIds { get; set; } = string.Empty; // CSV e.g. "GV01,NV02"
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCritical { get; set; } = false;
        public DateTime? StartTime { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class TimetableEntry
    {
        public int Id { get; set; }
        public int RosterId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int DayOfWeek { get; set; } = 2; // 2=Monday, ..., 7=Saturday
        public int Period { get; set; } = 1;
        public string Room { get; set; } = string.Empty;
    }

    public class Subject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShortName { get; set; } = string.Empty;
        public string ColorHex { get; set; } = "#1976D2";
        public string Icon { get; set; } = "📚";
        public string DefaultRoom { get; set; } = string.Empty;
        public int WeeklyPeriods { get; set; } = 2;
        public bool IsSystem { get; set; } = false;
        public int DisplayOrder { get; set; } = 0;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 6: TASK MANAGEMENT HUB �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public partial class TaskItem
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string AssignedBy { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
        public string Status { get; set; } = "Todo"; // Todo, InProgress, Done, Overdue
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime Deadline { get; set; } = DateTime.Now.AddDays(7).Date;
        public DateTime? CompletedAt { get; set; }
        public int? ParentTaskId { get; set; }
        public string Attachments { get; set; } = "[]"; // JSON
        public string Department { get; set; } = string.Empty;
    }

    public class TaskComment
    {
        public int Id { get; set; }
        public int TaskId { get; set; }
        public string Author { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 7: B�f�'�,O C�f�'�,O & KPI �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public class EvaluationRecord
    {
        public int Id { get; set; }
        public string EvaluatorId { get; set; } = string.Empty;
        public string TargetId { get; set; } = string.Empty;
        public string RoleRelation { get; set; } = "Peer"; // Peer, Manager, Student
        public int Rating { get; set; } = 5;
        public string Comments { get; set; } = string.Empty;
        public DateTime EvaluatedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 8: H�f¡�,»�.�?TC SINH N�f�'â�,�šNG CAO �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public class PortfolioItem
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "Product"; // Product, Achievement, Certificate
        public string FilePath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class LearningDiary
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now.Date;
        public string Content { get; set; } = string.Empty;
        public string Mood { get; set; } = "Happy"; // Happy, Neutral, Stressed
        public string Goals { get; set; } = string.Empty;
        public string Reflection { get; set; } = string.Empty;
    }

    public class SelfEvaluation
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Semester { get; set; } = string.Empty;
        public string Competency { get; set; } = "{}"; // JSON
        public string Character { get; set; } = "{}"; // JSON
        public string TeacherComment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 9: GI�f�'�,O VI�f�'�. N N�f�'â�,�šNG CAO �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public partial class LessonPlan
    {
        public int Id { get; set; }
        public string TeacherId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Template { get; set; } = "Chu�f¡�,º�,©n"; // Chu�f¡�,º�,©n, STEM, D�f¡�,»�,± �f�'�,¡n
        public DateTime WeekDate { get; set; } = DateTime.Now;
        public int Period { get; set; } = 1;
        public string Status { get; set; } = "Draft"; // Draft, Done
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class LessonPlanDraft
    {
        public int Id { get; set; }
        public int? LessonPlanId { get; set; }
        public string TeacherId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Template { get; set; } = "Chuẩn";
        public DateTime WeekDate { get; set; } = DateTime.Now;
        public int Period { get; set; } = 1;
        public DateTime LastSaved { get; set; } = DateTime.Now;
    }

    public class LessonPlanVersion
    {
        public int Id { get; set; }
        public int LessonPlanId { get; set; }
        public int VersionNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SavedAt { get; set; } = DateTime.Now;
        public string Notes { get; set; } = string.Empty;
    }

    public class HomeroomDiary
    {
        public int Id { get; set; }
        public int RosterId { get; set; }
        public DateTime Date { get; set; } = DateTime.Now.Date;
        public int AbsentCount { get; set; } = 0;
        public string DisciplineNotes { get; set; } = string.Empty;
        public string Events { get; set; } = string.Empty;
        public string Reminders { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    public class PeriodLogbook
    {
        public int Id { get; set; }
        public string ClassName { get; set; } = string.Empty;
        public int? LessonId { get; set; }
        public string LessonTitle { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int Period { get; set; } = 1;
        public DateTime Date { get; set; } = DateTime.Today;
        public int TotalStudents { get; set; } = 0;
        public int PresentCount { get; set; } = 0;
        public int AbsentCount { get; set; } = 0;
        public string AbsentNotes { get; set; } = string.Empty;
        public string Rating { get; set; } = "A"; // A (Tốt), B (Khá), C (Trung bình), D (Cần cố gắng)
        public string TeacherComment { get; set; } = string.Empty;
        public string HomeworkAssigned { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 10: THI �f�?z�,UA & GAMIFICATION �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public partial class Badge
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IconPath { get; set; } = string.Empty;
        public int RequiredPoints { get; set; } = 0;
    }

    public class UserBadge
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty; // TeacherId or StudentId
        public int BadgeId { get; set; }
        public DateTime AwardedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, PHASE 11: B�f¡�,º�,¢NG TIN & AI �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    public partial class Bulletin
    {
        public int Id { get; set; }
        public string Category { get; set; } = "School"; // School, YouthUnion, Staff, Student, Competition, Public, TradeUnion, Celebration
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string Audience { get; set; } = "All"; // All, GV, HS, PH, NV
        public string Priority { get; set; } = "Normal"; // High, Normal, Low
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Published"; // Published, Pending, Rejected
        /// <summary>�f�?z�,�f�?��,°�f¡�,»�,ng d�f¡�,º�,«n file �f�?zâ�,��o�f�'�,­nh k�f�'�,¨m (n�f¡�,º�,¿u c�f�'�,³)</summary>
        public string AttachmentPath { get; set; } = string.Empty;
        /// <summary>�f�?z�,�f�?��,°�f¡�,»�,ng d�f¡�,º�,«n �f¡�,º�,£nh minh h�f¡�,»�,a (n�f¡�,º�,¿u c�f�'�,³)</summary>
        public string ImageUrl { get; set; } = string.Empty;

        // P2-06: Scheduling
        /// <summary>Th�f¡�,»�,i gian l�f�'�,ªn l�f¡�,»â�,�¹ch xu�f¡�,º�,¥t b�f¡�,º�,£n (null = xu�f¡�,º�,¥t b�f¡�,º�,£n ngay)</summary>
        public DateTime? ScheduledAt { get; set; }
        /// <summary>Th�f¡�,»�,i gian h�f¡�,º�,¿t h�f¡�,º�,¡n t�f¡�,»�,± �f�?zâ�,��o�f¡�,»â�?z¢ng �f¡�,º�,©n (null = kh�f�'�,´ng h�f¡�,º�,¿t h�f¡�,º�,¡n)</summary>
        public DateTime? ExpiresAt { get; set; }
        /// <summary>�f�?z�,�f�'�,¡nh d�f¡�,º�,¥u th�f�'�,´ng b�f�'�,¡o �f�?zâ�,��o�f�?��,°�f¡�,»�,£c l�f�'�,ªn l�f¡�,»â�,�¹ch</summary>
        public bool IsScheduled { get; set; }

        // Phase 1: Analytics & Connectivity
        public int ViewCount { get; set; } = 0;
        public int LikeCount { get; set; } = 0;
        public string QrCodePath { get; set; } = string.Empty;
    }

    public class BulletinComment
    {
        public int Id { get; set; }
        public int BulletinId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    public class BulletinReadReceipt
    {
        public int Id { get; set; }
        public int BulletinId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime ReadAt { get; set; } = DateTime.Now;
    }

    public class BulletinPollOption
    {
        public int Id { get; set; }
        public int BulletinId { get; set; }
        public string OptionText { get; set; } = string.Empty;
        public int VotesCount { get; set; } = 0;
    }

    public class BulletinPollVote
    {
        public int Id { get; set; }
        public int OptionId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DateTime VotedAt { get; set; } = DateTime.Now;
    }

    public class BulletinApprovalRequest
    {
        public int Id { get; set; }
        public int BulletinId { get; set; }
        public string ApproverId { get; set; } = string.Empty;
        public int Step { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public string Comment { get; set; } = string.Empty;
        public DateTime? ActionedAt { get; set; }

        public Bulletin? Bulletin { get; set; }
    }

    /// <summary>P1-01: Lich su choi game mini cua hoc sinh</summary>
    public class GameSession
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        /// <summary>QuizBattle, WheelSpin, Crossword</summary>
        public string GameType { get; set; } = "QuizBattle";
        public int Score { get; set; }
        public int XpEarned { get; set; }
        public int QuestionsTotal { get; set; }
        public int QuestionsCorrect { get; set; }
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }

    /// <summary>P2-04: Bien ban sinh hoat to chuyen mon</summary>
    public class DeptMeeting
    {
        public int Id { get; set; }
        public string DeptName { get; set; } = string.Empty;
        public DateTime MeetingDate { get; set; } = DateTime.Now;
        public string Agenda { get; set; } = string.Empty;
        public string Minutes { get; set; } = string.Empty;
        public string Attendees { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>P2-04: Cong viec theo doi sau hop to</summary>
    public class DeptMeetingAction
    {
        public int Id { get; set; }
        public int MeetingId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = string.Empty;
        public DateTime DueDate { get; set; } = DateTime.Now.AddDays(7);
        public bool IsCompleted { get; set; }
    }

    public class YouthBchMessage
    {
        public int Id { get; set; }
        public string SenderId { get; set; } = string.Empty;
        public string SenderName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
    }

    /// <summary>P2-09: Muc tieu hoc tap cua HS</summary>
    public class StudentGoal
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public double TargetScore { get; set; }
        public string Semester { get; set; } = "HK1";
        /// <summary>Active, Achieved, Failed</summary>
        public string Status { get; set; } = "Active";
        public double ActualScore { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>P2-05: Phieu du gio danh gia tiet day</summary>
    public class ClassObservation
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string ObserverName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public DateTime ObservedAt { get; set; } = DateTime.Now;
        /// <summary>1-4 diem</summary>
        public int LessonPlanScore { get; set; } = 3;
        public int DeliveryScore { get; set; } = 3;
        public int StudentEngagementScore { get; set; } = 3;
        public int TeacherSupportScore { get; set; } = 3; // M?i CV 5555
        public int AssessmentScore { get; set; } = 3;     // M?i CV 5555
        public double TotalScore { get; set; } = 3.0;     // (25% LP + 30% Delivery + 20% SE + 15% Support + 10% Assess)
        public string Rating { get; set; } = "Kh";        // T?t (>=3.5) / Kh (>=2.5) / D?t (>=1.5) / Chưa đạt (<1.5)
        public string Notes { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;
    }

    /// <summary>P2-08: Khen thuong / Ky luat ca nhan HS</summary>
    public class DisciplineRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        /// <summary>Commendation, Warning, Discipline</summary>
        public string Type { get; set; } = "Warning";
        public string Reason { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public string ReportedBy { get; set; } = string.Empty;

        // --- WI-03: Discipline Module Extension ---
        /// <summary>Nh? / Trung b�nh / N?ng / D?c bi?t nghi�m tr?ng</summary>
        public string ViolationType { get; set; } = string.Empty;
        /// <summary>Nh?c nh? / Khi?n tr�ch / C?nh c�o / Bu?c th�i h?c</summary>
        public string DisciplineLevel { get; set; } = string.Empty;
        /// <summary>N?i dung x? l�</summary>
        public string Resolution { get; set; } = string.Empty;
        /// <summary>D� th�ng b�o PH chua</summary>
        public string ParentNotified { get; set; } = string.Empty;
        /// <summary>Ngu?i ph� duy?t (HT/PHT)</summary>
        public string ApprovedBy { get; set; } = string.Empty;
        /// <summary>Draft / Pending / Approved / Archived</summary>
        public string Status { get; set; } = "Draft";
    }

    /// <summary>P2-01: Ho so tu van tam ly ca nhan</summary>
    public class CounselingProfile
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string CounselorId { get; set; } = string.Empty;
        public string ProblemType { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = StatusConstants.CounselingStatus.Pending; // Phase 5 extension
        public DateTime? ScheduledAt { get; set; } // Phase 5 extension
    }

    /// <summary>P2-01: Ghi chep buoi tu van</summary>
    public class CounselingSession
    {
        public int Id { get; set; }
        public int ProfileId { get; set; }
        public DateTime SessionDate { get; set; } = DateTime.Now;
        public string Content { get; set; } = string.Empty;
        public string Solution { get; set; } = string.Empty;
        public DateTime? FollowUpDate { get; set; }
    }

    /// <summary>P2-03: Ho so suc khoe hoc sinh</summary>
    public class HealthRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Height { get; set; }
        public double Weight { get; set; }
        public double VisionLeft { get; set; }
        public double VisionRight { get; set; }
        public string ChronicConditions { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime ExamDate { get; set; } = DateTime.Now;
        public int? ExactAge { get; set; }
    }

    /// <summary>P2-02: Ket qua trac nghiem huong nghiep</summary>
    public class CareerTestResult
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string TestType { get; set; } = "MBTI"; // MBTI or Holland
        public string ResultCode { get; set; } = string.Empty;
        public string Careers { get; set; } = string.Empty;
        public DateTime TakenAt { get; set; } = DateTime.Now;
    }

    /// <summary>P2-07: Quan ly Cau lac bo</summary>
    public class Club
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Hoc thuat, The thao, Nghe thuat
        public string TeacherId { get; set; } = string.Empty;
        public int MaxMembers { get; set; } = 30;
        public string Description { get; set; } = string.Empty;
    }

    public class ClubMember
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Role { get; set; } = "Member";
        public DateTime JoinedAt { get; set; } = DateTime.Now;
    }

    public class ClubActivity
    {
        public int Id { get; set; }
        public int ClubId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; } = DateTime.Now;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-01: HR MANAGEMENT �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬

    public class Contract
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string ContractNumber { get; set; } = string.Empty;
        public string ContractType { get; set; } = string.Empty; // Probation, 1-Year, Indefinite
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = "Active"; // Active, Expired, Terminated
    }


    public class StaffAttendance
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public TimeSpan? CheckIn { get; set; }
        public TimeSpan? CheckOut { get; set; }
        public string Status { get; set; } = "Present"; // Present, Late, Absent, Leave
        public double WorkingHours { get; set; }
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-02: ASSET MANAGEMENT �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class Asset
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Electronics, Furniture, Vehicle
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime PurchaseDate { get; set; } = DateTime.Today;
        public double Value { get; set; }
        public string Location { get; set; } = string.Empty;
        public string Status { get; set; } = "InUse"; // InUse, UnderRepair, Disposed
        public string AssignedTo { get; set; } = string.Empty;
    }

    public class AssetTransfer
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string FromDept { get; set; } = string.Empty;
        public string ToDept { get; set; } = string.Empty;
        public DateTime TransferDate { get; set; } = DateTime.Today;
        public string Reason { get; set; } = string.Empty;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-03: TUITION FEE �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class TuitionRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public double Amount { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; } = "Unpaid"; // Unpaid, Paid, Overdue
        public string PaymentMethod { get; set; } = string.Empty; // Cash, Transfer
        public double PaidAmount { get; set; }
        public bool IsLocked { get; set; } = false;
        public DateTime? LedgerClosingDate { get; set; }
        public string TuitionChecksum { get; set; } = string.Empty;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-04: EPIDEMIC MONITOR �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class EpidemicCase
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Disease { get; set; } = string.Empty; // S�f¡�,»â�,��ot xu�f¡�,º�,¥t huy�f¡�,º�,¿t, C�f�'�,ºm A, Th�f¡�,»�,§y �f�?zâ�,��o�f¡�,º�,­u...
        public DateTime OnsetDate { get; set; } = DateTime.Today;
        public string Status { get; set; } = "Active"; // Active, Recovered
        public string IsolatedAt { get; set; } = "Home"; // Home, Hospital
    }

    public class FoodSafetyRecord
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string MenuItems { get; set; } = string.Empty;
        public bool SampleKept { get; set; } = false; // L�f�?��,°u m�f¡�,º�,«u 24h
        public string Inspector { get; set; } = string.Empty;
        public string Result { get; set; } = "Pass"; // Pass, Fail
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-05: YOUTH UNION �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    // --- P3-05: YOUTH UNION (BCH Chi Doan) ---
    public class YouthMember
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MemberType { get; set; } = "Doan vien"; // Doan vien, Doi vien
        public DateTime JoinDate { get; set; } = DateTime.Today;
        public string Position { get; set; } = "Thanh vien"; // Thanh vien, Bi thu, Pho BT, UV BCH
        // Task 1.1: New fields
        public string DoanCardNo { get; set; } = string.Empty;
        public string Status { get; set; } = "Active"; // Active, Inactive, Transferred
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string AvatarPath { get; set; } = string.Empty;
        public int TotalScore { get; set; } = 0;
    }

    public class YouthActivity
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Today;
        public string Participants { get; set; } = string.Empty;
        public int Points { get; set; }
        public string Evidence { get; set; } = string.Empty;
        // Task 1.1: New fields
        public string Status { get; set; } = "Draft"; // Draft, Approved, Completed
        public decimal Budget { get; set; } = 0;
        public string Location { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
        public int MaxParticipants { get; set; } = 0;
        public int PlanId { get; set; } = 0;
    }

    // --- BCH02: Diem danh sinh hoat ---
    public class YouthAttendance
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public DateTime SessionDate { get; set; } = DateTime.Today;
        public string SessionType { get; set; } = "Chi Doan";
        public string SessionTitle { get; set; } = string.Empty;
        public bool IsPresent { get; set; } = false;
        public string Note { get; set; } = string.Empty;
    }

    // --- BCH03: Ket nap Doan vien moi ---
    public class YouthRecruitment
    {
        public int Id { get; set; }
        public int? StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Applied"; // Applied, Reviewing, Approved, Rejected
        public DateTime ApplyDate { get; set; } = DateTime.Today;
        public DateTime? ApproveDate { get; set; }
        public string ApprovedBy { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
    }

    public class PushMessageLog
    {
        public int Id { get; set; }
        public int RecipientId { get; set; }
        public string RecipientRole { get; set; } = "Parent";
        public string Title { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public string Type { get; set; } = "General"; // Report, Emergency, Payment, Academic
        public DateTime SentAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Sent"; // Sent, Failed, Delivered, Read
        public string TargetClass { get; set; } = string.Empty;
        public string TargetGrade { get; set; } = string.Empty;
    }

    // --- BCH04: Phi Doan ---
    public class YouthFee
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public decimal Amount { get; set; }
        public string Period { get; set; } = string.Empty;
        public DateTime? PaidDate { get; set; }
        public string Status { get; set; } = "Unpaid"; // Paid, Unpaid
        public string CollectedBy { get; set; } = string.Empty;
    }

    // --- BCH10: Ke hoach hoat dong ---
    public class YouthPlan
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string PeriodType { get; set; } = "Month"; // Month, Quarter, Year
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
        public string Status { get; set; } = "Draft";
        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // --- BCH12: Dang ky tham gia su kien ---
    public class YouthEventRegistration
    {
        public int Id { get; set; }
        public int ActivityId { get; set; }
        public int MemberId { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public DateTime? AttendedAt { get; set; }
        public string Role { get; set; } = "Participant";
    }

    // --- BCH20: Diem thi dua ---
    public class YouthEmulationScore
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public int? ActivityId { get; set; }
        public int Score { get; set; }
        public string Category { get; set; } = "Activity";
        public string Reason { get; set; } = string.Empty;
        public DateTime AwardedDate { get; set; } = DateTime.Today;
        public string AwardedBy { get; set; } = string.Empty;
    }

    // --- BCH21: De xuat khen thuong ---
    public class YouthAwardProposal
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string ProposalType { get; set; } = "Ca nhan";
        public string Reason { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string ProposedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string EvidencePath { get; set; } = string.Empty;
    }

    // --- BCH22: Ho so khen thuong ---
    public class YouthAwardRecord
    {
        public int Id { get; set; }
        public int MemberId { get; set; }
        public string AwardTitle { get; set; } = string.Empty;
        public string DecisionNo { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public DateTime AwardDate { get; set; } = DateTime.Today;
    }

    // --- BCH23: Binh chon ---
    public class YouthVoting
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
        public string Status { get; set; } = "Open";
        public string Candidates { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    public class YouthVote
    {
        public int Id { get; set; }
        public int VotingId { get; set; }
        public int VoterId { get; set; }
        public int CandidateId { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.Now;
    }

    public class YouthVoterRegistry
    {
        public int Id { get; set; }
        public int VotingId { get; set; }
        public int VoterId { get; set; }
        public DateTime VotedAt { get; set; } = DateTime.Now;
    }

    // --- BCH50: Tai lieu Doan ---
    public class YouthDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public string FilePath { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedAt { get; set; } = DateTime.Now;
    }

    // --- BCH53: Ngan sach Doan ---
    public class YouthBudget
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "Income"; // Income, Expense
        public decimal Amount { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string Category { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public string CreatedBy { get; set; } = string.Empty;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-06: SBS TEMPLATES �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class BulletinTemplate
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Theme { get; set; } = string.Empty;
        public string HtmlContent { get; set; } = string.Empty;
        public string ThumbnailPath { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-08: ANONYMOUS COUNSELING �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class AnonymousQuery
    {
        public int Id { get; set; }
        public string EncryptedStudentId { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty; // Hoc tap, Tinh cam, Bao luc
        public string Question { get; set; } = string.Empty;
        public string Answer { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Answered
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-09: SKKN MODULE �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class Skkn
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuthorName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Level { get; set; } = "School"; // School, District, City
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Draft"; // Draft, Submitted, Approved, Rejected
        public string ReviewNote { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.Today;
        public string TeacherCode { get; set; } = string.Empty;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-11: FAMILY GAME �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class FamilyGameSession
    {
        public int Id { get; set; }
        public int ParentId { get; set; }
        public int StudentId { get; set; }
        public int Score { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ P3-12: REMEDIAL PLAN �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class RemedialPlan
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Goal { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; } = DateTime.Today.AddMonths(1);
        public int Progress { get; set; } = 0; // 0 - 100
        public int TeacherId { get; set; }
        public string Status { get; set; } = "Approved"; // Approved / Pending
    }

    public class RemedialSession
    {
        public int Id { get; set; }
        public int PlanId { get; set; }
        public DateTime SessionDate { get; set; } = DateTime.Today;
        public string Content { get; set; } = string.Empty;
        public string Improvement { get; set; } = string.Empty;
    }

    // �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬ PHA 4 MODELS �f¢â�,�â�?s¬�f¢â�,�â�?s¬�f¢â�,�â�?s¬
    public class OfficialDocument
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DocumentNumber { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; } = DateTime.Today;
        public string Type { get; set; } = "Incoming"; // Incoming, Outgoing, Internal
        public string FilePath { get; set; } = string.Empty;
        public string Recipient { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";
        public string Category { get; set; } = string.Empty; // Administrative, Financial, Academic, Emergency
        public string PrimaryHandlerId { get; set; } = string.Empty;
        public DateTime ProcessingDeadline { get; set; }
        public bool IsEscalated { get; set; } = false;
    }

    public class PayrollRecord
    {
        public int Id { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public int Month { get; set; } = DateTime.Today.Month;
        public int Year { get; set; } = DateTime.Today.Year;
        public decimal BaseSalary { get; set; }
        public decimal Allowance { get; set; }
        public decimal Deduction { get; set; }
        public decimal Tax { get; set; }
        public decimal NetSalary => BaseSalary + Allowance - Deduction - Tax;
        public string Status { get; set; } = "Draft"; // Draft, Paid
    }



    public class SchoolMenu
    {
        public int Id { get; set; }
        public DateTime Date { get; set; } = DateTime.Today;
        public string MealType { get; set; } = "Lunch"; // Breakfast, Lunch, Snack
        public string Items { get; set; } = string.Empty;
        public string NutritionInfo { get; set; } = string.Empty;
        public string Allergens { get; set; } = string.Empty;
    }

    public class FoodAllergy
    {
        public int Id { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string StudentCode { get; set; } = string.Empty;
        public string Allergen { get; set; } = string.Empty;
        public string Severity { get; set; } = "Mild"; // Mild, Moderate, Severe
        public string ActionPlan { get; set; } = string.Empty;
    }

    public class MedicalSupply
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = "Medicine"; // Medicine, Equipment, FirstAid
        public int Quantity { get; set; }
        public string Unit { get; set; } = "Box";
        public DateTime ExpiryDate { get; set; } = DateTime.Today.AddYears(1);
        public int MinAlertQty { get; set; } = 10;
    }

    public class EmergencyLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string StudentName { get; set; } = string.Empty;
        public string IncidentType { get; set; } = string.Empty; // Injury, Illness
        public string Description { get; set; } = string.Empty;
        public string FirstAidApplied { get; set; } = string.Empty;
        public string Status { get; set; } = "Resolved"; // Resolved, Hospitalized, ParentsNotified
    }

    public class SocialPost
    {
        public int Id { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public int Likes { get; set; } = 0;
        public string Status { get; set; } = "Published"; // Published, Flagged, Hidden
    }

    public class SocialComment
    {
        public int Id { get; set; }
        public int PostId { get; set; }
        public string AuthorName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Published";
    }

    public class MobileToken
    {
        public int Id { get; set; }
        public int UserId { get; set; } // Li�f�'�,ªn k�f¡�,º�,¿t �f�?zâ�,��o�f¡�,º�,¿n ID h�f¡�,»�,c sinh/ph�f¡�,»�,¥ huynh
        public string Role { get; set; } = "Parent"; // Parent, Student
        public string DeviceToken { get; set; } = string.Empty; // FCM Token ho�f¡�,º�,·c APNs Token
        public string DeviceName { get; set; } = string.Empty;
        public string Platform { get; set; } = "Android"; // Android, iOS
        public DateTime LastActive { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
        public string ApprovalStatus { get; set; } = "Approved"; // Approved, Pending, Rejected
    }


    public class TaskCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ColorCode { get; set; } = "#1976D2";
    }

    public class DocumentRoute
    {
        public int Id { get; set; }
        public string DocumentTitle { get; set; } = string.Empty;
        public string Sender { get; set; } = string.Empty;
        public string Receiver { get; set; } = string.Empty;
        public DateTime SentAt { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected, Forwarded
        public string Notes { get; set; } = string.Empty;
        /// <summary>S2-01: Multi-level: c�f¡�,º�,¥p ph�f�'�,ª duy�f¡�,»â�,�¡t t�f¡�,»â�,��oi �f�?zâ�,��oa (1=1 c�f¡�,º�,¥p, 2=2 c�f¡�,º�,¥p, 3=3 c�f¡�,º�,¥p)</summary>
        public int MaxApprovalLevel { get; set; } = 1;
        /// <summary>S2-01: C�f¡�,º�,¥p hi�f¡�,»â�,�¡n t�f¡�,º�,¡i �f�?zâ�,��o�f�'�,£ duy�f¡�,»â�,�¡t t�f¡�,»â�,�ºi</summary>
        public int CurrentApprovalLevel { get; set; } = 0;
    }

    public class StudentLeaveRequest
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public DateTime LeaveDate { get; set; } = DateTime.Now.Date;
        public string Reason { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string ParentPhone { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending"; // Pending, Approved, Rejected
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public string LeavePassCode => $"LP-{LeaveDate:yyyyMMdd}-{StudentId}";
    }

    public class SystemSetting
    {
        public string Id { get; set; } = string.Empty; // Key (VD: SchoolName, IP_Address)
        public string Value { get; set; } = string.Empty;
        public string Category { get; set; } = "General";
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class BackupLog
    {
        public int Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public double FileSizeBytes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public string TriggeredBy { get; set; } = "System"; // System, Admin
        public string Status { get; set; } = "Success";
    }


    // �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�, V6 AI & SMART PEDAGOGY MODELS �f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,�f¢â�,�¢�,

    /// <summary>F1.3: Ch�f¡�,º�,¥m �f�?zâ�,��oi�f¡�,»�?�?Tm K�f¡�,»�,¹ n�f�?z�?�?Tng m�f¡�,»�,m (Soft-skills Rubric)</summary>
    public class SoftSkillRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public double TeamworkScore { get; set; } // Thang 10
        public double PresentationScore { get; set; } // Thang 10
        public double CriticalThinkingScore { get; set; } // Thang 10
        public string AssessorName { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    /// <summary>F3.1: Qu�f¡�,º�,£n l�f�'�,½ T�f�'�, i s�f¡�,º�,£n v�f�'�,  C�f�?��,¡ s�f¡�,»�.¸ v�f¡�,º�,­t ch�f¡�,º�,¥t ph�f�'�,²ng h�f¡�,»�,c</summary>

    /// <summary>F3.2: Ch�f¡�,»�,¯ k�f�'�,½ �f�?zâ�,��oi�f¡�,»â�,�¡n t�f¡�,»�,­ Ph�f¡�,»�,¥ huynh (Parent E-Signature)</summary>
    public class ParentApproval
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string DocumentType { get; set; } = string.Empty; // GradeReport, ConductReport
        public int ReferenceId { get; set; } // ID b�f¡�,º�,£ng �f�?zâ�,��oi�f¡�,»�?�?Tm
        public string ParentName { get; set; } = string.Empty;
        public string SignatureToken { get; set; } = string.Empty; // M�f�'�,£ OTP / Token gi�f¡�,º�,£ l�f¡�,º�,­p
        public string Status { get; set; } = "Signed"; 
        public string IPAddress { get; set; } = string.Empty;
        public DateTime SignedAt { get; set; } = DateTime.Now;
    }
    // â�?��,�â�?��,�â�?��,� V7 M2: Sá»�?� THEO D�f�?�I T�f�?sM L�f Há»�'C �"�?¯á»�"NG (SEL) â�?��,�â�?��,�â�?��,�
    public partial class StudentMentalHealthRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public int MoodScore { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string RiskLevel { get; set; } = "Low";
        public string DetectedKeywords { get; set; } = string.Empty;
        public DateTime RecordedAt { get; set; } = DateTime.Now;
        
        // V7 P3.5: Quy trinh can thiep 3 cap
        /// <summary>Cap can thiep: 1=Lop, 2=Truong, 3=Chuyen gia</summary>
        public int InterventionLevel { get; set; } = 0;
        /// <summary>Ghi chu can thiep</summary>
        public string InterventionNotes { get; set; } = string.Empty;
        /// <summary>Ngay tai kham</summary>
        public DateTime? FollowUpDate { get; set; }
        /// <summary>Ngay giai quyet xong</summary>
        public DateTime? ResolvedAt { get; set; }
        // V7 P6.1: Trang thai thong bao
        public bool IsNotified { get; set; } = false;
        public DateTime? NotifiedAt { get; set; }
    }

    /// <summary>V7 P3.4: Phan hoi cua hoc sinh ve giao vien</summary>
    public partial class TeacherFeedback
    {
        public int Id { get; set; }
        public int TeacherId { get; set; }
        public int StudentId { get; set; }
        /// <summary>Diem danh gia 1-5</summary>
        public double Score { get; set; } = 3.0;
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }

    // --- P4-WI04: Bằng cấp / Chứng chỉ GV ---
    public class TeacherQualification
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string QualificationType { get; set; } = string.Empty; // C? nh�n/Th?c si/Ti?n si/GCNNN/IELTS
        public string InstitutionName { get; set; } = string.Empty;   // Tru?ng c?p
        public string Major { get; set; } = string.Empty;             // Chuy�n ng�nh
        public DateTime IssuedDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }                     // null = vinh vi?n
        public string CertificateNumber { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;          // Scan b?n g?c
        public string Status { get; set; } = "Active";               // Active/Expired/Pending
    }

    public class TrainingHistory
    {
        public int Id { get; set; }
        public int StaffId { get; set; }
        public string TrainingName { get; set; } = string.Empty;
        public string Organizer { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now;
        public int TotalHours { get; set; }
        public string Result { get; set; } = string.Empty;            // D?t/Kh�ng d?t/Giỏi/Xuất sắc
        public string CertificatePath { get; set; } = string.Empty;
    }

    // P4-WI05: T? Chuy�n M�n
    public class Department
    {
        public int Id { get; set; }
        public string DepartmentName { get; set; } = string.Empty;
        public string HeadTeacherId { get; set; } = string.Empty;
        public string DeputyHeadId { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string SchoolYear { get; set; } = string.Empty;
    }

    public class DepartmentMember
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int StaffId { get; set; }
        public string Role { get; set; } = "Th�nh vi�n"; // T? tru?ng/T? ph�/Th�nh vi�n
        public DateTime JoinedDate { get; set; } = DateTime.Now;
    }

    // === P4-WI06: Khen thưởng ===
    public class AwardRecord
    {
        public int Id { get; set; }
        public string TargetType { get; set; } = string.Empty;  // Student/Teacher/Class
        public int TargetId { get; set; }
        public string TargetName { get; set; } = string.Empty;
        public string AwardType { get; set; } = string.Empty;   // HSG/HSTT/GVDG/LopTienTien/GiayKhen/BangKhen
        public string Semester { get; set; } = string.Empty;    // HK1/HK2/CaNam
        public string SchoolYear { get; set; } = string.Empty;
        public string ProposedBy { get; set; } = string.Empty;
        public string ApprovedBy { get; set; } = string.Empty;
        public string Status { get; set; } = "Proposed";       // Proposed/Approved/Rejected/Printed
        public DateTime ProposedDate { get; set; } = DateTime.Now;
        public DateTime? ApprovedDate { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    // --- P4-WI10: S? Li�n L?c Di?n T? ---
    public class ContactBookEntry
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string SchoolYear { get; set; } = "2025-2026";
        public string Semester { get; set; } = "HK1";
        public double AverageGrade { get; set; } = 0.0;
        public int AbsentDays { get; set; } = 0;
        public string Conduct { get; set; } = "T?t";
        public string TeacherComment { get; set; } = string.Empty;
        public string ParentFeedback { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }

    // --- P4-WI12: Lịch dự giờ Tổ CM ---
    public class ObservationSchedule
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public int ObserverStaffId { get; set; }
        public string ObserverName { get; set; } = string.Empty;
        public int TeacherStaffId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public DateTime ScheduledDate { get; set; } = DateTime.Now;
        public int Period { get; set; } = 1;
        public string Status { get; set; } = "Scheduled"; // Scheduled / Completed / Cancelled
        public string Subject { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
    }

    // --- P4-WI13: Qu?n l� chuy�n d? ---
    public class ProfessionalTopic
    {
        public int Id { get; set; }
        public int DepartmentId { get; set; }
        public string TopicTitle { get; set; } = string.Empty;
        public string Presenter { get; set; } = string.Empty;
        public DateTime PresentationDate { get; set; } = DateTime.Now;
        public double AverageRating { get; set; } = 5.0;
        public int EvaluatorCount { get; set; } = 0;
        public string Status { get; set; } = "Planned"; // Planned / Completed
        public string DocumentPath { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class StudentMIProfile
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public double LinguisticScore { get; set; }
        public double LogicalScore { get; set; }
        public double SpatialScore { get; set; }
        public double MusicalScore { get; set; }
        public double KinestheticScore { get; set; }
        public double InterpersonalScore { get; set; }
        public double IntrapersonalScore { get; set; }
        public double NaturalisticScore { get; set; }
        public DateTime LastUpdated { get; set; } = DateTime.Now;
    }

    public class MiniGameRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string GameName { get; set; } = string.Empty;
        public string TargetMI { get; set; } = string.Empty;
        public int Score { get; set; }
        public int DurationSeconds { get; set; }
        public string AiFeedback { get; set; } = string.Empty;
        public DateTime PlayedAt { get; set; } = DateTime.Now;
    }

    public class LiteratureQuizHistory
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string ChapterId { get; set; } = string.Empty;
        public string ChapterName { get; set; } = string.Empty;
        public string Difficulty { get; set; } = "Mix";
        public int TotalQuestions { get; set; }
        public int CorrectAnswers { get; set; }
        public double Score => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions * 10.0 : 0;
        public double TimeSpentSeconds { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
        public string QuizType { get; set; } = "Practice";
        public string Topic { get; set; } = string.Empty;
        public string ProblemResultsJson { get; set; } = "[]";
    }

    public class LiteratureProblemResult
    {
        public string Question { get; set; } = string.Empty;
        public string UserAnswer { get; set; } = string.Empty;
        public string CorrectAnswer { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
        public string? Solution { get; set; }
    }

    public class GrammarTense
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NameVi { get; set; } = string.Empty;
        public string Structure { get; set; } = string.Empty;
        public string Example { get; set; } = string.Empty;
        public string Signal { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class GrammarQuestion
    {
        public int Id { get; set; }
        public string Category { get; set; } = string.Empty; // "FillBlank" or "MultipleChoice"
        public string QuestionText { get; set; } = string.Empty;
        public string Hint { get; set; } = string.Empty;
        public string AnswersJson { get; set; } = "[]"; // Serialized string array
        public string OptionsJson { get; set; } = "[]"; // Serialized string array
    }

    public class GrammarQuizHistory
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public string Grade { get; set; } = string.Empty;
        public string QuizType { get; set; } = string.Empty; // "FillBlank" or "MultipleChoice"
        public int CorrectAnswers { get; set; }
        public int TotalQuestions { get; set; }
        public double Score => TotalQuestions > 0 ? (double)CorrectAnswers / TotalQuestions * 10.0 : 0;
        public double TimeSpentSeconds { get; set; }
        public DateTime CompletedAt { get; set; } = DateTime.Now;
    }

    public class BookLoan
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public string BookTitle { get; set; } = string.Empty;
        public DateTime BorrowDate { get; set; } = DateTime.Today;
        public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);
        public DateTime? ReturnedDate { get; set; }
    }

    public class PatrolLog
    {
        public int Id { get; set; }
        public string CheckpointCode { get; set; } = string.Empty;
        public DateTime PatrolTime { get; set; } = DateTime.Now;
        public string GuardName { get; set; } = string.Empty;
        public string Status { get; set; } = "Normal";
        public string Notes { get; set; } = string.Empty;
    }

    public class VaccinationRecord
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string DiseaseName { get; set; } = string.Empty;
        public DateTime VaccinationDate { get; set; } = DateTime.Today;
        public DateTime NextDueDate { get; set; } = DateTime.Today.AddYears(1);
    }

    public class GatePickupLog
    {
        public int Id { get; set; }
        public DateTime PickupTime { get; set; } = DateTime.Now;
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public string ParentName { get; set; } = string.Empty;
        public string LicensePlate { get; set; } = string.Empty;
        public string Status { get; set; } = "Completed"; // Pending, Completed
    }

    public class MealFeedback
    {
        public int Id { get; set; }
        public int MenuId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; } = string.Empty;
        public int Rating { get; set; } // 1 to 5
        public string Comment { get; set; } = string.Empty;
        public DateTime FeedbackTime { get; set; } = DateTime.Now;
    }

    public class TuitionReconciliationLog
    {
        public int Id { get; set; }
        public int TuitionRecordId { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public double AmountMatched { get; set; }
        public DateTime ReconciledAt { get; set; }
        public string Status { get; set; } = string.Empty; // Success, Discrepancy, Suspense
        public string Notes { get; set; } = string.Empty;
    }

    public class AssetAuditLog
    {
        public int Id { get; set; }
        public int AssetId { get; set; }
        public string AuditorName { get; set; } = string.Empty;
        public DateTime AuditDate { get; set; }
        public string PhysicalCondition { get; set; } = string.Empty; // Good, WornOut, Broken
        public string Notes { get; set; } = string.Empty;
    }

    public class FoodWasteLog
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public string MealType { get; set; } = string.Empty; // Breakfast, Lunch, Snack
        public int PreparedMeals { get; set; }
        public int WastedMeals { get; set; }
        public double CostOfWaste { get; set; }
        public double WasteRatio { get; set; }
        public string Notes { get; set; } = string.Empty;
    }

    public class LibraryBook
    {
        public int Id { get; set; }
        public string BookCode { get; set; } = string.Empty; // Mã vạch/RFID cuốn sách (Unique)
        public string Title { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public int BookLexileLevel { get; set; } = 400; // Phân cấp trình độ đọc Lexile (ví dụ: 400, 600, 800)
        public int BookConditionScore { get; set; } = 10; // Điểm chất lượng hiện tại (1 - 10, ban đầu là 10)
        public int RepairCount { get; set; } = 0; // Số lần sửa chữa
        public string Status { get; set; } = "Active"; // Active, NeedsRepair, Retired
        public bool ReplenishmentRequired { get; set; } = false; // Cờ đề xuất mua bổ sung
    }

    public class BookReservation
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string BookCode { get; set; } = string.Empty;
        public DateTime ReservationDate { get; set; } = DateTime.Now;
        public DateTime HoldExpirationDate { get; set; }
        public string Status { get; set; } = "PendingPickUp"; // PendingPickUp, PickedUp, Cancelled, Expired
    }

    public class StudentPhysicalRestriction
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string MedicalCondition { get; set; } = string.Empty; // Chẩn đoán y tế nhạy cảm
        public string ExemptionLevel { get; set; } = "None"; // None, ReduceActivity, FullExemption
        public string MovementGuidelines { get; set; } = string.Empty; // Hướng dẫn vận động gửi Giáo viên
        public string AlternativeAssignmentTopic { get; set; } = string.Empty; // Đề tài lý thuyết thay thế
        public DateTime StartDate { get; set; } = DateTime.Today;
        public DateTime EndDate { get; set; }
        public string LoggedByNurse { get; set; } = string.Empty;
    }

    public class MedicalDisposalProposal
    {
        public int Id { get; set; }
        public int MedicalSupplyId { get; set; }
        public int QuantityToDispose { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime ProposalDate { get; set; } = DateTime.Now;
        public string NurseSignature { get; set; } = string.Empty;
        public string PrincipalApprovalSignature { get; set; } = string.Empty;
        public string Status { get; set; } = "PendingApproval"; // PendingApproval, Disposed, Rejected
    }

    public class TrafficCongestionLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public int VehicleCount { get; set; }
        public bool StaggeredDismissalTriggered { get; set; } = false;
        public string SuggestedSequence { get; set; } = string.Empty;
    }

    public class FoodSafetyInspectionLog
    {
        public int Id { get; set; }
        public int SupplierId { get; set; }
        public string BatchCode { get; set; } = string.Empty;
        public string IngredientName { get; set; } = string.Empty;
        public double TestScore { get; set; }
        public bool HasChemicalResidue { get; set; } = false;
        public string InspectionStatus { get; set; } = "Passed"; // Passed, Quarantined
        public DateTime InspectedAt { get; set; } = DateTime.Now;
    }

    public class IntrusionAlarm
    {
        public int Id { get; set; }
        public string SensorCode { get; set; } = string.Empty;
        public DateTime DetectionTime { get; set; } = DateTime.Now;
        public DateTime? VerifiedTime { get; set; }
        public string Status { get; set; } = "Triggered"; // Triggered, Verified, Escalated
    }

    public class Supplier
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactPerson { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public bool IsBlocked { get; set; } = false;
    }

    public class StudentDeviceLicense
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string DeviceCode { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Active"; // Active, Released, Revoked
    }

    public class StudentDeviceStatus
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public string DeviceName { get; set; } = string.Empty;
        public string Status { get; set; } = "Offline"; // Online, Offline, Anomalous
        public DateTime LastHeartbeat { get; set; } = DateTime.Now;
        public double CpuUsage { get; set; }
        public double MemoryUsage { get; set; }
        public string RunningAppsJson { get; set; } = string.Empty;
        public string ActiveWindow { get; set; } = string.Empty;
        public string AnomaliesDetected { get; set; } = string.Empty; // HighCpu, ForbiddenApp, None
        public bool IsLocked { get; set; } = false;
    }

    public class SystemDiagnosticLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string DeviceName { get; set; } = string.Empty;
        public int? StudentId { get; set; }
        public string LogType { get; set; } = "System"; // System, Application, Security, Network
        public string LogLevel { get; set; } = "Info"; // Info, Warning, Error, Critical
        public string Message { get; set; } = string.Empty;
        public string StackTrace { get; set; } = string.Empty;
        public bool IsResolved { get; set; } = false;
        public string DiagnosticResult { get; set; } = string.Empty;
        public string ResolutionAction { get; set; } = string.Empty;
    }

    public class PrincipalOverrideLog
    {
        public int Id { get; set; }
        public string PrincipalId { get; set; } = string.Empty;
        public string TargetResourceType { get; set; } = string.Empty;
        public string TargetResourceId { get; set; } = string.Empty;
        public int StudentId { get; set; }
        public string OverrideReason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string ValidationMethodUsed { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = true;
    }

    public class SensitiveDataAccessAudit
    {
        public int Id { get; set; }
        public string AccessorId { get; set; } = string.Empty;
        public string AccessorRole { get; set; } = string.Empty;
        public string TargetTable { get; set; } = string.Empty;
        public string OperationType { get; set; } = "Read";
        public string QueryFilter { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool IsAnomalous { get; set; } = false;
        public string MitigationActionTaken { get; set; } = "None";
    }

    public class StudyLoadAdjustment
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public double HomeworkReductionRatio { get; set; } = 0.0;
        public int AllowedLexileOffsetAdjustment { get; set; } = 0;
        public string MaxQuizDifficultyAllowed { get; set; } = "Hard";
        public string TriggerReason { get; set; } = string.Empty;
        public string AuthorizedBy { get; set; } = string.Empty;
        public DateTime StartDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; }
        public string Status { get; set; } = "Active";
    }

    public class CampusBeacon
    {
        public string Id { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string AreaZone { get; set; } = "Safe"; // Safe, Restricted
        public double CoordinateX { get; set; }
        public double CoordinateY { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class StudentLocationHistory
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string CurrentZone { get; set; } = string.Empty;
        public string NearbyBeaconId { get; set; } = string.Empty;
        public double CalculatedX { get; set; }
        public double CalculatedY { get; set; }
        public double Rssi { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.Now;
    }

    public class GateOfflineKey
    {
        public int Id { get; set; }
        public string KeyName { get; set; } = "School_Default_Public";
        public string PublicKeyData { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public bool IsActive { get; set; } = true;
    }

    public class GateBarrierLog
    {
        public int Id { get; set; }
        public string StudentCode { get; set; } = string.Empty;
        public string GateId { get; set; } = "MAIN_GATE_01";
        public string CommandAction { get; set; } = "Open"; // Open, Close, Lock
        public string TriggerSource { get; set; } = "QR_LeavePass";
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public bool HardwareConfirmed { get; set; } = false;
    }
}





