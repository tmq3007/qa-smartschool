using System;

using System.IO;

using System.Linq;

using Microsoft.EntityFrameworkCore;

using Serilog;

using Microsoft.Data.Sqlite;



namespace QASmartClass.Services

{

    /// <summary>

    /// Incremental Database Migrator — nâng cấp schema mà KHÔNG xóa dữ liệu.

    /// Thay thế pattern EnsureDeleted + EnsureCreated nguy hiểm.

    /// </summary>

    public static class DbMigrator

    {

        /// <summary>

        /// Thực hiện migration incremental từ version cũ → version mới.

        /// Nếu DB chưa tồn tại → tạo mới bằng EnsureCreated().

        /// Nếu DB đã tồn tại → chạy ALTER TABLE từng bước.

        /// </summary>

        public static void Migrate(Data.AppDbContext db, string targetVersion)

        {

            targetVersion = "6.05.0"; // Force all database migrations up to the current EF Core model version (5.70.0)

            var dbPath = AppPaths.DatabaseFile;

            var versionFile = AppPaths.DbVersionFile;



            // Check if database is corrupt (file exists but core tables like 'Students' are missing)

            // === UPGRADE_08: Ngăn xóa tệp đĩa vật lý và tự động khởi tạo dữ liệu khi hoạt động trên RAM DB Fallback ===

            if (Data.AppDbContext.FallbackInMemoryConnection != null)

            {

                Log.Information("[DbMigrator] Đang hoạt động trên RAM DB Fallback.");

                bool hasStudentsTable = false;

                try

                {

                    var conn = db.Database.GetDbConnection();

                    bool wasClosed = conn.State == System.Data.ConnectionState.Closed;

                    if (wasClosed) conn.Open();

                    using (var cmd = conn.CreateCommand())

                    {

                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Students'";

                        var result = cmd.ExecuteScalar();

                        hasStudentsTable = Convert.ToInt32(result) > 0;

                    }

                    if (wasClosed) conn.Close();

                }

                catch (Exception ex)

                {

                    Log.Warning("[DbMigrator] Error checking for Students table in RAM DB: {Msg}", ex.Message);

                }



                if (!hasStudentsTable)

                {

                    Log.Information("[DbMigrator] RAM DB Fallback trống. Đang khởi tạo các bảng và dữ liệu mẫu...");

                    db.Database.EnsureCreated();

                    SeedDefaultGradeTypes(db);

                    SeedDefaultRolePermissions(db);

                    SeedDefaultHomeworks(db);

                    SeedDefaultGrammar(db);

                    SeedMasterSettings(db);

                    EnsureSubjectsTable(db);

                    EnsurePeriodLogbooksTable(db);

                }

                return;

            }

            else if (File.Exists(dbPath) && new FileInfo(dbPath).Length > 0)

            {

                bool hasStudentsTable = false;

                try

                {

                    var conn = db.Database.GetDbConnection();

                    bool wasClosed = conn.State == System.Data.ConnectionState.Closed;

                    if (wasClosed) conn.Open();

                    using (var cmd = conn.CreateCommand())

                    {

                        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='Students'";

                        var result = cmd.ExecuteScalar();

                        hasStudentsTable = Convert.ToInt32(result) > 0;

                    }

                    if (wasClosed) conn.Close();

                }

                catch (Exception ex)

                {

                    Log.Warning("[DbMigrator] Error checking for Students table: {Msg}", ex.Message);

                }



                if (!hasStudentsTable)

                {

                    Log.Warning("[DbMigrator] Database file exists but core table 'Students' is missing. Database is corrupt or partially initialized. Recreating...");

                    try

                    {

                        db.Database.CloseConnection();

                        // Close all connections and delete database file to force a fresh install

                        SqliteConnection.ClearAllPools();

                        GC.Collect();

                        GC.WaitForPendingFinalizers();

                        

                        if (File.Exists(dbPath))

                        {

                            File.Delete(dbPath);

                            Log.Information("[DbMigrator] Deleted corrupt database file at {Path}", dbPath);

                        }

                        if (File.Exists(versionFile))

                        {

                            File.Delete(versionFile);

                        }

                    }

                    catch (Exception ex)

                    {

                        Log.Error("[DbMigrator] Failed to delete corrupt database file: {Msg}", ex.Message);

                    }

                }

            }



            // Case 1: DB chưa tồn tại hoặc rỗng (0-byte) → tạo mới (fresh install)

            if (!File.Exists(dbPath) || new FileInfo(dbPath).Length == 0)

            {

                Log.Information("[DbMigrator] Fresh install — creating new database");

                if (File.Exists(dbPath) && new FileInfo(dbPath).Length == 0)

                {

                    try { File.Delete(dbPath); } catch { }

                }

                db.Database.EnsureCreated();

                SeedDefaultGradeTypes(db);

                SeedDefaultRolePermissions(db);

                SeedDefaultHomeworks(db);

                SeedDefaultGrammar(db);

                SeedMasterSettings(db);

                EnsureSubjectsTable(db);

                EnsurePeriodLogbooksTable(db);

                SaveVersion(versionFile, targetVersion);

                return;

            }



            // Case 2: DB đã tồn tại → kiểm tra version

            var currentVersion = File.Exists(versionFile)

                ? File.ReadAllText(versionFile).Trim()

                : "0.0.0";



            if (currentVersion == targetVersion)

            {

                if (!TableExists(db, "GrammarTenses") ||

                    !ColumnExists(db, "StudentGrades", "IsConfirmed") ||

                    !ColumnExists(db, "Questions", "VideoUrl") ||

                    !ColumnExists(db, "Questions", "Explanation") ||

                    !ColumnExists(db, "Bulletins", "Category") ||

                    !TableExists(db, "BulletinApprovalRequests") ||

                    !TableExists(db, "BulletinReadReceipts") ||

                    !TableExists(db, "BulletinPollOptions") ||

                    !TableExists(db, "BulletinPollVotes"))

                {

                    Log.Warning("[DbMigrator] Database version is v{Ver} but schema is out of sync (missing tables or columns). Forcing migration run...", currentVersion);

                    currentVersion = "5.0.0"; // Force run migrations from v5.0.0 onwards

                }

                else

                {

                    Log.Information("[DbMigrator] Database is up-to-date (v{Ver})", targetVersion);

                    SeedMasterSettings(db);

                    EnsureSubjectsTable(db);

                    EnsurePeriodLogbooksTable(db);

                    SeedDefaultRolePermissions(db);

                    return;

                }

            }



            Log.Information("[DbMigrator] Upgrading database: v{Old} → v{New}", currentVersion, targetVersion);



            // Backup trước khi migrate

            BackupBeforeMigration(dbPath);



            using (var transaction = db.Database.BeginTransaction())

            {

                // Chạy migration từng bước

                try

                {

                    var ver = ParseVersion(currentVersion);



                    // v0.0.0 → v4.0.0: DB quá cũ, không có schema → tạo lại

                    if (ver < ParseVersion("4.0.0"))

                    {

                        Log.Warning("[DbMigrator] DB quá cũ (v{Ver}), tạo lại schema...", currentVersion);

                        db.Database.EnsureCreated();

                        transaction.Commit();

                        SaveVersion(versionFile, targetVersion);

                        return;

                    }



                    // v4.0.0 → v4.5.0: Thêm các bảng Question Bank, Teacher, Device Memory

                    if (ver < ParseVersion("4.5.0"))

                    {

                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS QuestionBankCategories (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            Name TEXT NOT NULL, Subject TEXT, Grade TEXT,

                            Description TEXT, CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS QuestionBankItems (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            CategoryId INTEGER NOT NULL, QuestionType TEXT NOT NULL,

                            Content TEXT NOT NULL, OptionsJson TEXT, CorrectAnswer TEXT,

                            Difficulty TEXT, Points INTEGER DEFAULT 10,

                            Subject TEXT, Grade TEXT,

                            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,

                            FOREIGN KEY (CategoryId) REFERENCES QuestionBankCategories(Id))");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS RememberedDevices (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            DeviceName TEXT, IPAddress TEXT, MacAddress TEXT,

                            AssignedStudentName TEXT, SeatNumber INTEGER,

                            LastSeen TEXT, Notes TEXT)");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS ClassRosters (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            ClassName TEXT NOT NULL, SchoolYear TEXT, Semester TEXT,

                            GradeLevel TEXT, TotalStudents INTEGER DEFAULT 0,

                            CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS ClassRosterStudents (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            RosterId INTEGER NOT NULL, StudentCode TEXT, FullName TEXT,

                            SeatNumber INTEGER, Gender TEXT, Notes TEXT,

                            FOREIGN KEY (RosterId) REFERENCES ClassRosters(Id))");



                        Log.Information("[DbMigrator] Applied migration: v4.0 → v4.5");

                    }



                    // v4.5.0 → v4.6.0: Thêm bảng Attendance, UsageLogs, Grades

                    if (ver < ParseVersion("4.6.0"))

                    {

                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS AttendanceRecords (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            StudentId INTEGER NOT NULL, ClassroomId INTEGER,

                            Date TEXT NOT NULL, Status TEXT NOT NULL DEFAULT 'Present',

                            Note TEXT, RecordedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,

                            FOREIGN KEY (StudentId) REFERENCES Students(Id))");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS UsageLogs (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            ToolName TEXT, Action TEXT, Duration INTEGER,

                            SessionId TEXT, RecordedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS GradeTypeMasters (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            Name TEXT NOT NULL, Weight REAL DEFAULT 1.0,

                            Description TEXT)");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentGrades (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            StudentId INTEGER NOT NULL, GradeTypeId INTEGER NOT NULL,

                            Score REAL, MaxScore REAL DEFAULT 10.0,

                            Subject TEXT, Semester TEXT,

                            RecordedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,

                            FOREIGN KEY (StudentId) REFERENCES Students(Id),

                            FOREIGN KEY (GradeTypeId) REFERENCES GradeTypeMasters(Id))");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS MathQuizHistories (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            StudentName TEXT, Score INTEGER, TotalQuestions INTEGER,

                            TimeTaken INTEGER, Difficulty TEXT,

                            RecordedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)");



                        Log.Information("[DbMigrator] Applied migration: v4.5 → v4.6");

                    }



                    // v4.6.0 → v4.7.0: Learning Analytics + Rubric System

                    if (ver < ParseVersion("4.7.0"))

                    {

                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS LearningAnalytics (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            StudentId INTEGER NOT NULL, Subject TEXT, Topic TEXT,

                            Score REAL NOT NULL, MaxScore REAL DEFAULT 10.0,

                            QuizId INTEGER, AssessmentType TEXT DEFAULT 'Quiz',

                            RecordedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,

                            FOREIGN KEY (StudentId) REFERENCES Students(Id))");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentAchievements (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            StudentId INTEGER NOT NULL, AchievementType TEXT,

                            AchievementName TEXT, Description TEXT, Icon TEXT DEFAULT '🏆',

                            EarnedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP,

                            FOREIGN KEY (StudentId) REFERENCES Students(Id))");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS Rubrics (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            Title TEXT NOT NULL, Subject TEXT, Grade TEXT,

                            CreatedBy TEXT, CreatedAt TEXT NOT NULL DEFAULT CURRENT_TIMESTAMP)");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS RubricCriteria (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            RubricId INTEGER NOT NULL, CriteriaName TEXT NOT NULL,

                            MaxPoints REAL DEFAULT 10.0,

                            Level1Desc TEXT DEFAULT 'Chưa đạt', Level2Desc TEXT DEFAULT 'Đạt',

                            Level3Desc TEXT DEFAULT 'Khá', Level4Desc TEXT DEFAULT 'Giỏi',

                            SortOrder INTEGER DEFAULT 0,

                            FOREIGN KEY (RubricId) REFERENCES Rubrics(Id))");



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS RubricGrades (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            RubricId INTEGER NOT NULL, StudentId INTEGER NOT NULL,

                            CriteriaId INTEGER NOT NULL, Level INTEGER DEFAULT 0,

                            Points REAL DEFAULT 0, Feedback TEXT, GradedAt TEXT,

                            GradedBy TEXT,

                            FOREIGN KEY (RubricId) REFERENCES Rubrics(Id),

                            FOREIGN KEY (StudentId) REFERENCES Students(Id))");



