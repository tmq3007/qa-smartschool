-- ============================================================
-- QA SmartClass - Math Learning System Database Schema
-- Version: 1.0 | Date: 2026-05-04
-- Database: SQLite
-- ============================================================

-- 1. STUDENTS & CLASSES
-- ============================================================

CREATE TABLE IF NOT EXISTS classes (
    class_id        TEXT PRIMARY KEY,           -- e.g. "10A1"
    class_name      TEXT NOT NULL,
    grade           INTEGER NOT NULL CHECK (grade BETWEEN 1 AND 12),
    school_year     TEXT NOT NULL,              -- e.g. "2025-2026"
    teacher_id      TEXT,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS students (
    student_id      TEXT PRIMARY KEY,           -- e.g. "HS_10A1_001"
    student_name    TEXT NOT NULL,
    class_id        TEXT NOT NULL REFERENCES classes(class_id),
    device_id       TEXT,                       -- tablet/BYOD identifier
    avatar_url      TEXT,
    total_xp        INTEGER DEFAULT 0,
    level           INTEGER DEFAULT 1,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_students_class ON students(class_id);

-- 2. CURRICULUM DATA
-- ============================================================

CREATE TABLE IF NOT EXISTS chapters (
    chapter_id      TEXT PRIMARY KEY,           -- e.g. "g10_ch01"
    chapter_name    TEXT NOT NULL,
    grade           INTEGER NOT NULL CHECK (grade BETWEEN 1 AND 12),
    chapter_order   INTEGER NOT NULL,
    total_sections  INTEGER DEFAULT 0,
    total_problems  INTEGER DEFAULT 0,
    thpt_weight     TEXT,                       -- e.g. "8-12%"
    estimated_hours REAL,
    data_file       TEXT NOT NULL,              -- e.g. "G10_CH01_Menh_De_Tap_Hop.json"
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_chapters_grade ON chapters(grade, chapter_order);

CREATE TABLE IF NOT EXISTS tool_mapping (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    chapter_id      TEXT NOT NULL REFERENCES chapters(chapter_id),
    tool_name       TEXT NOT NULL,              -- e.g. "QuadraticTool"
    UNIQUE(chapter_id, tool_name)
);

-- 3. EXERCISES & PROBLEMS
-- ============================================================

CREATE TABLE IF NOT EXISTS exercises (
    exercise_id     TEXT PRIMARY KEY,           -- e.g. "ex_stn_001"
    chapter_id      TEXT REFERENCES chapters(chapter_id),
    section_id      TEXT,
    difficulty      TEXT NOT NULL CHECK (difficulty IN ('Easy', 'Medium', 'Hard')),
    type            TEXT CHECK (type IN ('multiple_choice', 'calculation', 'proof')),
    question        TEXT NOT NULL,
    options         TEXT,                       -- JSON array for MC questions
    correct_answer  TEXT,
    solution        TEXT NOT NULL,
    points          INTEGER DEFAULT 10 CHECK (points BETWEEN 5 AND 30),
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_exercises_chapter ON exercises(chapter_id);
CREATE INDEX idx_exercises_difficulty ON exercises(difficulty);

-- 4. STUDENT PROGRESS
-- ============================================================

CREATE TABLE IF NOT EXISTS student_progress (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    chapter_id      TEXT NOT NULL REFERENCES chapters(chapter_id),
    status          TEXT DEFAULT 'not_started' CHECK (status IN ('not_started', 'in_progress', 'completed')),
    score           REAL DEFAULT 0,            -- 0.0 to 100.0
    time_spent_min  REAL DEFAULT 0,
    attempts        INTEGER DEFAULT 0,
    last_accessed   DATETIME,
    completed_at    DATETIME,
    UNIQUE(student_id, chapter_id)
);

CREATE INDEX idx_progress_student ON student_progress(student_id);
CREATE INDEX idx_progress_chapter ON student_progress(chapter_id);

CREATE TABLE IF NOT EXISTS exercise_attempts (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    exercise_id     TEXT NOT NULL REFERENCES exercises(exercise_id),
    student_answer  TEXT,
    is_correct      BOOLEAN NOT NULL,
    time_taken_sec  INTEGER,
    attempt_number  INTEGER DEFAULT 1,
    attempted_at    DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE INDEX idx_attempts_student ON exercise_attempts(student_id);
CREATE INDEX idx_attempts_exercise ON exercise_attempts(exercise_id);

-- 5. QUIZ & EXAM RESULTS
-- ============================================================

CREATE TABLE IF NOT EXISTS quiz_sessions (
    session_id      TEXT PRIMARY KEY,
    quiz_type       TEXT NOT NULL CHECK (quiz_type IN ('chapter_quiz', 'mock_exam', 'daily_challenge')),
    chapter_id      TEXT REFERENCES chapters(chapter_id),
    exam_file       TEXT,                      -- for mock exams
    total_questions INTEGER NOT NULL,
    time_limit_sec  INTEGER,                   -- NULL = no limit
    created_by      TEXT,                      -- teacher_id
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS quiz_results (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    session_id      TEXT NOT NULL REFERENCES quiz_sessions(session_id),
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    score           REAL NOT NULL,             -- 0.0 to 10.0
    correct_count   INTEGER NOT NULL,
    total_count     INTEGER NOT NULL,
    time_taken_sec  INTEGER,
    rank_in_class   INTEGER,
    submitted_at    DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(session_id, student_id)
);

CREATE INDEX idx_quiz_results_student ON quiz_results(student_id);
CREATE INDEX idx_quiz_results_session ON quiz_results(session_id);

-- 6. LESSON MANAGEMENT (Focus/Unfocus system)
-- ============================================================

CREATE TABLE IF NOT EXISTS lessons (
    lesson_id       TEXT PRIMARY KEY,
    teacher_id      TEXT NOT NULL,
    class_id        TEXT NOT NULL REFERENCES classes(class_id),
    chapter_id      TEXT REFERENCES chapters(chapter_id),
    lesson_title    TEXT NOT NULL,
    status          TEXT DEFAULT 'draft' CHECK (status IN ('draft', 'active', 'paused', 'completed')),
    started_at      DATETIME,
    ended_at        DATETIME,
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS lesson_focus_blocks (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    lesson_id       TEXT NOT NULL REFERENCES lessons(lesson_id),
    block_type      TEXT NOT NULL CHECK (block_type IN ('theory', 'exercise', 'quiz', 'discussion', 'tool')),
    content_ref     TEXT,                      -- exercise_id, section_id, or tool_name
    display_order   INTEGER NOT NULL,
    duration_min    INTEGER,
    is_focused      BOOLEAN DEFAULT 0,         -- currently focused on student screens?
    focused_at      DATETIME,
    unfocused_at    DATETIME
);

CREATE INDEX idx_focus_lesson ON lesson_focus_blocks(lesson_id);

-- 7. GAMIFICATION
-- ============================================================

CREATE TABLE IF NOT EXISTS badges (
    badge_id        TEXT PRIMARY KEY,           -- e.g. "first_perfect", "streak_7"
    badge_name      TEXT NOT NULL,
    description     TEXT,
    icon_url        TEXT,
    xp_reward       INTEGER DEFAULT 0,
    criteria        TEXT NOT NULL               -- JSON criteria
);

CREATE TABLE IF NOT EXISTS student_badges (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    badge_id        TEXT NOT NULL REFERENCES badges(badge_id),
    earned_at       DATETIME DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(student_id, badge_id)
);

CREATE TABLE IF NOT EXISTS daily_streaks (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    streak_date     DATE NOT NULL,
    exercises_done  INTEGER DEFAULT 0,
    xp_earned       INTEGER DEFAULT 0,
    UNIQUE(student_id, streak_date)
);

CREATE INDEX idx_streaks_student ON daily_streaks(student_id);

-- 8. HOMEWORK (BTVN)
-- ============================================================

CREATE TABLE IF NOT EXISTS homework (
    homework_id     TEXT PRIMARY KEY,
    teacher_id      TEXT NOT NULL,
    class_id        TEXT NOT NULL REFERENCES classes(class_id),
    chapter_id      TEXT REFERENCES chapters(chapter_id),
    title           TEXT NOT NULL,
    description     TEXT,
    exercise_ids    TEXT NOT NULL,              -- JSON array of exercise_ids
    deadline        DATETIME NOT NULL,
    max_attempts    INTEGER DEFAULT 3,
    status          TEXT DEFAULT 'active' CHECK (status IN ('draft', 'active', 'closed')),
    created_at      DATETIME DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS homework_submissions (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    homework_id     TEXT NOT NULL REFERENCES homework(homework_id),
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    answers         TEXT NOT NULL,             -- JSON {exercise_id: answer}
    score           REAL,
    submitted_at    DATETIME DEFAULT CURRENT_TIMESTAMP,
    is_late         BOOLEAN DEFAULT 0,
    UNIQUE(homework_id, student_id)
);

CREATE INDEX idx_hw_sub_student ON homework_submissions(student_id);

-- 9. SPACED REPETITION
-- ============================================================

CREATE TABLE IF NOT EXISTS review_schedule (
    id              INTEGER PRIMARY KEY AUTOINCREMENT,
    student_id      TEXT NOT NULL REFERENCES students(student_id),
    exercise_id     TEXT NOT NULL REFERENCES exercises(exercise_id),
    ease_factor     REAL DEFAULT 2.5,          -- SM-2 algorithm
    interval_days   INTEGER DEFAULT 1,
    repetitions     INTEGER DEFAULT 0,
    next_review     DATE NOT NULL,
    last_quality    INTEGER CHECK (last_quality BETWEEN 0 AND 5),
    UNIQUE(student_id, exercise_id)
);

CREATE INDEX idx_review_student ON review_schedule(student_id, next_review);

-- 10. ANALYTICS VIEWS
-- ============================================================

-- Student performance summary per chapter
CREATE VIEW IF NOT EXISTS v_student_chapter_summary AS
SELECT 
    s.student_id,
    s.student_name,
    s.class_id,
    c.chapter_id,
    c.chapter_name,
    c.grade,
    sp.status,
    sp.score,
    sp.time_spent_min,
    sp.attempts,
    sp.completed_at,
    CASE 
        WHEN sp.score >= 90 THEN 'A'
        WHEN sp.score >= 75 THEN 'B'
        WHEN sp.score >= 50 THEN 'C'
        ELSE 'D'
    END as grade_letter
FROM students s
LEFT JOIN student_progress sp ON s.student_id = sp.student_id
LEFT JOIN chapters c ON sp.chapter_id = c.chapter_id;

-- Class-level analytics
CREATE VIEW IF NOT EXISTS v_class_analytics AS
SELECT 
    cl.class_id,
    cl.class_name,
    cl.grade,
    c.chapter_id,
    c.chapter_name,
    COUNT(sp.student_id) as students_attempted,
    AVG(sp.score) as avg_score,
    MIN(sp.score) as min_score,
    MAX(sp.score) as max_score,
    SUM(CASE WHEN sp.status = 'completed' THEN 1 ELSE 0 END) as completed_count,
    COUNT(DISTINCT s.student_id) as total_students
FROM classes cl
JOIN students s ON cl.class_id = s.class_id
LEFT JOIN student_progress sp ON s.student_id = sp.student_id
LEFT JOIN chapters c ON sp.chapter_id = c.chapter_id
GROUP BY cl.class_id, c.chapter_id;

-- Difficulty analysis (which exercises students struggle with)
CREATE VIEW IF NOT EXISTS v_exercise_difficulty_analysis AS
SELECT 
    e.exercise_id,
    e.chapter_id,
    e.difficulty,
    e.question,
    COUNT(ea.id) as total_attempts,
    SUM(CASE WHEN ea.is_correct THEN 1 ELSE 0 END) as correct_count,
    ROUND(100.0 * SUM(CASE WHEN ea.is_correct THEN 1 ELSE 0 END) / COUNT(ea.id), 1) as success_rate,
    AVG(ea.time_taken_sec) as avg_time_sec
FROM exercises e
LEFT JOIN exercise_attempts ea ON e.exercise_id = ea.exercise_id
GROUP BY e.exercise_id
HAVING total_attempts > 0
ORDER BY success_rate ASC;

-- ============================================================
-- SEED DATA: Chapters from Phase4_Data
-- ============================================================

-- Grade 10
INSERT OR IGNORE INTO chapters (chapter_id, chapter_name, grade, chapter_order, data_file) VALUES
('g10_ch01', 'Mệnh Đề, Tập Hợp & Hàm Số', 10, 1, 'G10_CH01_Menh_De_Tap_Hop.json'),
('g10_ch02', 'Hàm Số & Đồ Thị', 10, 2, 'G10_CH02_Ham_So_Do_Thi.json'),
('g10_ch03', 'PT, BPT & Hệ PT Nâng Cao', 10, 3, 'G10_CH03_PT_BPT.json'),
('g10_ch04', 'Lượng Giác Cơ Bản', 10, 4, 'G10_CH04_Luong_Giac.json'),
('g10_ch05', 'Thống Kê Mô Tả', 10, 5, 'G10_CH05_Thong_Ke.json'),
('g10_ch06', 'Vectơ & Tọa Độ Trong Mặt Phẳng', 10, 6, 'G10_CH06_Vecto_Toa_Do.json');

-- Grade 11
INSERT OR IGNORE INTO chapters (chapter_id, chapter_name, grade, chapter_order, data_file) VALUES
('g11_ch01', 'Lượng Giác Nâng Cao', 11, 1, 'G11_CH01_Luong_Giac_NC.json'),
('g11_ch02', 'Dãy Số & Cấp Số', 11, 2, 'G11_CH02_Day_So.json'),
('g11_ch03', 'Giới Hạn & Hàm Số Liên Tục', 11, 3, 'G11_CH03_Gioi_Han.json'),
('g11_ch04', 'Đạo Hàm Cơ Bản', 11, 4, 'G11_CH04_Dao_Ham.json'),
('g11_ch05', 'Tổ Hợp & Xác Suất', 11, 5, 'G11_CH05_To_Hop_XS.json'),
('g11_ch06', 'Thống Kê & Xác Suất Nâng Cao', 11, 6, 'G11_CH06_Thong_Ke_XS.json');

-- Grade 12
INSERT OR IGNORE INTO chapters (chapter_id, chapter_name, grade, chapter_order, data_file) VALUES
('g12_ch01', 'Ứng Dụng Đạo Hàm Khảo Sát Hàm Số', 12, 1, 'G12_CH01_Dao_Ham.json'),
('g12_ch02', 'Hàm Số Mũ & Logarit', 12, 2, 'G12_CH02_Mu_Logarit.json'),
('g12_ch03', 'Nguyên Hàm, Tích Phân & Ứng Dụng', 12, 3, 'G12_CH03_Tich_Phan.json'),
('g12_ch04', 'Số Phức', 12, 4, 'G12_CH04_So_Phuc.json'),
('g12_ch05', 'Tổ Hợp & Xác Suất', 12, 5, 'G12_CH05_To_Hop_XS.json'),
('g12_ch06', 'Hình Học Không Gian (Phương pháp Tọa Độ)', 12, 6, 'G12_CH06_Hinh_Hoc_KG.json');

-- Gamification badges (starter set)
INSERT OR IGNORE INTO badges (badge_id, badge_name, description, xp_reward, criteria) VALUES
('first_login', 'Hành Khách Mới', 'Đăng nhập lần đầu tiên', 10, '{"type":"first_login"}'),
('first_perfect', 'Điểm 10 Đầu Tiên', 'Đạt điểm tuyệt đối lần đầu', 50, '{"type":"perfect_score","count":1}'),
('streak_3', 'Chuyên Cần 3', 'Học liên tục 3 ngày', 30, '{"type":"streak","days":3}'),
('streak_7', 'Chiến Binh Tuần', 'Học liên tục 7 ngày', 100, '{"type":"streak","days":7}'),
('streak_30', 'Huyền Thoại Tháng', 'Học liên tục 30 ngày', 500, '{"type":"streak","days":30}'),
('chapter_master', 'Bậc Thầy Chương', 'Hoàn thành 1 chương với điểm ≥ 90', 200, '{"type":"chapter_complete","min_score":90}'),
('quiz_king', 'Vua Quiz', 'Xếp hạng 1 trong lớp 5 lần', 300, '{"type":"quiz_rank","rank":1,"count":5}'),
('math_explorer', 'Nhà Thám Hiểm', 'Hoàn thành 5 chương', 250, '{"type":"chapters_completed","count":5}'),
('speed_solver', 'Giải Nhanh', 'Giải đúng 10 bài dưới 30s mỗi bài', 150, '{"type":"speed_solve","count":10,"max_sec":30}'),
('comeback_kid', 'Quay Lại Mạnh Mẽ', 'Cải thiện điểm từ D lên B+ trong 1 chương', 200, '{"type":"improvement","from":"D","to":"B"}');
