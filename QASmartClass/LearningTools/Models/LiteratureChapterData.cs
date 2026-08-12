using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace QASmartClass.LearningTools.Models
{
    /// <summary>JSON model for Phase4 Literature chapter data files</summary>
    public class LiteratureChapterData
    {
        [JsonPropertyName("metadata")]
        public LiteratureChapterMetadata Metadata { get; set; } = new();

        [JsonPropertyName("funFacts")]
        public List<string> FunFacts { get; set; } = new();

        [JsonPropertyName("realWorldApps")]
        public List<LiteratureRealWorldApp> RealWorldApps { get; set; } = new();

        [JsonPropertyName("openChallenge")]
        public string? OpenChallenge { get; set; }

        [JsonPropertyName("historyNote")]
        public string? HistoryNote { get; set; }

        [JsonPropertyName("studyTips")]
        public List<string> StudyTips { get; set; } = new();

        [JsonPropertyName("sections")]
        public List<LiteratureChapterSection> Sections { get; set; } = new();

        [JsonPropertyName("commonMistakes")]
        public List<LiteratureCommonMistake> CommonMistakes { get; set; } = new();

        [JsonPropertyName("quizBank")]
        public List<LiteratureQuizBankEntry>? QuizBank { get; set; }
    }

    public class LiteratureRealWorldApp
    {
        [JsonPropertyName("icon")] public string Icon { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("desc")] public string Desc { get; set; } = "";
    }

    public class LiteratureQuizBankEntry
    {
        [JsonPropertyName("quizId")] public string QuizId { get; set; } = "";
        [JsonPropertyName("title")] public string Title { get; set; } = "";
        [JsonPropertyName("timeLimit")] public int TimeLimit { get; set; } = 600;
        [JsonPropertyName("questions")] public List<LiteratureQuizBankQuestion>? Questions { get; set; }
    }

    public class LiteratureQuizBankQuestion
    {
        [JsonPropertyName("ref")] public string Ref { get; set; } = "";
    }

    public class LiteratureChapterMetadata
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

    public class LiteratureChapterSection
    {
        [JsonPropertyName("sectionId")] public string SectionId { get; set; } = "";
        [JsonPropertyName("sectionName")] public string SectionName { get; set; } = "";
        [JsonPropertyName("audioUrl")] public string? AudioUrl { get; set; }
        [JsonPropertyName("sgkUrl")] public string? SgkUrl { get; set; }
        [JsonPropertyName("order")] public int Order { get; set; }
        [JsonPropertyName("concepts")] public List<LiteratureConcept> Concepts { get; set; } = new();
        [JsonPropertyName("problems")] public List<LiteratureProblem> Problems { get; set; } = new();
    }

    public class LiteratureConcept
    {
        [JsonPropertyName("id")] public string Id { get; set; } = "";
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("imageUrls")] public List<string>? ImageUrls { get; set; }
        [JsonPropertyName("definition")] public string Definition { get; set; } = "";
        [JsonPropertyName("formula")] public string Formula { get; set; } = "";
        [JsonPropertyName("formulas")] public List<string> Formulas { get; set; } = new();
        [JsonPropertyName("properties")] public List<string> Properties { get; set; } = new();
        [JsonPropertyName("examples")] public object? Examples { get; set; }    // string[] or object[]
        [JsonPropertyName("method")] public object? Method { get; set; }         // string or List<string>
        [JsonPropertyName("rules")] public List<string>? Rules { get; set; }
        [JsonPropertyName("theorem")] public string? Theorem { get; set; }
        [JsonPropertyName("types")] public Dictionary<string, string>? Types { get; set; }
        [JsonPropertyName("cases")] public List<string>? Cases { get; set; }
        [JsonPropertyName("details")] public object? Details { get; set; }       // Dict<string,string> or nested obj
        [JsonPropertyName("note")] public string? Note { get; set; }
    }

    public class LiteratureProblem
    {
        [JsonPropertyName("problemId")] public string ProblemId { get; set; } = "";
        [JsonPropertyName("difficulty")] public string Difficulty { get; set; } = "Medium";
        [JsonPropertyName("type")] public string Type { get; set; } = "multiple_choice";
        [JsonPropertyName("question")] public string Question { get; set; } = "";
        [JsonPropertyName("options")] public List<string> Options { get; set; } = new();
        [JsonPropertyName("correctAnswer")] public string CorrectAnswer { get; set; } = "";
        [JsonPropertyName("solution")] public string Solution { get; set; } = "";
        [JsonPropertyName("explanation")] public string Explanation { set => Solution = value; }
        [JsonPropertyName("answer")] public string Answer { get; set; } = "";
        [JsonPropertyName("points")] public int Points { get; set; } = 10;
        [JsonPropertyName("hints")] public List<string>? Hints { get; set; }
        [JsonPropertyName("graphExpression")] public string? GraphExpression { get; set; }

        /// <summary>Returns the effective answer from either 'correctAnswer' or 'answer' field</summary>
        public string GetAnswer() =>
            !string.IsNullOrEmpty(CorrectAnswer) ? CorrectAnswer : Answer;
    }

    public class LiteratureCommonMistake
    {
        [JsonPropertyName("mistake")] public string Mistake { get; set; } = "";
        [JsonPropertyName("correction")] public string Correction { get; set; } = "";
    }
}
