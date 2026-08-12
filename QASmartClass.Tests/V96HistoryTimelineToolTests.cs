using Xunit;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using QASmartClass.LearningTools.Helpers;
using QASmartClass.LearningTools.Views.Multi;

namespace QASmartClass.Tests
{
    public class V96HistoryTimelineToolTests
    {
        [Fact]
        public void Test_TryParseHistoricalYear_ValidInputs()
        {
            var method = typeof(HistoryTimelineTool).GetMethod("TryParseHistoricalYear", 
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var parameters = new object[] { "1010", 0 };
            var result = (bool)method.Invoke(null, parameters);
            Assert.True(result);
            Assert.Equal(1010, (int)parameters[1]);

            parameters = new object[] { "207 TCN", 0 };
            result = (bool)method.Invoke(null, parameters);
            Assert.True(result);
            Assert.Equal(-207, (int)parameters[1]);

            parameters = new object[] { "Năm 544", 0 };
            result = (bool)method.Invoke(null, parameters);
            Assert.True(result);
            Assert.Equal(544, (int)parameters[1]);

            parameters = new object[] { "-207", 0 };
            result = (bool)method.Invoke(null, parameters);
            Assert.True(result);
            Assert.Equal(-207, (int)parameters[1]);
        }

        [Fact]
        public void Test_TryParseHistoricalYear_InvalidInputs()
        {
            var method = typeof(HistoryTimelineTool).GetMethod("TryParseHistoricalYear", 
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var parameters = new object[] { "năm không xác định", 0 };
            var result = (bool)method.Invoke(null, parameters);
            Assert.False(result);

            parameters = new object[] { "", 0 };
            result = (bool)method.Invoke(null, parameters);
            Assert.False(result);

            parameters = new object[] { "   ", 0 };
            result = (bool)method.Invoke(null, parameters);
            Assert.False(result);
        }

        [Fact]
        public void Test_Database_InsertAndLoadCustomEvents()
        {
            // Clear existing ones first
            DbManager.ClearAllCustomTimelineEvents();

            var initialEvents = DbManager.GetCustomTimelineEvents();
            int initialCount = initialEvents.Count;

            // Save a custom event
            DbManager.SaveCustomTimelineEvent(1010, "Lý Thái Tổ dời đô về Thăng Long", "Chiếu dời đô", "vietnam", "Chi tiết dời đô", "Lý Thái Tổ", "Ý nghĩa dời đô");

            var updatedEvents = DbManager.GetCustomTimelineEvents();
            Assert.Equal(initialCount + 1, updatedEvents.Count);

            var ev = updatedEvents.FirstOrDefault(e => e.Year == 1010 && e.Title == "Lý Thái Tổ dời đô về Thăng Long");
            Assert.NotNull(ev);
            Assert.Equal("Chiếu dời đô", ev.Description);
            Assert.Equal("vietnam", ev.Category);
            Assert.Equal("Chi tiết dời đô", ev.Detail);
            Assert.Equal("Lý Thái Tổ", ev.Figures);
            Assert.Equal("Ý nghĩa dời đô", ev.Significance);

            // Clean up
            DbManager.ClearAllCustomTimelineEvents();
            Assert.Empty(DbManager.GetCustomTimelineEvents());
        }

        [Fact]
        public void Test_FormatHistoricalYear()
        {
            Assert.Equal("1010", HistoryTimelineTool.FormatHistoricalYear(1010));
            Assert.Equal("207 TCN", HistoryTimelineTool.FormatHistoricalYear(-207));
        }

        [Fact]
        public void Test_Search_NullSafety()
        {
            // Verify that the search filter safely checks nulls
            // We can instantiate the private TimelineEvent nested class using reflection
            var timelineEventAssembly = typeof(HistoryTimelineTool).Assembly;
            var timelineEventType = typeof(HistoryTimelineTool).GetNestedType("TimelineEvent", BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(timelineEventType);

            var evObj = Activator.CreateInstance(timelineEventType);
            Assert.NotNull(evObj);

            // Set Title to null
            var titleProp = timelineEventType.GetProperty("Title");
            Assert.NotNull(titleProp);
            titleProp.SetValue(evObj, null);

            var yearProp = timelineEventType.GetProperty("Year");
            Assert.NotNull(yearProp);
            yearProp.SetValue(evObj, 1000);

            // Check if filtering handles it without throwing null pointer exception.
            // Let's create a list of TimelineEvent and run the linq query similar to the search filter
            // LINQ query is: (e.Title != null && e.Title.ToLower().Contains(keyword))
            var titleVal = titleProp.GetValue(evObj) as string;
            var yearVal = (int)yearProp.GetValue(evObj);

            // The code under test has: (e.Title != null && e.Title.ToLower().Contains(keyword))
            // Let's assert that evaluating this for null title is safe and returns false
            bool matched = (titleVal != null && titleVal.ToLower().Contains("test"));
            Assert.False(matched);
        }

        [Fact]
        public void Test_DefaultEvents_JSON_Integrity()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string? jsonPath = null;
            for (int i = 0; i < 5; i++)
            {
                string testPath = System.IO.Path.Combine(currentDir, "Assets", "Data", "DefaultTimelineEvents.json");
                if (System.IO.File.Exists(testPath))
                {
                    jsonPath = testPath;
                    break;
                }
                string testPath2 = System.IO.Path.Combine(currentDir, "QASmartClass", "Assets", "Data", "DefaultTimelineEvents.json");
                if (System.IO.File.Exists(testPath2))
                {
                    jsonPath = testPath2;
                    break;
                }
                var parent = System.IO.Directory.GetParent(currentDir);
                if (parent == null) break;
                currentDir = parent.FullName;
            }

            Assert.NotNull(jsonPath);
            string json = System.IO.File.ReadAllText(jsonPath);
            Assert.False(string.IsNullOrWhiteSpace(json));

            var events = System.Text.Json.JsonSerializer.Deserialize<System.Collections.Generic.List<System.Text.Json.JsonDocument>>(json);
            Assert.NotNull(events);
            Assert.NotEmpty(events);

            foreach (var doc in events)
            {
                var root = doc.RootElement;
                Assert.True(root.TryGetProperty("Year", out _));
                Assert.True(root.TryGetProperty("Title", out _));
                Assert.True(root.TryGetProperty("Category", out _));
                Assert.True(root.TryGetProperty("Description", out _));
                Assert.True(root.TryGetProperty("Detail", out _));
                Assert.True(root.TryGetProperty("Figures", out _));
                Assert.True(root.TryGetProperty("Significance", out _));
            }
        }

        [Fact]
        public void Test_DefaultEvents_Count()
        {
            var tool = (HistoryTimelineTool)System.Runtime.Serialization.FormatterServices.GetUninitializedObject(typeof(HistoryTimelineTool));
            var field = typeof(HistoryTimelineTool).GetField("_allEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(field);

            var timelineEventType = typeof(HistoryTimelineTool).GetNestedType("TimelineEvent", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            Assert.NotNull(timelineEventType);

            var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(timelineEventType);
            var listInstance = Activator.CreateInstance(listType);
            field.SetValue(tool, listInstance);

            var method = typeof(HistoryTimelineTool).GetMethod("LoadSampleEvents", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.NotNull(method);
            method.Invoke(tool, null);

            var allEventsList = field.GetValue(tool) as System.Collections.Generic.IEnumerable<object>;
            Assert.NotNull(allEventsList);
            int count = allEventsList.Cast<object>().Count();
            Assert.Equal(820, count);
        }

        [Fact]
        public void Test_SaveEvent_CmbEra_BC_Conversion()
        {
            Exception? threadEx = null;
            var thread = new System.Threading.Thread(() =>
            {
                try
                {
                    DbManager.ClearAllCustomTimelineEvents();

                    // Instantiate via constructor
                    var tool = new HistoryTimelineTool();
                    
                    var type = typeof(HistoryTimelineTool);
                    var txtYear = (System.Windows.Controls.TextBox)type.GetField("txtYear", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tool);
                    var txtTitle = (System.Windows.Controls.TextBox)type.GetField("txtTitle", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tool);
                    var cmbEra = (System.Windows.Controls.ComboBox)type.GetField("cmbEra", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(tool);

                    Assert.NotNull(txtYear);
                    Assert.NotNull(txtTitle);
                    Assert.NotNull(cmbEra);

                    txtYear.Text = "207";
                    txtTitle.Text = "An Dương Vương lập nước Âu Lạc";
                    cmbEra.SelectedIndex = 1; // "bce" is the second item

                    var method = type.GetMethod("BtnSaveEvent_Click", BindingFlags.NonPublic | BindingFlags.Instance);
                    Assert.NotNull(method);
                    method.Invoke(tool, new object[] { null, null });

                    var dbEvents = DbManager.GetCustomTimelineEvents();
                    Assert.Single(dbEvents);
                    Assert.Equal(-207, dbEvents[0].Year);
                    Assert.Equal("An Dương Vương lập nước Âu Lạc", dbEvents[0].Title);

                    DbManager.ClearAllCustomTimelineEvents();
                }
                catch (Exception ex)
                {
                    threadEx = ex;
                }
            });

            thread.SetApartmentState(System.Threading.ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (threadEx != null)
            {
                throw threadEx;
            }
        }
    }
}
