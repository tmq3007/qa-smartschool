using System;
using System.Collections.Generic;
using System.Text.Json;
using QASmartClass.Classroom.Views;
using Serilog;

namespace QASmartClass.Helpers
{
    public static class QuizAnswerParser
    {
        public static List<AnswerDetail> ParseAnswers(string json)
        {
            var list = new List<AnswerDetail>();
            if (string.IsNullOrWhiteSpace(json))
                return list;

            string trimmed = json.Trim();

            try
            {
                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    // Case A: JSON Array (List of objects or List of string logs)
                    using (var doc = JsonDocument.Parse(trimmed))
                    {
                        var root = doc.RootElement;
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in root.EnumerateArray())
                            {
                                if (element.ValueKind == JsonValueKind.Object)
                                {
                                    var detail = ParseJsonElementToDetail(element);
                                    list.Add(detail);
                                }
                                else if (element.ValueKind == JsonValueKind.String)
                                {
                                    // Format 3: List of string logs like "Q1: A (Dung) — Content"
                                    string val = element.GetString() ?? "";
                                    var detail = ParseStringLog(val);
                                    if (detail != null)
                                        list.Add(detail);
                                }
                            }
                        }
                    }
                }
                else if (trimmed.StartsWith("{") && trimmed.EndsWith("}"))
                {
                    // Case B: JSON Object (Single AnswerDetail object or Dictionary<string, string>)
                    using (var doc = JsonDocument.Parse(trimmed))
                    {
                        var root = doc.RootElement;
                        bool isSingleDetail = root.TryGetProperty("QuestionId", out _) || 
                                              root.TryGetProperty("questionId", out _) ||
                                              root.TryGetProperty("Answer", out _) ||
                                              root.TryGetProperty("answer", out _);
                        if (isSingleDetail)
                        {
                            var detail = ParseJsonElementToDetail(root);
                            list.Add(detail);
                        }
                        else
                        {
                            // Format 1: Dictionary<string, string/int> mapping question.Id.ToString() or index -> student answer
                            bool isZeroBased = root.TryGetProperty("0", out _);
                            foreach (var prop in root.EnumerateObject())
                            {
                                if (int.TryParse(prop.Name, out int qId))
                                {
                                    int targetQId = isZeroBased ? (qId + 1) : qId;
                                    string ansVal = "";
                                    if (prop.Value.ValueKind == JsonValueKind.String)
                                    {
                                        ansVal = prop.Value.GetString() ?? "";
                                    }
                                    else if (prop.Value.ValueKind == JsonValueKind.Number)
                                    {
                                        int idxVal = prop.Value.GetInt32();
                                        if (idxVal >= 0 && idxVal < 26)
                                            ansVal = ((char)('A' + idxVal)).ToString();
                                    }

                                    list.Add(new AnswerDetail
                                    {
                                        QuestionId = targetQId,
                                        Answer = ansVal,
                                        Correct = false, // Recalculated on-the-fly in UI
                                        Points = 0,
                                        TimeTaken = 0
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warning("QuizAnswerParser: Error parsing answers JSON. Msg: {Err}. Data: {Raw}", ex.Message, json);
            }

            return list;
        }

        private static AnswerDetail ParseJsonElementToDetail(JsonElement element)
        {
            var detail = new AnswerDetail();

            if (element.TryGetProperty("QuestionId", out var qidProp) || element.TryGetProperty("questionId", out qidProp))
            {
                if (qidProp.ValueKind == JsonValueKind.Number)
                    detail.QuestionId = qidProp.GetInt32();
                else if (qidProp.ValueKind == JsonValueKind.String && int.TryParse(qidProp.GetString(), out int qid))
                    detail.QuestionId = qid;
            }

            if (element.TryGetProperty("Answer", out var ansProp) || element.TryGetProperty("answer", out ansProp))
            {
                if (ansProp.ValueKind == JsonValueKind.String)
                {
                    detail.Answer = ansProp.GetString() ?? "";
                }
                else if (ansProp.ValueKind == JsonValueKind.Number)
                {
                    int idxVal = ansProp.GetInt32();
                    if (idxVal >= 0 && idxVal < 26)
                        detail.Answer = ((char)('A' + idxVal)).ToString();
                }
            }

            if (element.TryGetProperty("Correct", out var corProp) || element.TryGetProperty("correct", out corProp))
            {
                if (corProp.ValueKind == JsonValueKind.True || corProp.ValueKind == JsonValueKind.False)
                    detail.Correct = corProp.GetBoolean();
                else if (corProp.ValueKind == JsonValueKind.String && bool.TryParse(corProp.GetString(), out bool cor))
                    detail.Correct = cor;
            }

            if (element.TryGetProperty("Points", out var ptsProp) || element.TryGetProperty("points", out ptsProp))
            {
                if (ptsProp.ValueKind == JsonValueKind.Number)
                    detail.Points = ptsProp.GetInt32();
                else if (ptsProp.ValueKind == JsonValueKind.String && int.TryParse(ptsProp.GetString(), out int pts))
                    detail.Points = pts;
            }

            if (element.TryGetProperty("TimeTaken", out var timeProp) || element.TryGetProperty("timeTaken", out timeProp))
            {
                if (timeProp.ValueKind == JsonValueKind.Number)
                    detail.TimeTaken = timeProp.GetInt32();
                else if (timeProp.ValueKind == JsonValueKind.String && int.TryParse(timeProp.GetString(), out int time))
                    detail.TimeTaken = time;
            }

            return detail;
        }

        private static AnswerDetail? ParseStringLog(string val)
        {
            try
            {
                if (val.StartsWith("Q") && val.Contains(":"))
                {
                    int colonIdx = val.IndexOf(':');
                    string qNumStr = val.Substring(1, colonIdx - 1);
                    if (int.TryParse(qNumStr, out int qNum))
                    {
                        string rest = val.Substring(colonIdx + 1).Trim();
                        string ans = "";
                        bool correct = false;
                        if (rest.Length > 0)
                        {
                            ans = rest.Substring(0, 1);
                            correct = rest.Contains("(Dung)") || rest.Contains("(Correct)") || rest.Contains("(Chính xác)");
                        }
                        return new AnswerDetail
                        {
                            QuestionId = qNum,
                            Answer = ans,
                            Correct = correct,
                            Points = 0,
                            TimeTaken = 0
                        };
                    }
                }
            }
            catch { }
            return null;
        }

        public static string NormalizeShortAnswer(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string s = input.ToLower();
            char[] punctuation = new char[] { '.', ',', '?', '!', ':', '\"', '\'' };
            foreach (var p in punctuation) s = s.Replace(p.ToString(), "");
            var parts = s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return string.Join(" ", parts).Trim();
        }

        public static string RemoveVietnameseDiacritics(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string[] arr1 = new string[] { "á", "à", "ả", "ã", "ạ", "â", "ấ", "ầ", "ẩ", "ẫ", "ậ", "ă", "ắ", "ằ", "ẳ", "ẵ", "ặ",
                "đ", "é", "è", "ẻ", "ẽ", "ẹ", "ê", "ế", "ề", "ể", "ễ", "ệ", "í", "ì", "ỉ", "ĩ", "ị",
                "ó", "ò", "ỏ", "õ", "ọ", "ô", "ố", "ồ", "ổ", "ỗ", "ộ", "ơ", "ớ", "ờ", "ở", "ỡ", "ợ",
                "ú", "ù", "ủ", "ũ", "ụ", "ư", "ứ", "ừ", "ử", "ữ", "ự", "ý", "ỳ", "ỷ", "ỹ", "ỵ" };
            string[] arr2 = new string[] { "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a", "a",
                "d", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "e", "i", "i", "i", "i", "i",
                "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o", "o",
                "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "u", "y", "y", "y", "y", "y" };
            string result = input;
            for (int i = 0; i < arr1.Length; i++)
            {
                result = result.Replace(arr1[i], arr2[i]);
                result = result.Replace(arr1[i].ToUpper(), arr2[i].ToUpper());
            }
            return result;
        }

        public static bool IsNaturalScienceSubject(string subject)
        {
            if (string.IsNullOrEmpty(subject)) return false;
            string subj = subject.Trim().ToLower();
            return subj == "toán" || subj == "vật lý" || subj == "hóa học" || subj == "sinh học" || subj == "tin học" ||
                   subj == "toan" || subj == "vat ly" || subj == "hoa hoc" || subj == "sinh hoc" || subj == "tin hoc" ||
                   subj == "math" || subj == "physics" || subj == "chemistry" || subj == "biology" || subj == "it" || subj == "computer science" ||
                   subj == "khoa học tự nhiên" || subj == "khtn" || subj == "vật lí" || subj == "toán học";
        }

        public static bool GradeAnswer(string studentAns, string correctAnswer, string questionType, string subject = "")
        {
            if (string.IsNullOrEmpty(correctAnswer)) return false;
            string qType = (questionType ?? "").ToLower();
            
            bool ignoreAccents = IsNaturalScienceSubject(subject);
            
            if (qType == "short" || qType == "shortanswer" || qType == "short_answer")
            {
                string sAns = NormalizeShortAnswer(studentAns);
                string cAns = NormalizeShortAnswer(correctAnswer);
                if (ignoreAccents)
                {
                    sAns = RemoveVietnameseDiacritics(sAns);
                    cAns = RemoveVietnameseDiacritics(cAns);
                }
                return sAns == cAns;
            }
            else if (qType == "fib" || qType == "fillblank" || qType == "fill_blank")
            {
                var studentParts = studentAns.Split(';');
                var correctParts = correctAnswer.Split(';');
                if (studentParts.Length != correctParts.Length) return false;
                for (int i = 0; i < studentParts.Length; i++)
                {
                    string sPart = NormalizeShortAnswer(studentParts[i]);
                    string cPart = NormalizeShortAnswer(correctParts[i]);
                    if (ignoreAccents)
                    {
                        sPart = RemoveVietnameseDiacritics(sPart);
                        cPart = RemoveVietnameseDiacritics(cPart);
                    }
                    if (sPart != cPart)
                        return false;
                }
                return true;
            }
            else if (qType == "ordering" || qType == "order")
            {
                return GradeOrderingAnswer(studentAns, correctAnswer);
            }
            else if (qType == "matching" || qType == "match")
            {
                return GradeMatchingAnswer(studentAns, correctAnswer);
            }
            
            string sFinal = studentAns.Trim();
            string cFinal = correctAnswer.Trim();
            if (ignoreAccents)
            {
                sFinal = RemoveVietnameseDiacritics(sFinal);
                cFinal = RemoveVietnameseDiacritics(cFinal);
            }
            return sFinal.Equals(cFinal, StringComparison.OrdinalIgnoreCase);
        }

        private static bool GradeOrderingAnswer(string studentAns, string correctAnswer)
        {
            string sNorm = NormalizeOrderString(studentAns);
            string cNorm = NormalizeOrderString(correctAnswer);
            return sNorm == cNorm;
        }

        private static string NormalizeOrderString(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string s = input.Replace("→", "|").Replace("->", "|").Replace("=>", "|");
            var parts = s.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
            var cleanParts = new List<string>();
            foreach (var p in parts)
            {
                string clean = p.Trim().ToLower();
                if (!string.IsNullOrEmpty(clean)) cleanParts.Add(clean);
            }
            return string.Join("|", cleanParts);
        }

        private static bool GradeMatchingAnswer(string studentAns, string correctAnswer)
        {
            var studentPairs = ParseMatchingPairs(studentAns);
            var correctPairs = ParseMatchingPairs(correctAnswer);

            if (studentPairs.Count != correctPairs.Count || studentPairs.Count == 0) return false;

            foreach (var kvp in correctPairs)
            {
                if (!studentPairs.TryGetValue(kvp.Key, out string studentVal) || 
                    !studentVal.Equals(kvp.Value, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
            return true;
        }

        private static Dictionary<string, string> ParseMatchingPairs(string input)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(input)) return dict;

            string cleanInput = input;
            if (cleanInput.StartsWith("MATCH:", StringComparison.OrdinalIgnoreCase))
            {
                cleanInput = cleanInput.Substring("MATCH:".Length);
            }

            var pairs = cleanInput.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var parts = pair.Split(new[] { "->" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string key = parts[0].Trim().ToLower();
                    string val = parts[1].Trim().ToLower();
                    if (!string.IsNullOrEmpty(key))
                    {
                        dict[key] = val;
                    }
                }
            }
            return dict;
        }
    }
}
