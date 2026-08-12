using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QASmartClass.LearningTools.Models
{
    /// <summary>JSON model for Phase4 chapter data files</summary>
    public class MathChapterData
    {
        [JsonPropertyName("metadata")]
        public ChapterMetadata Metadata { get; set; } = new();

        // Phase 2 Pedagogical Fields
        [JsonPropertyName("funFacts")]
        public List<string> FunFacts { get; set; } = new();

        [JsonPropertyName("realWorldApps")]
        public List<RealWorldApp> RealWorldApps { get; set; } = new();

        [JsonPropertyName("openChallenge")]
        public string? OpenChallenge { get; set; }

        [JsonPropertyName("historyNote")]
        public string? HistoryNote { get; set; }

        [JsonPropertyName("studyTips")]
        public List<string> StudyTips { get; set; } = new();

        [JsonPropertyName("sections")]
        public List<ChapterSection> Sections { get; set; } = new();

        [JsonPropertyName("commonMistakes")]
        public List<CommonMistake> CommonMistakes { get; set; } = new();

        [JsonPropertyName("quizBank")]
        public List<QuizBankEntry>? QuizBank { get; set; }
    }

    public class RealWorldApp
    {
        [JsonPropertyName("icon")] public string Icon { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("desc")] public string Desc { get; set; } = "";
    }

    public class QuizBankEntry
    {
        [JsonPropertyName("quizId")] public string QuizId { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("timeLimit")] public int TimeLimit { get; set; } = 600;
        [JsonPropertyName("questions")] public List<QuizBankQuestion>? Questions { get; set; }
    }

    public class QuizBankQuestion
    {
        [JsonPropertyName("ref")] public string Ref { get; set; } = "";
    }

    public class ChapterMetadata
    {
        [JsonPropertyName("chapterId")] public string ChapterId { get; set; } = "";
        [JsonPropertyName("chapterName")] public string ChapterName { get; set; } = "";
        [JsonPropertyName("grade")] public int Grade { get; set; }
        [JsonPropertyName("totalSections")] public int TotalSections { get; set; }
        [JsonPropertyName("totalProblems")] public int TotalProblems { get; set; }
        [JsonPropertyName("thptWeight")] public string ThptWeight { get; set; } = "";
        [JsonPropertyName("estimatedHours")] public double EstimatedHours { get; set; }
        [JsonPropertyName("toolMapping")] public List<string> ToolMapping { get; set; } = new();
    }

    public class ChapterSection
    {
        [JsonPropertyName("sectionId")] public string SectionId { get; set; } = "";
        [JsonPropertyName("sectionName")] public string SectionName { get; set; } = "";
        [JsonPropertyName("audioUrl")] public string? AudioUrl { get; set; }
        [JsonPropertyName("sgkUrl")] public string? SgkUrl { get; set; }
        [JsonPropertyName("order")] public int Order { get; set; }
        [JsonPropertyName("concepts")] public List<Concept> Concepts { get; set; } = new();
        [JsonPropertyName("problems")] public List<Problem> Problems { get; set; } = new();
    }

    public class Concept
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("imageUrls")] public List<string>? ImageUrls { get; set; }
        [JsonPropertyName("definition")] public string Definition { get; set; } = "";
        [JsonPropertyName("formula")] public string Formula { get; set; } = "";
        [JsonPropertyName("formulas")] public List<string> Formulas { get; set; } = new();
        [JsonPropertyName("properties")] public List<string> Properties { get; set; } = new();
        [JsonPropertyName("examples")] public object? Examples { get; set; }    // string[] or object[]
        // Additional fields found in real chapter JSON data
        [JsonPropertyName("method")] public object? Method { get; set; }         // string or List<string>
        [JsonPropertyName("rules")] public List<string>? Rules { get; set; }
        [JsonPropertyName("theorem")] public string? Theorem { get; set; }
        [JsonPropertyName("types")] public object? Types { get; set; }
        [JsonPropertyName("cases")] public List<string>? Cases { get; set; }
        [JsonPropertyName("details")] public object? Details { get; set; }       // Dict<string,string> or nested obj
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    public class Problem
    {
        [JsonPropertyName("problemId")] public string ProblemId { get; set; } = "";
        [JsonPropertyName("difficulty")] public string Difficulty { get; set; } = "Medium";
        [JsonPropertyName("type")] public string Type { get; set; } = "multiple_choice";
        [JsonPropertyName("question")] public string Question { get; set; } = "";
        [JsonPropertyName("options")] public List<string> Options { get; set; } = new();
        [JsonPropertyName("correctAnswer")] public string CorrectAnswer { get; set; } = "";
        [JsonPropertyName("solution")] public string Solution { get; set; } = "";
        [JsonPropertyName("answer")] public string Answer { get; set; } = "";
        [JsonPropertyName("points")] public int Points { get; set; } = 10;
        [JsonPropertyName("hints")] public List<string>? Hints { get; set; }
        [JsonPropertyName("graphExpression")] public string? GraphExpression { get; set; }

        /// <summary>Returns the effective answer from either 'correctAnswer' or 'answer' field</summary>
        public string GetAnswer() =>
            !string.IsNullOrEmpty(CorrectAnswer) ? CorrectAnswer : Answer;
    }

    public class CommonMistake
    {
        [JsonPropertyName("mistake")] public string Mistake { get; set; } = "";
        [JsonPropertyName("correction")] public string Correction { get; set; } = "";
    }

    // ═══════════════════════════════════════════════════════════
    //  MOCK EXAM MODELS (THPT_Mock_Exam_*.json)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Model for THPT Mock Exam files</summary>
    public class MockExamData
    {
        [JsonPropertyName("metadata")] public ExamMetadata Metadata { get; set; } = new();
        [JsonPropertyName("questions")] public List<ExamQuestion> Questions { get; set; } = new();
    }

    public class ExamMetadata
    {
        [JsonPropertyName("examId")] public string ExamId { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("totalQuestions")] public int TotalQuestions { get; set; }
        [JsonPropertyName("timeLimit")] public int TimeLimit { get; set; } = 90;
        [JsonPropertyName("totalPoints")] public double TotalPoints { get; set; } = 10;
        [JsonPropertyName("pointsPerQuestion")] public double PointsPerQuestion { get; set; } = 0.2;
        [JsonPropertyName("grade")] public int? Grade { get; set; }
        [JsonPropertyName("examType")] public string? ExamType { get; set; }
    }

    public class ExamQuestion
    {
        [JsonPropertyName("id")] public int Id { get; set; }
        [JsonPropertyName("topic")] public string Topic { get; set; } = "";

        // Support both full ("Easy"/"Medium"/"Hard") and short ("d") difficulty formats
        [JsonPropertyName("difficulty")] public string? DifficultyFull { get; set; }
        [JsonPropertyName("d")] public string? DifficultyShort { get; set; }

        // Support both full ("question") and short ("q") question formats
        [JsonPropertyName("question")] public string? QuestionFull { get; set; }
        [JsonPropertyName("q")] public string? QuestionShort { get; set; }

        // Support both full ("options") and short ("o") options formats
        [JsonPropertyName("options")] public List<string>? OptionsFull { get; set; }
        [JsonPropertyName("o")] public List<string>? OptionsShort { get; set; }

        // Support both full ("answer") and short ("a") answer formats
        [JsonPropertyName("answer")] public string? AnswerFull { get; set; }
        [JsonPropertyName("a")] public string? AnswerShort { get; set; }

        [JsonPropertyName("hints")] public List<string>? HintsFull { get; set; }
        [JsonPropertyName("h")] public List<string>? HintsShort { get; set; }

        [JsonPropertyName("graphExpression")] public string? GraphExpressionFull { get; set; }
        [JsonPropertyName("g")] public string? GraphExpressionShort { get; set; }

        [JsonPropertyName("solution")] public string? Solution { get; set; }

        // Unified accessors
        public string GetDifficulty() => DifficultyFull ?? DifficultyShort ?? "Medium";
        public string GetQuestion() => QuestionFull ?? QuestionShort ?? "";
        public List<string> GetOptions() => OptionsFull ?? OptionsShort ?? new();
        public string GetAnswer() => AnswerFull ?? AnswerShort ?? "";
        public List<string> GetHints() => HintsFull ?? HintsShort ?? new();
        public string GetGraphExpression() => GraphExpressionFull ?? GraphExpressionShort ?? "";
    }

    // ═══════════════════════════════════════════════════════════
    //  TOOL-TOPIC MAPPING (tool_topic_mapping.json)
    // ═══════════════════════════════════════════════════════════

    /// <summary>Root model for tool_topic_mapping.json</summary>
    public class ToolTopicMappingData
    {
        [JsonPropertyName("mappings")] public List<TopicMappingEntry> Mappings { get; set; } = new();
        [JsonPropertyName("examMapping")] public Dictionary<string, ExamMappingEntry> ExamMapping { get; set; } = new();
    }

    public class TopicMappingEntry
    {
        [JsonPropertyName("tool")] public string Tool { get; set; } = "";
        [JsonPropertyName("chapters")] public List<string> Chapters { get; set; } = new();
        [JsonPropertyName("topics")] public List<string> Topics { get; set; } = new();
        [JsonPropertyName("grades")] public List<int> Grades { get; set; } = new();
        [JsonPropertyName("dataFile")] public string DataFile { get; set; } = "";
    }

    public class ExamMappingEntry
    {
        [JsonPropertyName("file")] public string File { get; set; } = "";
        [JsonPropertyName("tools")] public List<string> Tools { get; set; } = new();
    }
}