                        Log.Information("[DbMigrator] Applied migration: v4.6 → v4.7");

                    }



                    // Upgrading to v5.25.0: Cảnh báo số dư ví Canteen tùy chỉnh (V25)

                    if (ver < ParseVersion("5.25.0"))

                    {

                        RunSafeSql(db, "ALTER TABLE Students ADD COLUMN LowBalanceThreshold TEXT NOT NULL DEFAULT '30000'");

                        Log.Information("[DbMigrator] Applied migration to v5.25.0 (LowBalanceThreshold)");

                    }



                    // Upgrading to v5.27.0: Bảng RolePermissions và AssetBookings (V27)

                    if (ver < ParseVersion("5.27.0"))

                    {

                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS RolePermissions (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            Role TEXT NOT NULL,

                            PermissionTag TEXT NOT NULL

                        );");



                        // Seed default role permissions (compat with V26 static map)

                        string[] rbacStatements = new string[]

                        {

                            // BaoVe

                            "SELECT 'BaoVe', 'Gate' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'BaoVe' AND PermissionTag = 'Gate')",

                            "SELECT 'BaoVe', 'security' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'BaoVe' AND PermissionTag = 'security')",

                            "SELECT 'BaoVe', 'emergency' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'BaoVe' AND PermissionTag = 'emergency')",

                            // YTe

                            "SELECT 'YTe', 'counseling' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'YTe' AND PermissionTag = 'counseling')",

                            "SELECT 'YTe', 'health' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'YTe' AND PermissionTag = 'health')",

                            "SELECT 'YTe', 'epidemic' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'YTe' AND PermissionTag = 'epidemic')",

                            "SELECT 'YTe', 'food_safety' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'YTe' AND PermissionTag = 'food_safety')",

                            "SELECT 'YTe', 'medical_inventory' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'YTe' AND PermissionTag = 'medical_inventory')",

                            "SELECT 'YTe', 'emergency' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'YTe' AND PermissionTag = 'emergency')",

                            // LaoCong

                            "SELECT 'LaoCong', 'janitor' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'LaoCong' AND PermissionTag = 'janitor')",

                            // Bep

                            "SELECT 'Bep', 'Canteen' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Bep' AND PermissionTag = 'Canteen')",

                            "SELECT 'Bep', 'kitchen' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Bep' AND PermissionTag = 'kitchen')",

                            "SELECT 'Bep', 'food_safety' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Bep' AND PermissionTag = 'food_safety')",

                            // Ketoan

                            "SELECT 'Ketoan', 'Canteen' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Ketoan' AND PermissionTag = 'Canteen')",

                            "SELECT 'Ketoan', 'assets' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Ketoan' AND PermissionTag = 'assets')",

                            "SELECT 'Ketoan', 'tuition' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Ketoan' AND PermissionTag = 'tuition')",

                            "SELECT 'Ketoan', 'payroll' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Ketoan' AND PermissionTag = 'payroll')",

                            "SELECT 'Ketoan', 'youth_fee' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Ketoan' AND PermissionTag = 'youth_fee')",

                            "SELECT 'Ketoan', 'youth_planbudget' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Ketoan' AND PermissionTag = 'youth_planbudget')",

                            // ThuQuy

                            "SELECT 'ThuQuy', 'Canteen' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'ThuQuy' AND PermissionTag = 'Canteen')",

                            "SELECT 'ThuQuy', 'assets' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'ThuQuy' AND PermissionTag = 'assets')",

                            "SELECT 'ThuQuy', 'tuition' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'ThuQuy' AND PermissionTag = 'tuition')",

                            "SELECT 'ThuQuy', 'payroll' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'ThuQuy' AND PermissionTag = 'payroll')",

                            "SELECT 'ThuQuy', 'youth_fee' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'ThuQuy' AND PermissionTag = 'youth_fee')",

                            "SELECT 'ThuQuy', 'youth_planbudget' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'ThuQuy' AND PermissionTag = 'youth_planbudget')",

                            // Counselor

                            "SELECT 'Counselor', 'counseling' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'Counselor' AND PermissionTag = 'counseling')",

                            // HieuPho

                            "SELECT 'HieuPho', 'push_notification' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'push_notification')",

                            "SELECT 'HieuPho', 'staff_performance' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'staff_performance')",

                            "SELECT 'HieuPho', 'moet' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'moet')",

                            "SELECT 'HieuPho', 'reports' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'reports')",

                            "SELECT 'HieuPho', 'document_manager' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'document_manager')",

                            "SELECT 'HieuPho', 'leave_request' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'leave_request')",

                            "SELECT 'HieuPho', 'mobile_app' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'mobile_app')",

                            "SELECT 'HieuPho', 'app_analytics' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'app_analytics')",

                            "SELECT 'HieuPho', 'tuition' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'tuition')",

                            "SELECT 'HieuPho', 'assets' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'assets')",

                            "SELECT 'HieuPho', 'counseling' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'counseling')",

                            "SELECT 'HieuPho', 'health' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'health')",

                            "SELECT 'HieuPho', 'epidemic' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'epidemic')",

                            "SELECT 'HieuPho', 'food_safety' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'food_safety')",

                            "SELECT 'HieuPho', 'medical_inventory' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'medical_inventory')",

                            "SELECT 'HieuPho', 'emergency' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'emergency')",

                            "SELECT 'HieuPho', 'Gate' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'Gate')",

                            "SELECT 'HieuPho', 'Canteen' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'Canteen')",

                            "SELECT 'HieuPho', 'security' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'security')",

                            "SELECT 'HieuPho', 'janitor' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'janitor')",

                            "SELECT 'HieuPho', 'kitchen' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'kitchen')",

                            "SELECT 'HieuPho', 'department_mgmt' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'department_mgmt')",

                            "SELECT 'HieuPho', 'award_mgmt' WHERE NOT EXISTS (SELECT 1 FROM RolePermissions WHERE Role = 'HieuPho' AND PermissionTag = 'award_mgmt')"

                        };



                        foreach (var stmt in rbacStatements)

                        {

                            RunSafeSql(db, $"INSERT INTO RolePermissions (Role, PermissionTag) {stmt};");

                        }



                        RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS AssetBookings (

                            Id INTEGER PRIMARY KEY AUTOINCREMENT,

                            AssetId INTEGER NOT NULL,

                            BookedBy TEXT NOT NULL,

                            BookingDate TEXT NOT NULL,

                            TimeSlot INTEGER NOT NULL

                        );");



                        Log.Information("[DbMigrator] Applied migration to v5.27.0 (RolePermissions & AssetBookings)");

                    }



                    // Upgrading to v5.30.0: Mật khẩu băm tự động và xóa mật khẩu thô

                    if (ver < ParseVersion("5.30.0"))

                    {

                        try

                        {

                            var teachers = db.TeacherProfiles.ToList();

                            bool changed = false;

                            foreach (var teacher in teachers)

                            {

                                if (string.IsNullOrEmpty(teacher.PasswordHash) && !string.IsNullOrEmpty(teacher.TeacherPassword))

                                {

                                    teacher.PasswordHash = QASmartTouch.Services.AuthenticationService.HashPassword(teacher.TeacherPassword);

                                    teacher.TeacherPassword = "";

                                    changed = true;

                                }

                                else if (!string.IsNullOrEmpty(teacher.TeacherPassword))

                                {

                                    teacher.TeacherPassword = "";

                                    changed = true;

                                }

                            }

                            if (changed)

                            {

                                db.SaveChanges();

                                Log.Information("[DbMigrator] Successfully migrated plaintext passwords to PBKDF2 hashes for all teachers");

                            }

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to migrate plaintext passwords during v5.30.0 migration");

                        }

                        Log.Information("[DbMigrator] Applied migration to v5.30.0 (Password Hashing Migration)");

                    }



                    // Upgrading to v5.31.0: Seed all default role permissions into database RolePermissions table

                    if (ver < ParseVersion("5.31.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS RolePermissions (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                Role TEXT NOT NULL,

                                PermissionTag TEXT NOT NULL

                            );");



                            var defaultPermissions = new System.Collections.Generic.Dictionary<string, string[]>

                            {

                                ["BaoVe"] = new[] { "Gate", "security", "emergency" },

                                ["YTe"] = new[] { "counseling", "health", "epidemic", "food_safety", "medical_inventory", "emergency" },

                                ["LaoCong"] = new[] { "janitor" },

                                ["Bep"] = new[] { "Canteen", "kitchen", "food_safety" },

                                ["Ketoan"] = new[] { "Canteen", "assets", "tuition", "payroll", "reports", "youth_fee", "youth_planbudget" },

                                ["ThuQuy"] = new[] { "Canteen", "tuition", "payroll", "youth_fee", "youth_planbudget" },

                                ["Counselor"] = new[] { "counseling" },

                                ["Librarian"] = new[] { "library", "document_manager", "reports" },

                                ["GV"] = new[] { "department_mgmt", "award_mgmt" },

                                ["HieuPho"] = new[] { 

                                    "Overview", "push_notification", "staff_performance", "moet", "reports", "document_manager", 

                                    "leave_request", "mobile_app", "app_analytics", "tuition", "assets", "counseling", 

                                    "health", "epidemic", "food_safety", "medical_inventory", "emergency", "Gate", 

                                    "Canteen", "security", "janitor", "kitchen", "department_mgmt", "award_mgmt"

                                }

                            };



                            bool changed = false;

                            foreach (var kvp in defaultPermissions)

                            {

                                var role = kvp.Key;

                                foreach (var tag in kvp.Value)

                                {

                                    // Check if this role permission already exists

                                    var exists = db.RolePermissions.Any(rp => rp.Role == role && rp.PermissionTag == tag);

                                    if (!exists)

                                    {

                                        db.RolePermissions.Add(new Data.RolePermission

                                        {

                                            Role = role,

                                            PermissionTag = tag

                                        });

                                        changed = true;

                                    }

                                }

                            }



                            if (changed)

                            {

                                db.SaveChanges();

                                Log.Information("[DbMigrator] Seeded missing default role permissions into database");

                            }

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to seed role permissions during v5.31.0 migration");

                        }

                        Log.Information("[DbMigrator] Applied migration to v5.31.0 (Dynamic RBAC Seeding)");

                    }



                    // Upgrading to v5.33.0: Add database indexes for performance optimization

                    if (ver < ParseVersion("5.33.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_AssetBookings_AssetId ON AssetBookings (AssetId);");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentGrades_StudentId ON StudentGrades (StudentId);");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_AttendanceRecords_StudentCode ON AttendanceRecords (StudentCode);");

                            Log.Information("[DbMigrator] Successfully created indexes for database performance optimization");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create database indexes during v5.33.0 migration");

                        }

                        Log.Information("[DbMigrator] Applied migration to v5.33.0 (Database Indexing Upgrade)");

                    }



                    // Upgrading to v5.36.0: Add WalletChecksum column to Students table

                    if (ver < ParseVersion("5.36.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE Students ADD COLUMN WalletChecksum TEXT NOT NULL DEFAULT ''");

                            Log.Information("[DbMigrator] Applied migration to v5.36.0 (WalletChecksum)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to add WalletChecksum column during v5.36.0 migration");

                        }

                    }



                    // Upgrading to v5.37.0: Add StudentCode column to FoodAllergies table (V37)

                    if (ver < ParseVersion("5.37.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE FoodAllergies ADD COLUMN StudentCode TEXT NOT NULL DEFAULT ''");

                            Log.Information("[DbMigrator] Applied migration to v5.37.0 (FoodAllergies StudentCode)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to add StudentCode column during v5.37.0 migration");

                        }

                    }



                    // Upgrading to v5.38.0: Create Surveys and SurveyResponses tables for Quick Survey 4.3 upgrade

                    if (ver < ParseVersion("5.38.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS Surveys (

                                Id TEXT PRIMARY KEY,

                                Title TEXT NOT NULL,

                                Description TEXT,

                                SurveyType TEXT NOT NULL,

                                QuestionText TEXT NOT NULL,

                                OptionsJson TEXT NOT NULL,

                                TimeLimitSeconds INTEGER NOT NULL DEFAULT 0,

                                IsAnonymous INTEGER NOT NULL DEFAULT 1,

                                TargetClasses TEXT NOT NULL,

                                CreatedByTeacher TEXT NOT NULL,

                                CreatedAt TEXT NOT NULL

                            );");



                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS SurveyResponses (

                                Id TEXT PRIMARY KEY,

                                SurveyId TEXT NOT NULL,

                                StudentCode TEXT NOT NULL,

                                StudentName TEXT NOT NULL,

                                SelectedOptionIndex INTEGER NOT NULL,

                                SelectedOptionText TEXT NOT NULL,

                                ResponseTimeSeconds INTEGER NOT NULL,

                                SubmittedAt TEXT NOT NULL,

                                FOREIGN KEY (SurveyId) REFERENCES Surveys(Id) ON DELETE CASCADE

                            );");



                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SurveyResponses_SurveyId ON SurveyResponses (SurveyId);");

                            Log.Information("[DbMigrator] Created Surveys and SurveyResponses tables successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create Surveys and SurveyResponses tables during v5.38.0 migration");

                        }

                    }



                    // Upgrading to v5.39.0: Create SurveyTemplates table for saving custom question templates

                    if (ver < ParseVersion("5.39.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS SurveyTemplates (

                                Id TEXT PRIMARY KEY,

                                Title TEXT NOT NULL,

                                Description TEXT,

                                SurveyType TEXT NOT NULL,

                                QuestionText TEXT NOT NULL,

                                OptionsJson TEXT NOT NULL,

                                CreatedAt TEXT NOT NULL

                            );");

                            Log.Information("[DbMigrator] Created SurveyTemplates table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create SurveyTemplates table during v5.39.0 migration");

                        }

                    }



                    // Upgrading to v5.40.0: Add ImageUrl column to Questions table for quiz images

                    if (ver < ParseVersion("5.40.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE Questions ADD COLUMN ImageUrl TEXT NOT NULL DEFAULT ''");

                            Log.Information("[DbMigrator] Added ImageUrl column to Questions table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to add ImageUrl column during v5.40.0 migration");

                        }

                    }



                    // Upgrading to v5.41.0: Create Homeworks table and migrate old logs

                    if (ver < ParseVersion("5.41.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS Homeworks (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                Subject TEXT NOT NULL DEFAULT '',

                                Title TEXT NOT NULL DEFAULT '',

                                Description TEXT NOT NULL DEFAULT '',

                                Deadline TEXT NOT NULL DEFAULT '',

                                CreatedAt TEXT NOT NULL DEFAULT '',

                                AttachmentPath TEXT NOT NULL DEFAULT '',

                                ClassId TEXT NOT NULL DEFAULT ''

                            )");

                            Log.Information("[DbMigrator] Created Homeworks table successfully");



                            // Migrate old event logs to the new Homeworks table

                            var oldLogs = db.EventLogs.Where(e => e.EventType == "HOMEWORK").ToList();

                            foreach (var log in oldLogs)

                            {

                                try

                                {

                                    var hw = System.Text.Json.JsonSerializer.Deserialize<TempHomeworkItem>(log.Details);

                                    if (hw != null)

                                    {

                                        string deadlineStr = hw.Deadline.ToString("yyyy-MM-dd HH:mm:ss");

                                        string createdStr = log.Timestamp.ToString("yyyy-MM-dd HH:mm:ss");

                                        

                                        db.Database.ExecuteSqlRaw(

                                            "INSERT INTO Homeworks (Subject, Title, Description, Deadline, CreatedAt, AttachmentPath, ClassId) VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",

                                            hw.Subject ?? "Toán", hw.Title ?? "", hw.Description ?? "", deadlineStr, createdStr, hw.Attachment ?? "", "");

                                    }

                                }

                                catch (Exception exLog)

                                {

                                    Log.Warning("[DbMigrator] Failed to migrate homework log ID {Id}: {Err}", log.Id, exLog.Message);

                                }

                            }

                            Log.Information("[DbMigrator] Migrated old event logs to Homeworks table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to migrate database to v5.41.0");

                        }

                    }



                    // Upgrading to v5.42.0: Create StudentConductHistories table and migrate old conduct scores

                    if (ver < ParseVersion("5.42.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentConductHistories (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                StudentId INTEGER NOT NULL,

                                Semester TEXT NOT NULL DEFAULT '',

                                SchoolYear TEXT NOT NULL DEFAULT '',

                                ConductScore INTEGER NOT NULL DEFAULT 100

                            )");

                            Log.Information("[DbMigrator] Created StudentConductHistories table successfully");



                            // Copy existing student conduct scores to historical records table

                            db.Database.ExecuteSqlRaw(@"

                                INSERT INTO StudentConductHistories (StudentId, Semester, SchoolYear, ConductScore)

                                SELECT Id, 'HK1', '2025-2026', ConductScore FROM Students

                                WHERE Id NOT IN (SELECT StudentId FROM StudentConductHistories WHERE Semester = 'HK1' AND SchoolYear = '2025-2026')

                            ");

                            db.Database.ExecuteSqlRaw(@"

                                INSERT INTO StudentConductHistories (StudentId, Semester, SchoolYear, ConductScore)

                                SELECT Id, 'HK2', '2025-2026', ConductScore FROM Students

                                WHERE Id NOT IN (SELECT StudentId FROM StudentConductHistories WHERE Semester = 'HK2' AND SchoolYear = '2025-2026')

                            ");

                            Log.Information("[DbMigrator] Migrated old student conduct scores successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to migrate database to v5.42.0");

                        }

                    }



                    // Upgrading to v5.43.0: Create database index for StudentConductHistories

                    if (ver < ParseVersion("5.43.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentConductHistories_SchoolYear_Semester ON StudentConductHistories (SchoolYear, Semester);");

                            Log.Information("[DbMigrator] Created database index for StudentConductHistories successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create database index during v5.43.0 migration");

                        }

                    }



                    // Upgrading to v5.44.0: Create LiteratureQuizHistories table and index

                    if (ver < ParseVersion("5.44.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS LiteratureQuizHistories (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                StudentCode TEXT NOT NULL,

                                StudentName TEXT NOT NULL,

                                Grade TEXT NOT NULL,

                                ChapterId TEXT NOT NULL,

                                ChapterName TEXT NOT NULL,

                                Difficulty TEXT NOT NULL DEFAULT 'Mix',

                                TotalQuestions INTEGER NOT NULL,

                                CorrectAnswers INTEGER NOT NULL,

                                TimeSpentSeconds REAL NOT NULL,

                                CompletedAt TEXT NOT NULL,

                                QuizType TEXT NOT NULL DEFAULT 'Practice',

                                Topic TEXT NOT NULL,

                                ProblemResultsJson TEXT NOT NULL DEFAULT '[]'

                            );");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_LiteratureQuizHistories_StudentCode ON LiteratureQuizHistories (StudentCode);");

                            Log.Information("[DbMigrator] Created database table and index for LiteratureQuizHistories successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create LiteratureQuizHistories database table during v5.44.0 migration");

                        }

                    }



                    // Upgrading to v5.45.0: Create StudentMindmaps table

                    if (ver < ParseVersion("5.45.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentMindmaps (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                Title TEXT NOT NULL,

                                DataJson TEXT NOT NULL,

                                Category TEXT,

                                UpdatedAt TEXT NOT NULL,

                                StudentCode TEXT

                            );");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentMindmaps_StudentCode ON StudentMindmaps (StudentCode);");

                            Log.Information("[DbMigrator] Created StudentMindmaps table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create StudentMindmaps table during v5.45.0 migration");

                        }

                    }



                    // Upgrading to v5.46.0: Create Grammar tables

                    if (ver < ParseVersion("5.46.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS GrammarTenses (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                Name TEXT NOT NULL,

                                NameVi TEXT NOT NULL,

                                Structure TEXT NOT NULL,

                                Example TEXT NOT NULL,

                                Signal TEXT NOT NULL,

                                Color TEXT NOT NULL

                            );");



                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS GrammarQuestions (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                Category TEXT NOT NULL,

                                QuestionText TEXT NOT NULL,

                                Hint TEXT NOT NULL,

                                AnswersJson TEXT NOT NULL DEFAULT '[]',

                                OptionsJson TEXT NOT NULL DEFAULT '[]'

                            );");



                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS GrammarQuizHistories (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                StudentCode TEXT NOT NULL,

                                StudentName TEXT NOT NULL,

                                Grade TEXT NOT NULL,

                                QuizType TEXT NOT NULL,

                                CorrectAnswers INTEGER NOT NULL,

                                TotalQuestions INTEGER NOT NULL,

                                TimeSpentSeconds REAL NOT NULL,

                                CompletedAt TEXT NOT NULL

                            );");



                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_GrammarQuizHistories_StudentCode ON GrammarQuizHistories (StudentCode);");

                            

                            Log.Information("[DbMigrator] Created Grammar tables and index successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create Grammar database tables during v5.46.0 migration");

                        }

                    }



                    // Upgrading to v5.47.0: Add Explanation column to Questions table for quiz explanations

                    if (ver < ParseVersion("5.47.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE Questions ADD COLUMN Explanation TEXT NOT NULL DEFAULT ''");

                            Log.Information("[DbMigrator] Added Explanation column to Questions table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to add Explanation column during v5.47.0 migration");

                        }

                    }



                    // Upgrading to v5.48.0: Add IsSynced column to SurveyResponses and indexes for performance

                    if (ver < ParseVersion("5.48.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE SurveyResponses ADD COLUMN IsSynced INTEGER NOT NULL DEFAULT 1");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SurveyResponses_SurveyId ON SurveyResponses(SurveyId)");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SurveyResponses_StudentCode ON SurveyResponses(StudentCode)");

                            Log.Information("[DbMigrator] Added IsSynced column and indexes to SurveyResponses successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade SurveyResponses schema during v5.48.0 migration");

                        }

                    }



                    // Upgrading to v5.49.0: Add IsConfirmed column to StudentGrades table (Phase 5)

                    if (ver < ParseVersion("5.49.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE StudentGrades ADD COLUMN IsConfirmed INTEGER NOT NULL DEFAULT 1");

                            Log.Information("[DbMigrator] Added IsConfirmed column to StudentGrades table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade StudentGrades schema during v5.49.0 migration");

                        }

                    }



                    // Upgrading to v5.50.0: Add VideoUrl column to Questions table (Phase 6)

                    if (ver < ParseVersion("5.50.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE Questions ADD COLUMN VideoUrl TEXT;");

                            Log.Information("[DbMigrator] Added VideoUrl column to Questions table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade Questions schema during v5.50.0 migration");

                        }

                    }



                    // Upgrading to v5.51.0: Add OfflineSyncItems table for built-in offline synchronization support

                    if (ver < ParseVersion("5.51.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS OfflineSyncItems (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                ActionType TEXT NOT NULL,

                                PayloadJson TEXT NOT NULL,

                                CreatedAt TEXT NOT NULL,

                                RetryCount INTEGER DEFAULT 0,

                                Status TEXT NOT NULL DEFAULT 'Pending'

                            );");

                            Log.Information("[DbMigrator] Created OfflineSyncItems table successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to create OfflineSyncItems table during v5.51.0 migration");

                        }

                    }



                    // Upgrading to v5.60.0: Add LessonPlanDrafts, LessonPlanVersions, and PositionX/Y columns in Students (Phase 7)

                    if (ver < ParseVersion("5.60.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS LessonPlanDrafts (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                LessonPlanId INTEGER,

                                TeacherId TEXT,

                                Subject TEXT,

                                Grade TEXT,

                                Title TEXT,

                                Content TEXT,

                                Template TEXT,

                                WeekDate TEXT,

                                Period INTEGER,

                                LastSaved TEXT

                            );");



                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS LessonPlanVersions (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                LessonPlanId INTEGER NOT NULL,

                                VersionNumber INTEGER NOT NULL,

                                Title TEXT,

                                Content TEXT,

                                SavedAt TEXT,

                                Notes TEXT

                            );");



                            if (!ColumnExists(db, "Students", "PositionX"))

                            {

                                RunSafeSql(db, "ALTER TABLE Students ADD COLUMN PositionX REAL DEFAULT 0.0;");

                            }

                            if (!ColumnExists(db, "Students", "PositionY"))

                            {

                                RunSafeSql(db, "ALTER TABLE Students ADD COLUMN PositionY REAL DEFAULT 0.0;");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v5.60.0 successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.60.0 migration");

                        }

                    }



                    // Upgrading to v5.61.0: Add RowHash column to AuditLogs

                    if (ver < ParseVersion("5.61.0"))

                    {

                        try

                        {

                            if (!ColumnExists(db, "AuditLogs", "RowHash"))

                            {

                                RunSafeSql(db, "ALTER TABLE AuditLogs ADD COLUMN RowHash TEXT DEFAULT '';");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v5.61.0 successfully");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.61.0 migration");

                        }

                    }



                    // Upgrading to v5.62.0: Add indexes on AuditLogs for Timestamp and ActorName

                    if (ver < ParseVersion("5.62.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_Timestamp ON AuditLogs(Timestamp);");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_AuditLogs_ActorName ON AuditLogs(ActorName);");

                            Log.Information("[DbMigrator] Upgraded database to v5.62.0 successfully (Added indexes on AuditLogs)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.62.0 migration");

                        }

                    }



                    // Upgrading to v5.63.0: Add Near Future to GrammarTenses and seed Near Future questions / update Q12 hint

                    if (ver < ParseVersion("5.63.0"))

                    {

                        try

                        {

                            var hasNearFuture = db.GrammarTenses.Any(t => t.Name == "Near Future");

                            if (!hasNearFuture)

                            {

                                db.GrammarTenses.Add(new Data.GrammarTense 

                                { 

                                    Name = "Near Future", 

                                    NameVi = "Tương lai gần", 

                                    Structure = "S + am/is/are + going to + V_inf", 

                                    Example = "We are going to buy a new car next week.", 

                                    Signal = "next week, next month, soon, tonight", 

                                    Color = "#EF6C00" 

                                });

                                db.SaveChanges();

                                Log.Information("[DbMigrator] Added Near Future tense to GrammarTenses table");

                            }



                            // Cập nhật gợi ý câu 12

                            var q12 = db.GrammarQuestions.FirstOrDefault(q => q.QuestionText.Contains("not/see") && q.QuestionText.Contains("since Monday"));

                            if (q12 != null && q12.Hint == "Chia động từ thể phủ định")

                            {

                                q12.Hint = "Chia động từ phủ định - Hiện tại hoàn thành";

                                db.SaveChanges();

                                Log.Information("[DbMigrator] Updated question 12 hint in GrammarQuestions table");

                            }



                            // Thêm các câu hỏi mẫu Near Future

                            var hasNearFutureQ1 = db.GrammarQuestions.Any(q => q.QuestionText.Contains("We ___ (buy) a new car next week."));

                            if (!hasNearFutureQ1)

                            {

                                db.GrammarQuestions.Add(new Data.GrammarQuestion 

                                { 

                                    Category = "FillBlank", 

                                    QuestionText = "We ___ (buy) a new car next week.", 

                                    Hint = "Chia động từ - Tương lai gần", 

                                    AnswersJson = "[\"are going to buy\"]",

                                    OptionsJson = "[]"

                                });

                            }



                            var hasNearFutureQ2 = db.GrammarQuestions.Any(q => q.QuestionText.Contains("Look at those black clouds! It ___ (rain) soon."));

                            if (!hasNearFutureQ2)

                            {

                                db.GrammarQuestions.Add(new Data.GrammarQuestion 

                                { 

                                    Category = "FillBlank", 

                                    QuestionText = "Look at those black clouds! It ___ (rain) soon.", 

                                    Hint = "Chia động từ - Tương lai gần", 

                                    AnswersJson = "[\"is going to rain\"]",

                                    OptionsJson = "[]"

                                });

                            }



                            var hasNearFutureQ3 = db.GrammarQuestions.Any(q => q.QuestionText.Contains("They are going to visit their grandparents tonight."));

                            if (!hasNearFutureQ3)

                            {

                                db.GrammarQuestions.Add(new Data.GrammarQuestion 

                                { 

                                    Category = "MultipleChoice", 

                                    QuestionText = "\"They are going to visit their grandparents tonight.\"", 

                                    Hint = "Xác định thì phù hợp", 

                                    AnswersJson = "[\"Tương lai gần\"]", 

                                    OptionsJson = "[\"Tương lai đơn\",\"Tương lai gần\",\"Hiện tại tiếp diễn\",\"Tương lai tiếp diễn\"]" 

                                });

                            }

                            db.SaveChanges();

                            Log.Information("[DbMigrator] Seeded Near Future questions in GrammarQuestions table");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade Grammar tables during v5.63.0 migration");

                        }

                    }



                    // Upgrading to v5.64.0: Add Notes column to InboxMessages

                    if (ver < ParseVersion("5.64.0"))

                    {

                        try

                        {

                            if (!ColumnExists(db, "InboxMessages", "Notes"))

                            {

                                RunSafeSql(db, "ALTER TABLE InboxMessages ADD COLUMN Notes TEXT NOT NULL DEFAULT '';");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v5.64.0 successfully (Added Notes column to InboxMessages)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.64.0 migration");

                        }

                    }



                    // Upgrading to v5.65.0: Create StudentDeviceStatuses and SystemDiagnosticLogs tables

                    if (ver < ParseVersion("5.65.0"))

                    {

                        try

                        {

                            if (!TableExists(db, "StudentDeviceStatuses"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentDeviceStatuses (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    StudentId INTEGER NOT NULL,

                                    DeviceName TEXT NOT NULL DEFAULT '',

                                    Status TEXT NOT NULL DEFAULT 'Offline',

                                    LastHeartbeat TEXT NOT NULL,

                                    CpuUsage REAL NOT NULL,

                                    MemoryUsage REAL NOT NULL,

                                    RunningAppsJson TEXT NOT NULL DEFAULT '',

                                    ActiveWindow TEXT NOT NULL DEFAULT '',

                                    AnomaliesDetected TEXT NOT NULL DEFAULT '',

                                    IsLocked INTEGER NOT NULL DEFAULT 0

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentDeviceStatuses_StudentId ON StudentDeviceStatuses(StudentId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentDeviceStatuses_Status ON StudentDeviceStatuses(Status);");

                            }



                            if (!TableExists(db, "SystemDiagnosticLogs"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS SystemDiagnosticLogs (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    Timestamp TEXT NOT NULL,

                                    DeviceName TEXT NOT NULL DEFAULT '',

                                    StudentId INTEGER NULL,

                                    LogType TEXT NOT NULL DEFAULT 'System',

                                    LogLevel TEXT NOT NULL DEFAULT 'Info',

                                    Message TEXT NOT NULL DEFAULT '',

                                    StackTrace TEXT NOT NULL DEFAULT '',

                                    IsResolved INTEGER NOT NULL DEFAULT 0,

                                    DiagnosticResult TEXT NOT NULL DEFAULT '',

                                    ResolutionAction TEXT NOT NULL DEFAULT ''

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SystemDiagnosticLogs_StudentId ON SystemDiagnosticLogs(StudentId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SystemDiagnosticLogs_Timestamp ON SystemDiagnosticLogs(Timestamp);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SystemDiagnosticLogs_IsResolved ON SystemDiagnosticLogs(IsResolved);");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v5.65.0 successfully (Created StudentDeviceStatuses and SystemDiagnosticLogs tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.65.0 migration");

                        }

                    }



                    // Upgrading to v5.70.0: Create PrincipalOverrideLogs, SensitiveDataAccessAudits, and StudyLoadAdjustments tables

                    if (ver < ParseVersion("5.70.0"))

                    {

                        try

                        {

                            if (!TableExists(db, "PrincipalOverrideLogs"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS PrincipalOverrideLogs (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    PrincipalId TEXT NOT NULL DEFAULT '',

                                    TargetResourceType TEXT NOT NULL DEFAULT '',

                                    TargetResourceId TEXT NOT NULL DEFAULT '',

                                    StudentId INTEGER NOT NULL,

                                    OverrideReason TEXT NOT NULL DEFAULT '',

                                    Timestamp TEXT NOT NULL,

                                    ValidationMethodUsed TEXT NOT NULL DEFAULT '',

                                    IsSuccess INTEGER NOT NULL DEFAULT 1

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_PrincipalOverrideLogs_PrincipalId ON PrincipalOverrideLogs(PrincipalId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_PrincipalOverrideLogs_Timestamp ON PrincipalOverrideLogs(Timestamp);");

                            }



                            if (!TableExists(db, "SensitiveDataAccessAudits"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS SensitiveDataAccessAudits (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    AccessorId TEXT NOT NULL DEFAULT '',

                                    AccessorRole TEXT NOT NULL DEFAULT '',

                                    TargetTable TEXT NOT NULL DEFAULT '',

                                    OperationType TEXT NOT NULL DEFAULT 'Read',

                                    QueryFilter TEXT NOT NULL DEFAULT '',

                                    Timestamp TEXT NOT NULL,

                                    IsAnomalous INTEGER NOT NULL DEFAULT 0,

                                    MitigationActionTaken TEXT NOT NULL DEFAULT 'None'

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SensitiveDataAccessAudits_AccessorId ON SensitiveDataAccessAudits(AccessorId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SensitiveDataAccessAudits_Timestamp ON SensitiveDataAccessAudits(Timestamp);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SensitiveDataAccessAudits_IsAnomalous ON SensitiveDataAccessAudits(IsAnomalous);");

                            }



                            if (!TableExists(db, "StudyLoadAdjustments"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudyLoadAdjustments (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    StudentId INTEGER NOT NULL,

                                    HomeworkReductionRatio REAL NOT NULL,

                                    AllowedLexileOffsetAdjustment INTEGER NOT NULL,

                                    MaxQuizDifficultyAllowed TEXT NOT NULL DEFAULT 'Hard',

                                    TriggerReason TEXT NOT NULL DEFAULT '',

                                    AuthorizedBy TEXT NOT NULL DEFAULT '',

                                    StartDate TEXT NOT NULL,

                                    EndDate TEXT NOT NULL,

                                    Status TEXT NOT NULL DEFAULT 'Active'

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudyLoadAdjustments_StudentId ON StudyLoadAdjustments(StudentId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudyLoadAdjustments_Status ON StudyLoadAdjustments(Status);");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v5.70.0 successfully (Created PrincipalOverrideLogs, SensitiveDataAccessAudits, and StudyLoadAdjustments tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.70.0 migration");

                        }

                    }



                    // Upgrading to v5.75.0: Create CampusBeacons, StudentLocationHistories, GateOfflineKeys, and GateBarrierLogs tables

                    if (ver < ParseVersion("5.75.0"))

                    {

                        try

                        {

                            if (!TableExists(db, "CampusBeacons"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS CampusBeacons (

                                    Id TEXT NOT NULL PRIMARY KEY,

                                    LocationName TEXT NOT NULL DEFAULT '',

                                    AreaZone TEXT NOT NULL DEFAULT 'Safe',

                                    CoordinateX REAL NOT NULL,

                                    CoordinateY REAL NOT NULL,

                                    IsActive INTEGER NOT NULL DEFAULT 1

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_CampusBeacons_IsActive ON CampusBeacons(IsActive);");

                            }



                            if (!TableExists(db, "StudentLocationHistories"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentLocationHistories (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    StudentCode TEXT NOT NULL DEFAULT '',

                                    CurrentZone TEXT NOT NULL DEFAULT '',

                                    NearbyBeaconId TEXT NOT NULL DEFAULT '',

                                    CalculatedX REAL NOT NULL,

                                    CalculatedY REAL NOT NULL,

                                    Rssi REAL NOT NULL,

                                    Timestamp TEXT NOT NULL

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentLocationHistories_StudentCode ON StudentLocationHistories(StudentCode);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentLocationHistories_Timestamp ON StudentLocationHistories(Timestamp);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentLocationHistories_NearbyBeaconId ON StudentLocationHistories(NearbyBeaconId);");

                            }



                            if (!TableExists(db, "GateOfflineKeys"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS GateOfflineKeys (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    KeyName TEXT NOT NULL DEFAULT 'School_Default_Public',

                                    PublicKeyData TEXT NOT NULL DEFAULT '',

                                    CreatedAt TEXT NOT NULL,

                                    IsActive INTEGER NOT NULL DEFAULT 1

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_GateOfflineKeys_IsActive ON GateOfflineKeys(IsActive);");

                            }



                            if (!TableExists(db, "GateBarrierLogs"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS GateBarrierLogs (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    StudentCode TEXT NOT NULL DEFAULT '',

                                    GateId TEXT NOT NULL DEFAULT 'MAIN_GATE_01',

                                    CommandAction TEXT NOT NULL DEFAULT 'Open',

                                    TriggerSource TEXT NOT NULL DEFAULT 'QR_LeavePass',

                                    Timestamp TEXT NOT NULL,

                                    HardwareConfirmed INTEGER NOT NULL DEFAULT 0

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_GateBarrierLogs_StudentCode ON GateBarrierLogs(StudentCode);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_GateBarrierLogs_Timestamp ON GateBarrierLogs(Timestamp);");

                            }



                            Log.Information("[DbMigrator] Upgraded database to v5.75.0 successfully (Created CampusBeacons, StudentLocationHistories, GateOfflineKeys, and GateBarrierLogs tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.75.0 migration");

                        }

                    }



                    // Upgrading to v5.80.0: Create SubstituteAssignments, ChemicalRequisitions, StaffPayrollLedgers, and MaintenanceTickets tables

                    if (ver < ParseVersion("5.80.0"))

                    {

                        try

                        {

                            if (!TableExists(db, "SubstituteAssignments"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS SubstituteAssignments (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    LeaveRequestId INTEGER NOT NULL,

                                    OriginalTeacherCode TEXT NOT NULL DEFAULT '',

                                    SubstituteTeacherCode TEXT NOT NULL DEFAULT '',

                                    Date TEXT NOT NULL,

                                    PeriodIndex INTEGER NOT NULL,

                                    ClassName TEXT NOT NULL DEFAULT '',

                                    Status TEXT NOT NULL DEFAULT 'Assigned',

                                    RoomName TEXT NOT NULL DEFAULT ''

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SubstituteAssignments_SubstituteTeacherCode ON SubstituteAssignments(SubstituteTeacherCode);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SubstituteAssignments_Date ON SubstituteAssignments(Date);");

                            }



                            if (!TableExists(db, "ChemicalRequisitions"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS ChemicalRequisitions (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    AssetBookingId INTEGER NOT NULL,

                                    ChemicalName TEXT NOT NULL DEFAULT '',

                                    RequiredQuantity REAL NOT NULL,

                                    IsHazardous INTEGER NOT NULL DEFAULT 0,

                                    ApprovedByHOD TEXT NOT NULL DEFAULT '',

                                    ApprovedByPrincipal TEXT NOT NULL DEFAULT '',

                                    Status TEXT NOT NULL DEFAULT 'Pending',

                                    SafetyNotes TEXT NOT NULL DEFAULT ''

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_ChemicalRequisitions_AssetBookingId ON ChemicalRequisitions(AssetBookingId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_ChemicalRequisitions_ChemicalName ON ChemicalRequisitions(ChemicalName);");

                            }



                            if (!TableExists(db, "StaffPayrollLedgers"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StaffPayrollLedgers (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    StaffCode TEXT NOT NULL DEFAULT '',

                                    MonthYear TEXT NOT NULL DEFAULT '',

                                    BaseSalary REAL NOT NULL,

                                    OvertimeHours REAL NOT NULL,

                                    OvertimeRate REAL NOT NULL,

                                    TeachingBonus REAL NOT NULL,

                                    FinalAmount REAL NOT NULL,

                                    LedgerChecksum TEXT NOT NULL DEFAULT '',

                                    IsLocked INTEGER NOT NULL DEFAULT 0,

                                    CalculatedAt TEXT NOT NULL

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StaffPayrollLedgers_StaffCode ON StaffPayrollLedgers(StaffCode);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StaffPayrollLedgers_MonthYear ON StaffPayrollLedgers(MonthYear);");

                            }



                            if (!TableExists(db, "MaintenanceTickets"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS MaintenanceTickets (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    RoomName TEXT NOT NULL DEFAULT '',

                                    FacilityName TEXT NOT NULL DEFAULT '',

                                    Description TEXT NOT NULL DEFAULT '',

                                    Severity TEXT NOT NULL DEFAULT 'Medium',

                                    Status TEXT NOT NULL DEFAULT 'Pending',

                                    AssignedStaffCode TEXT NOT NULL DEFAULT '',

                                    CreatedAt TEXT NOT NULL,

                                    ResolvedAt TEXT,

                                    IsEscalated INTEGER NOT NULL DEFAULT 0,

                                    ResolutionNotes TEXT NOT NULL DEFAULT ''

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_MaintenanceTickets_Status ON MaintenanceTickets(Status);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_MaintenanceTickets_Severity ON MaintenanceTickets(Severity);");

                            }



                            Log.Information("[DbMigrator] Upgraded database to v5.80.0 successfully (Created SubstituteAssignments, ChemicalRequisitions, StaffPayrollLedgers, and MaintenanceTickets tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.80.0 migration");

                        }

                    }



                    if (ver < ParseVersion("5.90.0"))

                    {

                        try

                        {

                            if (!TableExists(db, "SecurityLockdownLogs"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS SecurityLockdownLogs (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    TriggeredByStaffCode TEXT NOT NULL DEFAULT '',

                                    StartedAt TEXT NOT NULL,

                                    EndedAt TEXT,

                                    LockdownType TEXT NOT NULL DEFAULT '',

                                    Status TEXT NOT NULL DEFAULT 'Active',

                                    ResolutionNotes TEXT NOT NULL DEFAULT ''

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SecurityLockdownLogs_TriggeredByStaffCode ON SecurityLockdownLogs(TriggeredByStaffCode);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SecurityLockdownLogs_Status ON SecurityLockdownLogs(Status);");

                            }



                            if (!TableExists(db, "KitchenColdStorageLogs"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS KitchenColdStorageLogs (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    FridgeId TEXT NOT NULL DEFAULT '',

                                    Temperature REAL NOT NULL,

                                    Humidity REAL NOT NULL,

                                    Timestamp TEXT NOT NULL,

                                    IsViolation INTEGER NOT NULL DEFAULT 0

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_KitchenColdStorageLogs_FridgeId ON KitchenColdStorageLogs(FridgeId);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_KitchenColdStorageLogs_Timestamp ON KitchenColdStorageLogs(Timestamp);");

                            }



                            if (!TableExists(db, "StudentCarePlans"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS StudentCarePlans (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    StudentCode TEXT NOT NULL DEFAULT '',

                                    ChronicCondition TEXT NOT NULL DEFAULT '',

                                    MedicalTriggers TEXT NOT NULL DEFAULT '',

                                    EmergencyMeasures TEXT NOT NULL DEFAULT '',

                                    IsActive INTEGER NOT NULL DEFAULT 1,

                                    CreatedAt TEXT NOT NULL

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentCarePlans_StudentCode ON StudentCarePlans(StudentCode);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_StudentCarePlans_IsActive ON StudentCarePlans(IsActive);");

                            }



                            if (!TableExists(db, "ResourceWasteLedgers"))

                            {

                                RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS ResourceWasteLedgers (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    Date TEXT NOT NULL,

                                    JanitorStaffCode TEXT NOT NULL DEFAULT '',

                                    OrganicKg REAL NOT NULL,

                                    RecyclableKg REAL NOT NULL,

                                    NonRecyclableKg REAL NOT NULL,

                                    HazardousKg REAL NOT NULL,

                                    ElectricityKwh REAL NOT NULL,

                                    WaterCubicMeters REAL NOT NULL,

                                    RecordedAt TEXT NOT NULL

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_ResourceWasteLedgers_Date ON ResourceWasteLedgers(Date);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_ResourceWasteLedgers_JanitorStaffCode ON ResourceWasteLedgers(JanitorStaffCode);");

                            }



                            Log.Information("[DbMigrator] Upgraded database to v5.90.0 successfully (Created SecurityLockdownLogs, KitchenColdStorageLogs, StudentCarePlans, and ResourceWasteLedgers tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.90.0 migration");

                        }

                    }



                    if (ver < ParseVersion("5.95.0"))

                    {

                        try

                        {

                            if (!ColumnExists(db, "Bulletins", "Category"))

                            {

                                RunSafeSql(db, "ALTER TABLE Bulletins ADD COLUMN Category TEXT NOT NULL DEFAULT 'School';");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_Bulletins_Category ON Bulletins(Category);");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v5.95.0 successfully (Added Category to Bulletins)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.95.0 migration");

                        }

                    }



                    if (ver < ParseVersion("5.99.0"))

                    {

                        try

                        {

                            if (!TableExists(db, "BulletinApprovalRequests"))

                            {

                                RunSafeSql(db, @"CREATE TABLE BulletinApprovalRequests (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    BulletinId INTEGER NOT NULL,

                                    ApproverId TEXT NOT NULL,

                                    Step INTEGER NOT NULL,

                                    Status TEXT NOT NULL DEFAULT 'Pending',

                                    Comment TEXT NOT NULL DEFAULT '',

                                    ActionedAt TEXT NULL,

                                    FOREIGN KEY (BulletinId) REFERENCES Bulletins(Id) ON DELETE CASCADE

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_BulletinApprovalRequests_BulletinId ON BulletinApprovalRequests(BulletinId);");

                            }



                            if (!TableExists(db, "BulletinReadReceipts"))

                            {

                                RunSafeSql(db, @"CREATE TABLE BulletinReadReceipts (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    BulletinId INTEGER NOT NULL,

                                    UserId TEXT NOT NULL,

                                    ReadAt TEXT NOT NULL,

                                    FOREIGN KEY (BulletinId) REFERENCES Bulletins(Id) ON DELETE CASCADE

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_BulletinReadReceipts_BulletinId ON BulletinReadReceipts(BulletinId);");

                            }



                            if (!TableExists(db, "BulletinPollOptions"))

                            {

                                RunSafeSql(db, @"CREATE TABLE BulletinPollOptions (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    BulletinId INTEGER NOT NULL,

                                    OptionText TEXT NOT NULL,

                                    VotesCount INTEGER NOT NULL DEFAULT 0,

                                    FOREIGN KEY (BulletinId) REFERENCES Bulletins(Id) ON DELETE CASCADE

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_BulletinPollOptions_BulletinId ON BulletinPollOptions(BulletinId);");

                            }



                            if (!TableExists(db, "BulletinPollVotes"))

                            {

                                RunSafeSql(db, @"CREATE TABLE BulletinPollVotes (

                                    Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                    OptionId INTEGER NOT NULL,

                                    UserId TEXT NOT NULL,

                                    VotedAt TEXT NOT NULL,

                                    FOREIGN KEY (OptionId) REFERENCES BulletinPollOptions(Id) ON DELETE CASCADE

                                );");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_BulletinPollVotes_OptionId ON BulletinPollVotes(OptionId);");

                            }



                            Log.Information("[DbMigrator] Upgraded database to v5.99.0 successfully (Created BulletinApprovalRequests, ReadReceipts, PollOptions, PollVotes tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v5.99.0 migration");

                        }

                    }



                    if (ver < ParseVersion("6.00.0"))

                    {

                        try

                        {

                            if (!ColumnExists(db, "DailyTasks", "TimeFrame"))

                            {

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN TimeFrame TEXT NOT NULL DEFAULT '';");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN Collaborators TEXT NOT NULL DEFAULT '';");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN CollaboratorIds TEXT NOT NULL DEFAULT '';");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN Location TEXT NOT NULL DEFAULT '';");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN Description TEXT NOT NULL DEFAULT '';");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN IsCritical INTEGER NOT NULL DEFAULT 0;");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN StartTime TEXT NULL;");

                                RunSafeSql(db, "ALTER TABLE DailyTasks ADD COLUMN EndTime TEXT NULL;");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_DailyTasks_AssignedTo ON DailyTasks(AssignedTo);");

                                RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_DailyTasks_DueDate ON DailyTasks(DueDate);");

                            }

                            Log.Information("[DbMigrator] Upgraded database to v6.00.0 successfully (Extended DailyTasks fields)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v6.00.0 migration");

                        }

                    }



                    if (ver < ParseVersion("6.01.0"))

                    {

                        try

                        {

                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS LibraryBooks (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                BookCode TEXT NOT NULL UNIQUE,

                                Title TEXT NOT NULL,

                                Subject TEXT NOT NULL,

                                BookLexileLevel INTEGER DEFAULT 0,

                                BookConditionScore INTEGER DEFAULT 10,

                                Status TEXT NOT NULL DEFAULT 'Active',

                                ReplenishmentRequired INTEGER DEFAULT 0

                            );");



                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS BookReservations (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                BookCode TEXT NOT NULL,

                                StudentId INTEGER NOT NULL,

                                ReservedDate TEXT NOT NULL,

                                ExpiryDate TEXT NOT NULL,

                                Status TEXT NOT NULL DEFAULT 'Pending',

                                FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE RESTRICT

                            );");



                            RunSafeSql(db, @"CREATE TABLE IF NOT EXISTS BookLoans (

                                Id INTEGER PRIMARY KEY AUTOINCREMENT,

                                BookCode TEXT NOT NULL,

                                BookTitle TEXT NOT NULL,

                                StudentId INTEGER NOT NULL,

                                StudentName TEXT NOT NULL,

                                LoanedDate TEXT NOT NULL,

                                DueDate TEXT NOT NULL,

                                ReturnedDate TEXT,

                                ReturnConditionScore INTEGER,

                                FineAmount REAL DEFAULT 0,

                                Status TEXT NOT NULL DEFAULT 'Active',

                                FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE RESTRICT

                            );");



                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_LibraryBooks_BookCode ON LibraryBooks (BookCode);");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_BookLoans_StudentId ON BookLoans (StudentId);");

                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_BookReservations_StudentId ON BookReservations (StudentId);");

                            

                            Log.Information("[DbMigrator] Upgraded database to v6.01.0 successfully (Created physical Library tables)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v6.01.0 migration");

                        }

                    }



                    // Upgrading to v6.02.0: Upgrade SecurityLogs structure and indexing for Visitors

                    if (ver < ParseVersion("6.02.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN CccdNumber TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN CccdHash TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN VisitorName TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN Gender TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN Address TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN HostTeacherId TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN VisitPurpose TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN BadgeNumber TEXT DEFAULT '';");

                            RunSafeSql(db, "ALTER TABLE SecurityLogs ADD COLUMN IsCheckedOut INTEGER NOT NULL DEFAULT 0;");



                            RunSafeSql(db, "CREATE INDEX IF NOT EXISTS IX_SecurityLogs_CccdHash ON SecurityLogs(CccdHash);");



                            // Migrate old security logs

                            var oldLogs = db.SecurityLogs.ToList();

                            foreach (var log in oldLogs)

                            {

                                if (string.IsNullOrEmpty(log.Description)) continue;

                                

                                var desc = log.Description;

                                string name = "";

                                string cccd = "";

                                string gender = "";

                                string address = "";

                                

                                var match = System.Text.RegularExpressions.Regex.Match(desc, @"Khách:\s*(.*?)\s*-\s*CCCD:\s*(.*?)\s*-\s*Giới tính:\s*(.*?)\s*-\s*Địa chỉ:\s*(.*)");

                                if (match.Success)

                                {

                                    name = match.Groups[1].Value.Trim();

                                    cccd = match.Groups[2].Value.Trim();

                                    gender = match.Groups[3].Value.Trim();

                                    address = match.Groups[4].Value.Trim();

                                }

                                else

                                {

                                    var matchName = System.Text.RegularExpressions.Regex.Match(desc, @"Khách:\s*([^-]+)");

                                    var matchCccd = System.Text.RegularExpressions.Regex.Match(desc, @"CCCD:\s*([^-]+)");

                                    var matchGender = System.Text.RegularExpressions.Regex.Match(desc, @"Giới tính:\s*([^-]+)");

                                    var matchAddress = System.Text.RegularExpressions.Regex.Match(desc, @"Địa chỉ:\s*(.*)");

                                    

                                    if (matchName.Success) name = matchName.Groups[1].Value.Trim();

                                    if (matchCccd.Success) cccd = matchCccd.Groups[1].Value.Trim();

                                    if (matchGender.Success) gender = matchGender.Groups[1].Value.Trim();

                                    if (matchAddress.Success) address = matchAddress.Groups[1].Value.Trim();

                                }



                                if (!string.IsNullOrEmpty(cccd) || !string.IsNullOrEmpty(name))

                                {

                                    log.VisitorName = string.IsNullOrEmpty(name) ? log.PersonInvolved : name;

                                    log.CccdNumber = cccd;

                                    log.Gender = gender;

                                    log.Address = address;

                                    

                                    if (!string.IsNullOrEmpty(cccd))

                                    {

                                        log.CccdHash = QASmartClass.Utilities.CryptoHelper.ComputeHMAC(cccd);

                                    }

                                }

                                else

                                {

                                    log.VisitorName = "Khách vãng lai";

                                }

                            }

                            db.SaveChanges();

                            Log.Information("[DbMigrator] Upgraded database to v6.02.0 successfully (Visitor fields added to SecurityLogs)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v6.02.0 migration");

                        }

                    }



                    // Upgrading to v6.03.0: Add ApprovalStatus to MobileTokens

                    if (ver < ParseVersion("6.03.0"))

                    {

                        try

                        {

                            RunSafeSql(db, "ALTER TABLE MobileTokens ADD COLUMN ApprovalStatus TEXT DEFAULT 'Approved';");

                            Log.Information("[DbMigrator] Upgraded database to v6.03.0 successfully (Added ApprovalStatus to MobileTokens)");

                        }

                        catch (Exception ex)

                        {

                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v6.03.0 migration");

                        }

                    }

                    // Upgrading to v6.04.0: Aligned 11A1 lessons data to Grade 11 and Ensure Sample PDFs exist
                    if (ver < ParseVersion("6.04.0"))
                    {
                        try
                        {
                            // Aligned 11A1 lessons data to Grade 11
                            // 1. TOÁN (Lesson Id = 19): Toán 12 -> Toán 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'C1.1 — Hàm số lượng giác và đồ thị' WHERE Id = 19;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 MỤC TIÊU BÀI HỌC\n• Nắm vững định nghĩa và tập xác định của các hàm số lượng giác.\n• Xác định tính tuần hoàn, tính chẵn lẻ và vẽ đồ thị các hàm số sin, cos, tan, cot.\n• Áp dụng hàm số lượng giác giải quyết các bài toán chu kỳ thực tế.' WHERE LessonId = 19 AND ContentType = 'Text' AND Data LIKE '%MỤC TIÊU BÀI HỌC%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_Toan_lop_11_Ham_so_luong_giac.pdf' WHERE LessonId = 19 AND ContentType = 'PDF';");

                            // 2. VẬT LÝ (Lesson Id = 20): Vật lý 10 -> Vật lý 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'C2.1 — Lực thế và thế năng điện' WHERE Id = 20;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 MỤC TIÊU BÀI HỌC\n• Hiểu định nghĩa công của lực điện trường và tính chất thế năng của nó.\n• Tính toán công của lực điện trong trường đều và thế năng điện tích.\n• Ứng dụng điện trường trong công nghệ (máy lọc bụi tĩnh điện).' WHERE LessonId = 20 AND ContentType = 'Text' AND Data LIKE '%MỤC TIÊU BÀI HỌC%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_Vat_ly_lop_11_The_nang_dien.pdf' WHERE LessonId = 20 AND ContentType = 'PDF';");

                            // 3. NGỮ VĂN (Lesson Id = 22): Ngữ văn 12 -> Ngữ văn 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'C2.2 — Chữ người tử tù - Nguyễn Tuân' WHERE Id = 22;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 MỤC TIÊU BÀI HỌC\n• Phân tích vẻ đẹp hình tượng nhân vật Huấn Cao hào hoa, khí phách.\n• Hiểu rõ tấm lòng biệt nhỡn liên tài và tính cách của viên quản ngục.\n• Cảm nhận cảnh cho chữ - một cảnh tượng xưa nay chưa từng có.' WHERE LessonId = 22 AND ContentType = 'Text' AND Data LIKE '%MỤC TIÊU BÀI HỌC%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_Ngu_van_lop_11_Chu_nguoi_tu_tu.pdf' WHERE LessonId = 22 AND ContentType = 'PDF';");

                            // 4. TIẾNG ANH (Lesson Id = 23): Tiếng Anh 10 -> Tiếng Anh 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'Unit 1 — Generation Gap & Grammar in Context' WHERE Id = 23;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 LEARNING OBJECTIVES\n• Learn key vocabulary regarding family conflicts and generation gaps.\n• Master the usage of modal verbs (must, have to, should) in context.\n• Discuss and present solutions for domestic generation differences.' WHERE LessonId = 23 AND ContentType = 'Text' AND Data LIKE '%LEARNING OBJECTIVES%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_Tieng_Anh_lop_11_Generation_Gap.pdf' WHERE LessonId = 23 AND ContentType = 'PDF';");

                            // 5. SINH HỌC (Lesson Id = 24): Sinh học 12 -> Sinh học 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'C2.3 — Khái quát về cảm ứng ở sinh vật' WHERE Id = 24;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 MỤC TIÊU BÀI HỌC\n• Định nghĩa cảm ứng và nêu ví dụ thực tế ở thực vật và động vật.\n• Giải thích cơ chế hướng động và ứng động ở thực vật.\n• Ý nghĩa của cảm ứng đối với sự sinh trưởng và thích nghi của sinh vật.' WHERE LessonId = 24 AND ContentType = 'Text' AND Data LIKE '%MỤC TIÊU BÀI HỌC%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_Sinh_hoc_lop_11_Cam_ung.pdf' WHERE LessonId = 24 AND ContentType = 'PDF';");

                            // 6. ĐỊA LÝ (Lesson Id = 26): Địa lý 10 -> Địa lý 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'C3.3 — Vị trí địa lý và điều kiện tự nhiên Đông Nam Á' WHERE Id = 26;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 MỤC TIÊU BÀI HỌC\n• Xác định vị trí địa lý, phạm vi lãnh thổ khu vực Đông Nam Á.\n• Phân tích đặc điểm tự nhiên và tài nguyên thiên nhiên của khu vực.\n• Đánh giá thuận lợi và khó khăn đối với phát triển kinh tế xã hội.' WHERE LessonId = 26 AND ContentType = 'Text' AND Data LIKE '%MỤC TIÊU BÀI HỌC%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_Dia_ly_lop_11_Dong_Nam_A.pdf' WHERE LessonId = 26 AND ContentType = 'PDF';");

                            // 7. GDCD (Lesson Id = 28): GDCD 12 -> GDCD 11
                            RunSafeSql(db, "UPDATE Lessons SET Grade = 'Lớp 11', Title = 'Bài 1 — Công dân với sự phát triển kinh tế' WHERE Id = 28;");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = '📌 MỤC TIÊU BÀI HỌC\n• Hiểu rõ vai trò của sản xuất của cải vật chất đối với sự phát triển xã hội.\n• Phân tích các yếu tố cấu thành quá trình sản xuất (sức lao động, đối tượng...).\n• Nâng cao trách nhiệm công dân trong học tập đóng góp phát triển kinh tế.' WHERE LessonId = 28 AND ContentType = 'Text' AND Data LIKE '%MỤC TIÊU BÀI HỌC%';");
                            RunSafeSql(db, "UPDATE LessonContents SET Data = 'C:\\Users\\DELL\\Documents\\QA SmartClass\\Library\\Bài giảng\\Bai_giang_GDCD_lop_11_Kinh_te.pdf' WHERE LessonId = 28 AND ContentType = 'PDF';");

                            EnsureGrade11SamplePdfsExist();

                            // Upgrade to v6.05.0: SchoolAsset Coordinates migration
                            if (!ColumnExists(db, "SchoolAssets", "PositionX"))
                            {
                                RunSafeSql(db, "ALTER TABLE SchoolAssets ADD COLUMN PositionX REAL DEFAULT 0.0;");
                            }
                            if (!ColumnExists(db, "SchoolAssets", "PositionY"))
                            {
                                RunSafeSql(db, "ALTER TABLE SchoolAssets ADD COLUMN PositionY REAL DEFAULT 0.0;");
                            }

                            Log.Information("[DbMigrator] Upgraded database to v6.04.0 successfully (Aligned 11A1 lessons data to Grade 11 and Ensure Sample PDFs exist)");
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "[DbMigrator] Failed to upgrade database schema during v6.04.0 migration");
                        }
                    }




                    // v6.05.0: Thêm các cột SmartLibrary cho bảng Students

                    if (ver < ParseVersion("6.05.0"))

                    {

                        RunSafeSql(db, "ALTER TABLE Students ADD COLUMN StudentLexileScore INTEGER NOT NULL DEFAULT 400");

                        RunSafeSql(db, "ALTER TABLE Students ADD COLUMN ReservationBlockedUntil TEXT NULL");

                        RunSafeSql(db, "ALTER TABLE Students ADD COLUMN ConsecutiveSuccessfulPickups INTEGER NOT NULL DEFAULT 0");

                        Log.Information("[DbMigrator] Applied migration to v6.05.0 (SmartLibrary student columns)");

                    }



                    // Seed default GradeTypeMasters if empty

                    SeedDefaultGradeTypes(db);

                    SeedDefaultRolePermissions(db);

                    SeedDefaultHomeworks(db);

                    SeedDefaultGrammar(db);

                    SeedMasterSettings(db);



                    transaction.Commit();



                    // Ghi version mới

                    SaveVersion(versionFile, targetVersion);

                    Log.Information("[DbMigrator] Migration complete → v{Ver}", targetVersion);

                }

                catch (Exception ex)

                {

                    try

                    {

                        transaction.Rollback();

                    }

                    catch (Exception rollbackEx)

                    {

                        Log.Error(rollbackEx, "[DbMigrator] Transaction rollback failed");

                    }



                    Log.Error(ex, "[DbMigrator] Migration failed! Falling back to EnsureCreated");

                    // Fallback an toàn: tạo các bảng thiếu mà không xóa bảng cũ

                    db.Database.EnsureCreated();

                    SeedDefaultGradeTypes(db);

                    SeedDefaultRolePermissions(db);

                    SeedDefaultHomeworks(db);

                    SeedDefaultGrammar(db);

                    SeedMasterSettings(db);

                    SaveVersion(versionFile, targetVersion);

                }

            }

        }



        private static void SeedMasterSettings(Data.AppDbContext db)

        {

            try

            {

                bool hasChanged = false;

                if (!db.SystemSettings.Any(s => s.Id == "Broadcast_UdpHeartbeatTimeout"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Broadcast_UdpHeartbeatTimeout", 

                        Value = "10", 

                        Category = "Broadcast", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Broadcast_EnableScreenExclusion"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Broadcast_EnableScreenExclusion", 

                        Value = "true", 

                        Category = "Broadcast", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Library_DamagedBookBlockMode"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Library_DamagedBookBlockMode", 

                        Value = "0", 

                        Category = "Library", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Library_DamagedBookBlockDays"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Library_DamagedBookBlockDays", 

                        Value = "3", 

                        Category = "Library", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Library_DamagedBookConductDeduction"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Library_DamagedBookConductDeduction", 

                        Value = "5", 

                        Category = "Library", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_OfflineVerificationStrictness"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Security_OfflineVerificationStrictness", 

                        Value = "1", 

                        Category = "IT", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_ComPortCheckingMode"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Security_ComPortCheckingMode", 

                        Value = "1", 

                        Category = "IT", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Kitchen_MealCalculationFormula"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Kitchen_MealCalculationFormula", 

                        Value = "1", 

                        Category = "IT", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Kitchen_EnableColdStorageWidget"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "Kitchen_EnableColdStorageWidget", 

                        Value = "true", 

                        Category = "IT", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Bulletin_StrictCategorySafetyMode"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting 

                    { 

                        Id = "IT_Bulletin_StrictCategorySafetyMode", 

                        Value = "1", 

                        Category = "Bulletin", 

                        LastUpdated = DateTime.Now 

                    });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_AuthLevel"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_AuthLevel", Value = "High", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_Encryption"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_Encryption", Value = "True", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_Notification"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_Notification", Value = "InApp", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_BlinkingStyle"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_BlinkingStyle", Value = "Pulse", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_AutoReleaseMode"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_AutoReleaseMode", Value = "DailyReset", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_PreRegistration"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_PreRegistration", Value = "Enabled", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_Visitor_PrintBadge"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_Visitor_PrintBadge", Value = "Disabled", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_PreRegistration_Portal"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_PreRegistration_Portal", Value = "False", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_HostNotification_Enabled"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_HostNotification_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_WayfindingMap_Enabled"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_WayfindingMap_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_SelfCheckOut_Enabled"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_SelfCheckOut_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Security_SentimentSurvey_Enabled"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Security_SentimentSurvey_Enabled", Value = "False", Category = "Security", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_System_DeploymentModel"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_System_DeploymentModel", Value = "Model_B_Standard", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Database_WALMode"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Database_WALMode", Value = "Enabled", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Sync_Interval"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Sync_Interval", Value = "5", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_UI_GraphicsLevel"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_UI_GraphicsLevel", Value = "DynamicSVG", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Mobile_AutoApprove"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Mobile_AutoApprove", Value = "Enabled", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Asset_MaintenanceIntervalMonths"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Asset_MaintenanceIntervalMonths", Value = "6", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "Asset_AutoCancelBookingsOnRepair"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "Asset_AutoCancelBookingsOnRepair", Value = "1", Category = "Database", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Document_DefaultSLADays"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Document_DefaultSLADays", Value = "3", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Incident_AutoNotifyParents"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Incident_AutoNotifyParents", Value = "Enabled", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (!db.SystemSettings.Any(s => s.Id == "IT_Audit_IntegrityCheckMode"))

                {

                    db.SystemSettings.Add(new Data.SystemSetting { Id = "IT_Audit_IntegrityCheckMode", Value = "Strict", Category = "IT", LastUpdated = DateTime.Now });

                    hasChanged = true;

                }

                if (hasChanged)

                {

                    db.SaveChanges();

                    Log.Information("[DbMigrator] Seeded default Master Settings (UDP Heartbeat, Screen Exclusion, Library, Security, Visitor, and IT Mobile settings)");

                }

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Failed to seed default Master Settings: {Err}", ex.Message);

            }

        }



        private static void SeedDefaultGradeTypes(Data.AppDbContext db)

        {

            try

            {

                if (!db.GradeTypeMasters.Any())

                {

                    db.GradeTypeMasters.AddRange(

                        new Data.GradeTypeMaster { Code = "TX", DisplayName = "Thường xuyên", ShortName = "15p", Weight = 1, MaxAttempts = 2, SortOrder = 1, IsActive = true, Notes = "Tự động khởi tạo" },

                        new Data.GradeTypeMaster { Code = "GK", DisplayName = "Giữa kỳ", ShortName = "GK", Weight = 2, MaxAttempts = 1, SortOrder = 2, IsActive = true, Notes = "Tự động khởi tạo" },

                        new Data.GradeTypeMaster { Code = "CK", DisplayName = "Cuối kỳ", ShortName = "CK", Weight = 3, MaxAttempts = 1, SortOrder = 3, IsActive = true, Notes = "Tự động khởi tạo" }

                    );

                    db.SaveChanges();

                    Log.Information("[DbMigrator] Seeded default GradeTypeMasters");

                }

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Failed to seed default GradeTypeMasters: {Err}", ex.Message);

            }

        }



        private static void SeedDefaultRolePermissions(Data.AppDbContext db)

        {

            try

            {

                var hpPermissions = new[] { 
                    "leave_request", "Incidents", "event_calendar", "department_mgmt", "award_mgmt", 
                    "staff_performance", "task_management", "eoffice_routing", "counseling", "health", 
                    "epidemic", "food_safety", "medical_inventory", "emergency", "Gate", "Canteen", 
                    "security", "janitor", "kitchen", "document_manager", "library", "mobile_app", 
                    "app_analytics", "hr_profile", "hr_leave", "hr_attendance", "hr_contract",
                    "youth_dashboard", "youth_members", "youth_recruitment", "youth_attendance", 
                    "youth_activities", "youth_eventreg", "youth_emulation", "youth_award", 
                    "youth_voting", "youth_document", "youth_planbudget", "youth_fee"
                };

                // Hiệu trưởng có 100% tất cả các quyền của Hiệu phó, cộng thêm các quyền chiến lược vĩ mô
                var htPermissions = hpPermissions.Concat(new[] { 
                    "Overview", "moet", "reports", "payroll", "tuition", "assets", "push_notification", "data_exporter" 
                }).Distinct().ToArray();

                var defaultPermissions = new System.Collections.Generic.Dictionary<string, string[]>
                {
                    ["BaoVe"] = new[] { "Gate", "security", "emergency" },
                    ["YTe"] = new[] { "counseling", "health", "epidemic", "food_safety", "medical_inventory", "emergency" },
                    ["LaoCong"] = new[] { "janitor" },
                    ["Bep"] = new[] { "Canteen", "kitchen", "food_safety" },
                    ["Ketoan"] = new[] { "Canteen", "assets", "tuition", "payroll", "reports", "youth_fee", "youth_planbudget" },
                    ["ThuQuy"] = new[] { "Canteen", "tuition", "payroll", "youth_fee", "youth_planbudget" },
                    ["Counselor"] = new[] { "counseling" },
                    ["Librarian"] = new[] { "library", "document_manager", "reports" },
                    ["GV"] = new[] { "event_calendar", "department_mgmt", "award_mgmt", "leave_request", "Incidents", "counseling" },
                    ["HieuPho"] = hpPermissions,
                    ["HieuTruong"] = htPermissions
                };

                bool changed = false;
                foreach (var kvp in defaultPermissions)
                {
                    var role = kvp.Key;
                    foreach (var tag in kvp.Value)
                    {
                        bool exists = db.RolePermissions.Any(rp => rp.Role == role && rp.PermissionTag == tag);
                        if (!exists)
                        {
                            db.RolePermissions.Add(new Data.RolePermission
                            {
                                Role = role,
                                PermissionTag = tag
                            });
                            changed = true;
                        }
                    }
                }

                // Loại bỏ khỏi HieuPho các quyền chuyên biệt của Hiệu trưởng/Admin
                var forbiddenForHp = new[] { "moet", "payroll", "system_settings", "role_manager", "backup_restore", "system_audit" };
                var invalidHpPerms = db.RolePermissions.Where(rp => rp.Role == "HieuPho" && forbiddenForHp.Contains(rp.PermissionTag)).ToList();
                if (invalidHpPerms.Any())
                {
                    db.RolePermissions.RemoveRange(invalidHpPerms);
                    changed = true;
                }

                // Loại bỏ triệt để các quyền quản trị/vĩ mô nếu từng lỡ gán cho GV
                var forbiddenForGv = new[] { "Overview", "moet", "reports", "app_analytics", "system_settings", "role_manager", "backup_restore", "system_audit", "payroll", "tuition", "assets", "Gate", "security", "janitor", "kitchen", "medical_inventory", "push_notification" };
                var invalidGvPerms = db.RolePermissions.Where(rp => rp.Role == "GV" && forbiddenForGv.Contains(rp.PermissionTag)).ToList();
                if (invalidGvPerms.Any())
                {
                    db.RolePermissions.RemoveRange(invalidGvPerms);
                    changed = true;
                }



                if (changed)

                {

                    db.SaveChanges();

                    Log.Information("[DbMigrator] Synchronized default role permissions");

                }

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Failed to seed default role permissions: {Err}", ex.Message);

            }

        }



        private static void SeedDefaultHomeworks(Data.AppDbContext db)

        {

            try

            {

                if (!db.Homeworks.Any())

                {

                    db.Homeworks.AddRange(

                        new Data.Homework

                        {

                            Subject = "Toán",

                            Title = "Bài tập về nhà: Giải hệ phương trình và căn thức",

                            Description = "Học sinh thực hiện làm các bài tập sau vào vở hoặc soạn bài trên hệ thống:\n\nBài 1: Giải hệ phương trình sau:\n  a) 2x + y = 5\n     3x - y = 5\n  b) x - 2y = -1\n     2x + y = 3\n\nBài 2: Rút gọn biểu thức A = (√x / (√x - 1)) - (1 / (x - √x)) với x > 0 và x ≠ 1.\n\n📌 Ghi chú: Chụp ảnh lời giải chi tiết hoặc nộp file ảnh/PDF đính kèm.",

                            Deadline = DateTime.Today.AddDays(3).AddHours(17).AddMinutes(0),

                            CreatedAt = DateTime.Now,

                            AttachmentPath = "",

                            ClassId = ""

                        },

                        new Data.Homework

                        {

                            Subject = "Ngữ Văn",

                            Title = "Viết đoạn văn nghị luận về tình mẫu tử",

                            Description = "Đề bài: Viết một đoạn văn (khoảng 200 chữ) trình bày suy nghĩ của em về vai trò của tình mẫu tử trong cuộc sống của mỗi con người.\n\n💡 Gợi ý:\n- Mở đoạn: Giới thiệu vấn đề nghị luận (tình mẫu tử là gì, tầm quan trọng).\n- Thân đoạn: Phân tích biểu hiện của tình mẫu tử, ý nghĩa đối với sự trưởng thành của con người, liên hệ thực tế.\n- Kết đoạn: Khái quát lại giá trị của tình mẫu tử và bài học nhận thức, hành động.\n\n📌 Ghi chú: Học sinh có thể soạn bài trực tiếp trên tab 'Soạn bài' của phần mềm hoặc tải file Word bài làm lên.",

                            Deadline = DateTime.Today.AddDays(5).AddHours(23).AddMinutes(59),

                            CreatedAt = DateTime.Now,

                            AttachmentPath = "",

                            ClassId = ""

                        },

                        new Data.Homework

                        {

                            Subject = "Tiếng Anh",

                            Title = "Unit 9 Homework: Essay about Protecting the Environment",

                            Description = "Write a short paragraph (120 - 150 words) about things you can do to protect the environment in your local area.\n\nKey vocabulary to use:\n- reduce, reuse, recycle\n- energy-saving light bulbs\n- public transport\n- single-use plastic\n\n📌 Ghi chú: Soạn trực tiếp hoặc tải file word/PDF của em lên.",

                            Deadline = DateTime.Today.AddDays(2).AddHours(12).AddMinutes(0),

                            CreatedAt = DateTime.Now,

                            AttachmentPath = "",

                            ClassId = ""

                        }

                    );

                    db.SaveChanges();

                    Log.Information("[DbMigrator] Seeded default Homework items");

                }

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Failed to seed default Homework items: {Err}", ex.Message);

            }

        }

        public static void EnsureSubjectsTable(Data.AppDbContext db)
        {
            try
            {
                if (!TableExists(db, "Subjects"))
                {
                    RunSafeSql(db, @"
                        CREATE TABLE IF NOT EXISTS ""Subjects"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_Subjects"" PRIMARY KEY AUTOINCREMENT,
                            ""Name"" TEXT NOT NULL,
                            ""ShortName"" TEXT NOT NULL,
                            ""ColorHex"" TEXT NOT NULL,
                            ""Icon"" TEXT NOT NULL,
                            ""DefaultRoom"" TEXT NOT NULL,
                            ""WeeklyPeriods"" INTEGER NOT NULL,
                            ""IsSystem"" INTEGER NOT NULL,
                            ""DisplayOrder"" INTEGER NOT NULL
                        );");
                    Log.Information("[DbMigrator] Created table Subjects");
                }

                SeedDefaultSubjects(db);
            }
            catch (Exception ex)
            {
                Log.Warning("[DbMigrator] EnsureSubjectsTable error: {Err}", ex.Message);
            }
        }

        public static void EnsurePeriodLogbooksTable(Data.AppDbContext db)
        {
            try
            {
                if (!TableExists(db, "PeriodLogbooks"))
                {
                    RunSafeSql(db, @"
                        CREATE TABLE IF NOT EXISTS ""PeriodLogbooks"" (
                            ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_PeriodLogbooks"" PRIMARY KEY AUTOINCREMENT,
                            ""ClassName"" TEXT NOT NULL DEFAULT '',
                            ""LessonId"" INTEGER NULL,
                            ""LessonTitle"" TEXT NOT NULL DEFAULT '',
                            ""Subject"" TEXT NOT NULL DEFAULT '',
                            ""Period"" INTEGER NOT NULL DEFAULT 1,
                            ""Date"" TEXT NOT NULL,
                            ""TotalStudents"" INTEGER NOT NULL DEFAULT 0,
                            ""PresentCount"" INTEGER NOT NULL DEFAULT 0,
                            ""AbsentCount"" INTEGER NOT NULL DEFAULT 0,
                            ""AbsentNotes"" TEXT NOT NULL DEFAULT '',
                            ""Rating"" TEXT NOT NULL DEFAULT 'A',
                            ""TeacherComment"" TEXT NOT NULL DEFAULT '',
                            ""HomeworkAssigned"" TEXT NOT NULL DEFAULT '',
                            ""TeacherName"" TEXT NOT NULL DEFAULT '',
                            ""CreatedAt"" TEXT NOT NULL
                        );");
                    Log.Information("[DbMigrator] Created table PeriodLogbooks");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[DbMigrator] EnsurePeriodLogbooksTable error: {Err}", ex.Message);
            }
        }

        private static void SeedDefaultSubjects(Data.AppDbContext db)
        {
            try
            {
                if (!db.Subjects.Any())
                {
                    var defaults = new[]
                    {
                        new Data.Subject { Name = "Toán học", ShortName = "Toán", ColorHex = "#1976D2", Icon = "📐", DefaultRoom = "P.Học", WeeklyPeriods = 4, IsSystem = true, DisplayOrder = 1 },
                        new Data.Subject { Name = "Ngữ văn", ShortName = "Văn", ColorHex = "#7B1FA2", Icon = "📖", DefaultRoom = "P.Học", WeeklyPeriods = 4, IsSystem = true, DisplayOrder = 2 },
                        new Data.Subject { Name = "Tiếng Anh", ShortName = "Anh", ColorHex = "#00838F", Icon = "🌐", DefaultRoom = "P.Ngoại ngữ", WeeklyPeriods = 3, IsSystem = true, DisplayOrder = 3 },
                        new Data.Subject { Name = "Vật lý", ShortName = "Lý", ColorHex = "#E64A19", Icon = "⚡", DefaultRoom = "P.Thực hành Lý", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 4 },
                        new Data.Subject { Name = "Hóa học", ShortName = "Hóa", ColorHex = "#2E7D32", Icon = "🧪", DefaultRoom = "P.Thực hành Hóa", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 5 },
                        new Data.Subject { Name = "Sinh học", ShortName = "Sinh", ColorHex = "#AD1457", Icon = "🧬", DefaultRoom = "P.Thực hành Sinh", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 6 },
                        new Data.Subject { Name = "Khoa học tự nhiên", ShortName = "KHTN", ColorHex = "#00897B", Icon = "🔬", DefaultRoom = "P.Thực hành KHTN", WeeklyPeriods = 4, IsSystem = true, DisplayOrder = 7 },
                        new Data.Subject { Name = "Lịch sử", ShortName = "Sử", ColorHex = "#5D4037", Icon = "🏛️", DefaultRoom = "P.Học", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 8 },
                        new Data.Subject { Name = "Địa lý", ShortName = "Địa", ColorHex = "#00695C", Icon = "🌍", DefaultRoom = "P.Học", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 9 },
                        new Data.Subject { Name = "Lịch sử & Địa lý", ShortName = "Sử-Địa", ColorHex = "#4E342E", Icon = "🗺️", DefaultRoom = "P.Học", WeeklyPeriods = 3, IsSystem = true, DisplayOrder = 10 },
                        new Data.Subject { Name = "GD Kinh tế & Pháp luật", ShortName = "GDKT&PL", ColorHex = "#F9A825", Icon = "⚖️", DefaultRoom = "P.Học", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 11 },
                        new Data.Subject { Name = "Giáo dục công dân", ShortName = "GDCD", ColorHex = "#F57F17", Icon = "🤝", DefaultRoom = "P.Học", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 12 },
                        new Data.Subject { Name = "Tin học", ShortName = "Tin", ColorHex = "#0277BD", Icon = "💻", DefaultRoom = "P.Tin học", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 13 },
                        new Data.Subject { Name = "Công nghệ", ShortName = "CN", ColorHex = "#558B2F", Icon = "⚙️", DefaultRoom = "P.Công nghệ", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 14 },
                        new Data.Subject { Name = "Giáo dục thể chất", ShortName = "GDTC", ColorHex = "#EF6C00", Icon = "⚽", DefaultRoom = "Sân thể dục", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 15 },
                        new Data.Subject { Name = "Âm nhạc", ShortName = "Nhạc", ColorHex = "#8E24AA", Icon = "🎵", DefaultRoom = "P.Âm nhạc", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 16 },
                        new Data.Subject { Name = "Mỹ thuật", ShortName = "MT", ColorHex = "#D81B60", Icon = "🎨", DefaultRoom = "P.Mỹ thuật", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 17 },
                        new Data.Subject { Name = "HĐ Trải nghiệm & Hướng nghiệp", ShortName = "HĐTN", ColorHex = "#FB8C00", Icon = "🌟", DefaultRoom = "Hội trường", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 18 },
                        new Data.Subject { Name = "STEM & Robotics", ShortName = "STEM", ColorHex = "#1565C0", Icon = "🤖", DefaultRoom = "P.STEM", WeeklyPeriods = 2, IsSystem = true, DisplayOrder = 19 },
                        new Data.Subject { Name = "GD Quốc phòng & An ninh", ShortName = "GDQP", ColorHex = "#33691E", Icon = "🎖️", DefaultRoom = "Sân trường", WeeklyPeriods = 1, IsSystem = true, DisplayOrder = 20 }
                    };

                    db.Subjects.AddRange(defaults);
                    db.SaveChanges();
                    Log.Information("[DbMigrator] Seeded default 20 subjects");
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[DbMigrator] SeedDefaultSubjects error: {Err}", ex.Message);
            }
        }



        private static void RunSafeSql(Data.AppDbContext db, string sql)

        {

            try

            {

                db.Database.ExecuteSqlRaw(sql);

            }

            catch (Exception ex)

            {

                // Bảng/cột đã tồn tại → bỏ qua (CREATE IF NOT EXISTS)

                Log.Debug("[DbMigrator] SQL skipped (already exists): {Err}", ex.Message);

            }

        }



        private static void BackupBeforeMigration(string dbPath)

        {

            try

            {

                var backupDir = AppPaths.BackupsDir;

                Directory.CreateDirectory(backupDir);

                var backupFile = Path.Combine(backupDir,

                    $"pre_migration_{DateTime.Now:yyyyMMdd_HHmmss}.db");

                File.Copy(dbPath, backupFile, overwrite: false);

                Log.Information("[DbMigrator] Pre-migration backup: {Path}", backupFile);

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Pre-migration backup failed: {Err}", ex.Message);

            }

        }



        private static void SaveVersion(string versionFile, string version)

        {

            Directory.CreateDirectory(Path.GetDirectoryName(versionFile)!);

            File.WriteAllText(versionFile, version);

        }



        private static Version ParseVersion(string ver)

        {

            return Version.TryParse(ver, out var v) ? v : new Version(0, 0, 0);

        }



        private class TempHomeworkItem

        {

            public string Subject { get; set; } = string.Empty;

            public string Title { get; set; } = string.Empty;

            public string Description { get; set; } = string.Empty;

            public DateTime Deadline { get; set; }

            public string Attachment { get; set; } = string.Empty;

        }



        private static void SeedDefaultGrammar(Data.AppDbContext db)

        {

            try

            {

                if (!db.GrammarTenses.Any())

                {

                    db.GrammarTenses.AddRange(

                        new Data.GrammarTense { Name = "Simple Present", NameVi = "Hiện tại đơn", Structure = "S + V_inf / V(s/es)", Example = "I go to school every day. / She goes to school.", Signal = "always, usually, every day, often", Color = "#1565C0" },

                        new Data.GrammarTense { Name = "Present Continuous", NameVi = "Hiện tại tiếp diễn", Structure = "S + am/is/are + V-ing", Example = "She is studying now.", Signal = "now, at the moment, look!, listen!", Color = "#1976D2" },

                        new Data.GrammarTense { Name = "Present Perfect", NameVi = "Hiện tại hoàn thành", Structure = "S + have/has + V3/ed", Example = "I have lived here for 5 years.", Signal = "since, for, already, yet, ever, never", Color = "#1E88E5" },

                        new Data.GrammarTense { Name = "Present Perfect Cont.", NameVi = "Hiện tại hoàn thành tiếp diễn", Structure = "S + have/has been + V-ing", Example = "He has been working all day.", Signal = "all day, all week, since, for", Color = "#42A5F5" },

                        new Data.GrammarTense { Name = "Simple Past", NameVi = "Quá khứ đơn", Structure = "S + V2/ed", Example = "I went to Hanoi last year.", Signal = "yesterday, last night, ago, in 2025", Color = "#2E7D32" },

                        new Data.GrammarTense { Name = "Past Continuous", NameVi = "Quá khứ tiếp diễn", Structure = "S + was/were + V-ing", Example = "They were playing when I came.", Signal = "when, while, at 8 PM yesterday", Color = "#388E3C" },

                        new Data.GrammarTense { Name = "Past Perfect", NameVi = "Quá khứ hoàn thành", Structure = "S + had + V3/ed", Example = "She had left before I arrived.", Signal = "before, after, by the time", Color = "#43A047" },

                        new Data.GrammarTense { Name = "Past Perfect Cont.", NameVi = "Quá khứ hoàn thành tiếp diễn", Structure = "S + had been + V-ing", Example = "He had been waiting for 2 hours.", Signal = "for, since, before", Color = "#66BB6A" },

                        new Data.GrammarTense { Name = "Simple Future", NameVi = "Tương lai đơn", Structure = "S + will + V_inf", Example = "I will call you tomorrow.", Signal = "tomorrow, next week, in the future", Color = "#E65100" },

                        new Data.GrammarTense { Name = "Near Future", NameVi = "Tương lai gần", Structure = "S + am/is/are + going to + V_inf", Example = "We are going to buy a new car next week.", Signal = "next week, next month, soon, tonight", Color = "#EF6C00" },

                        new Data.GrammarTense { Name = "Future Continuous", NameVi = "Tương lai tiếp diễn", Structure = "S + will be + V-ing", Example = "At 8 PM tomorrow, I will be studying.", Signal = "at this time tomorrow, at 8 PM tomorrow", Color = "#EF6C00" },

                        new Data.GrammarTense { Name = "Future Perfect", NameVi = "Tương lai hoàn thành", Structure = "S + will have + V3/ed", Example = "By 2030, I will have graduated.", Signal = "by, by the time, by end of", Color = "#F57C00" },

                        new Data.GrammarTense { Name = "Future Perfect Cont.", NameVi = "Tương lai hoàn thành tiếp diễn", Structure = "S + will have been + V-ing", Example = "By June, I will have been working here for 3 years.", Signal = "by, for", Color = "#FB8C00" }

                    );

                    db.SaveChanges();

                    Log.Information("[DbMigrator] Seeded default GrammarTenses");

                }



                if (!db.GrammarQuestions.Any())

                {

                    // Seed FillBlank questions

                    db.GrammarQuestions.AddRange(

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "I ___ (go) to school every day.", Hint = "Chia động từ - Hiện tại đơn", AnswersJson = "[\"go\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "She ___ (be) cooking now.", Hint = "Chia động từ - Hiện tại tiếp diễn", AnswersJson = "[\"is\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "They ___ (already/finish) their homework.", Hint = "Chia động từ - Hiện tại hoàn thành", AnswersJson = "[\"have already finished\",\"already have finished\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "He ___ (go) to Hanoi last year.", Hint = "Chia động từ - Quá khứ đơn", AnswersJson = "[\"went\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "I ___ (be) watching TV when you called.", Hint = "Chia động từ - Quá khứ tiếp diễn", AnswersJson = "[\"was\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "She ___ (leave) before I arrived.", Hint = "Chia động từ - Quá khứ hoàn thành", AnswersJson = "[\"had left\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "I ___ (call) you tomorrow.", Hint = "Chia động từ - Tương lai đơn", AnswersJson = "[\"will call\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "He ___ (play) football every Sunday.", Hint = "Chia động từ - Hiện tại đơn ngôi thứ 3 số ít", AnswersJson = "[\"plays\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "We ___ (study) English for 3 years.", Hint = "Chia động từ - Hiện tại hoàn thành", AnswersJson = "[\"have studied\",\"have been studying\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "She ___ (read) a book right now.", Hint = "Chia động từ - Hiện tại tiếp diễn", AnswersJson = "[\"is reading\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "They ___ (visit) Paris last summer.", Hint = "Chia động từ - Quá khứ đơn", AnswersJson = "[\"visited\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "I ___ (not/see) him since Monday.", Hint = "Chia động từ phủ định - Hiện tại hoàn thành", AnswersJson = "[\"haven't seen\",\"have not seen\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "She ___ (work) here since 2020.", Hint = "Chia động từ - Hiện tại hoàn thành tiếp diễn", AnswersJson = "[\"has worked\",\"has been working\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "By 2030, I ___ (graduate) from university.", Hint = "Chia động từ - Tương lai hoàn thành", AnswersJson = "[\"will have graduated\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "He always ___ (wake) up early.", Hint = "Chia động từ - Hiện tại đơn ngôi thứ 3 số ít", AnswersJson = "[\"wakes\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "We ___ (buy) a new car next week.", Hint = "Chia động từ - Tương lai gần", AnswersJson = "[\"are going to buy\"]", OptionsJson = "[]" },

                        new Data.GrammarQuestion { Category = "FillBlank", QuestionText = "Look at those black clouds! It ___ (rain) soon.", Hint = "Chia động từ - Tương lai gần", AnswersJson = "[\"is going to rain\"]", OptionsJson = "[]" }

                    );



                    // Seed MultipleChoice questions

                    db.GrammarQuestions.AddRange(

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"I go to school every day.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Hiện tại đơn\"]", OptionsJson = "[\"Hiện tại đơn\",\"Hiện tại tiếp diễn\",\"Quá khứ đơn\",\"Tương lai đơn\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"She is studying now.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Hiện tại tiếp diễn\"]", OptionsJson = "[\"Hiện tại đơn\",\"Hiện tại tiếp diễn\",\"Quá khứ tiếp diễn\",\"Hiện tại hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"I have lived here for 5 years.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Hiện tại hoàn thành\"]", OptionsJson = "[\"Quá khứ đơn\",\"Hiện tại hoàn thành\",\"Quá khứ hoàn thành\",\"Hiện tại đơn\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"I went to Hanoi last year.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Quá khứ đơn\"]", OptionsJson = "[\"Hiện tại đơn\",\"Quá khứ đơn\",\"Quá khứ tiếp diễn\",\"Hiện tại hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"They were playing when I came.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Quá khứ tiếp diễn\"]", OptionsJson = "[\"Quá khứ đơn\",\"Quá khứ tiếp diễn\",\"Hiện tại tiếp diễn\",\"Quá khứ hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"She had left before I arrived.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Quá khứ hoàn thành\"]", OptionsJson = "[\"Quá khứ đơn\",\"Quá khứ tiếp diễn\",\"Quá khứ hoàn thành\",\"Hiện tại hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"I will call you tomorrow.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Tương lai đơn\"]", OptionsJson = "[\"Hiện tại đơn\",\"Tương lai đơn\",\"Tương lai tiếp diễn\",\"Tương lai hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"He has been working all day.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Hiện tại hoàn thành tiếp diễn\"]", OptionsJson = "[\"Hiện tại tiếp diễn\",\"Hiện tại hoàn thành\",\"Hiện tại hoàn thành tiếp diễn\",\"Quá khứ tiếp diễn\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"At 8 PM, I will be studying.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Tương lai tiếp diễn\"]", OptionsJson = "[\"Tương lai đơn\",\"Tương lai tiếp diễn\",\"Hiện tại tiếp diễn\",\"Tương lai hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"By 2030, I will have graduated.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Tương lai hoàn thành\"]", OptionsJson = "[\"Tương lai đơn\",\"Tương lai tiếp diễn\",\"Tương lai hoàn thành\",\"Hiện tại hoàn thành\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"He had been waiting for 2 hours.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Quá khứ hoàn thành tiếp diễn\"]", OptionsJson = "[\"Quá khứ tiếp diễn\",\"Quá khứ hoàn thành\",\"Quá khứ hoàn thành tiếp diễn\",\"Hiện tại hoàn thành tiếp diễn\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"She plays tennis on Sundays.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Hiện tại đơn\"]", OptionsJson = "[\"Hiện tại đơn\",\"Hiện tại tiếp diễn\",\"Quá khứ đơn\",\"Tương lai đơn\"]" },

                        new Data.GrammarQuestion { Category = "MultipleChoice", QuestionText = "\"They are going to visit their grandparents tonight.\"", Hint = "Xác định thì phù hợp", AnswersJson = "[\"Tương lai gần\"]", OptionsJson = "[\"Tương lai đơn\",\"Tương lai gần\",\"Hiện tại tiếp diễn\",\"Tương lai tiếp diễn\"]" }

                    );



                    db.SaveChanges();

                    Log.Information("[DbMigrator] Seeded default GrammarQuestions");

                }

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Failed to seed default Grammar: {Err}", ex.Message);

            }

        }



        private static bool ColumnExists(Data.AppDbContext db, string tableName, string columnName)

        {

            try

            {

                var conn = db.Database.GetDbConnection();

                bool wasClosed = conn.State == System.Data.ConnectionState.Closed;

                if (wasClosed) conn.Open();

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = $"PRAGMA table_info({tableName})";

                    using (var reader = cmd.ExecuteReader())

                    {

                        while (reader.Read())

                        {

                            var name = reader["name"]?.ToString();

                            if (string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))

                            {

                                if (wasClosed) conn.Close();

                                return true;

                            }

                        }

                    }

                }

                if (wasClosed) conn.Close();

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Error checking column {Col} in table {Tab}: {Msg}", columnName, tableName, ex.Message);

            }

            return false;

        }



        public static void EnsureGrade11SamplePdfsExist()
        {
            try
            {
                var libraryFolder = Path.Combine(AppPaths.DocumentsDir, "Library", "Bài giảng");
                Directory.CreateDirectory(libraryFolder);

                string sampleSource = Path.Combine(AppPaths.DocumentsDir, "Library", "Bài giảng", "Bai_giang_Toan_lop_12_Khao_sat_ham_so.pdf");
                if (!File.Exists(sampleSource))
                {
                    var files = Directory.GetFiles(libraryFolder, "*.pdf");
                    if (files.Length > 0)
                    {
                        sampleSource = files[0];
                    }
                }

                string[] targetFiles = new string[]
                {
                    "Bai_giang_Toan_lop_11_Ham_so_luong_giac.pdf",
                    "Bai_giang_Vat_ly_lop_11_The_nang_dien.pdf",
                    "Bai_giang_Ngu_van_lop_11_Chu_nguoi_tu_tu.pdf",
                    "Bai_giang_Tieng_Anh_lop_11_Generation_Gap.pdf",
                    "Bai_giang_Sinh_hoc_lop_11_Cam_ung.pdf",
                    "Bai_giang_Dia_ly_lop_11_Dong_Nam_A.pdf",
                    "Bai_giang_GDCD_lop_11_Kinh_te.pdf"
                };

                foreach (var fileName in targetFiles)
                {
                    var targetPath = Path.Combine(libraryFolder, fileName);
                    if (!File.Exists(targetPath))
                    {
                        if (File.Exists(sampleSource))
                        {
                            File.Copy(sampleSource, targetPath);
                            Log.Information("[DbMigrator] Self-healed missing file: {Path}", targetPath);
                        }
                        else
                        {
                            File.WriteAllBytes(targetPath, new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2d, 0x31, 0x2e, 0x34, 0x0a, 0x25, 0xe2, 0xe3, 0xcf, 0xd3, 0x0a });
                            Log.Information("[DbMigrator] Created placeholder file: {Path}", targetPath);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("[DbMigrator] EnsureGrade11SamplePdfsExist error: {Msg}", ex.Message);
            }
        }

        private static bool TableExists(Data.AppDbContext db, string tableName)

        {

            try

            {

                var conn = db.Database.GetDbConnection();

                bool wasClosed = conn.State == System.Data.ConnectionState.Closed;

                if (wasClosed) conn.Open();

                using (var cmd = conn.CreateCommand())

                {

                    cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=@tableName";

                    var param = cmd.CreateParameter();

                    param.ParameterName = "@tableName";

                    param.Value = tableName;

                    cmd.Parameters.Add(param);

                    var count = Convert.ToInt32(cmd.ExecuteScalar());

                    if (wasClosed) conn.Close();

                    return count > 0;

                }

            }

            catch (Exception ex)

            {

                Log.Warning("[DbMigrator] Error checking table {Tab}: {Msg}", tableName, ex.Message);

            }

            return false;

        }

    }

}

