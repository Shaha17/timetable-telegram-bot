using System.Collections.Generic;
using System.Linq;
using Fizzler.Systems.HtmlAgilityPack;
using HtmlAgilityPack;
using Humanizer;
using timetable_telegram_bot.Models;

namespace timetable_telegram_bot.Services
{
    public static class TimetableParser
    {
        public static List<LessonGroup> ParseTeachersTimetableFromHtml(string html)
        {
            var lessonGroups = new List<LessonGroup>();
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var doc = htmlDoc.DocumentNode;
            var divWithTable = doc.QuerySelector(".timetable");
            var tableBody = divWithTable.ChildNodes.Single(n => n.Name == "table");

            {
                //temp data
                LessonGroup lessonGroup = null;
                Lesson lesson = null;
                string date = string.Empty, dayOfWeek = string.Empty;

                var tableChilds = tableBody.ChildNodes.Where(n => n.Name != "#text");
                foreach (var node in tableChilds)
                {
                    if (node.HasClass("tm_day"))
                    {
                        if (lessonGroup != null)
                        {
                            if (lesson != null)
                            {
                                lessonGroup.Lessons.Add(lesson);
                            }
                            lessonGroup.Title = $"{dayOfWeek} ({date})";
                            lessonGroup.Lessons.ForEach(l => l.Date = $"{dayOfWeek} ({date})");
                            lessonGroups.Add(lessonGroup);
                            lesson = null;
                        }

                        lessonGroup = new LessonGroup();
                        dayOfWeek = node.InnerText.Trim().Humanize(LetterCasing.Title);
                    }

                    if (node.HasClass("tm_date"))
                    {
                        date = node.InnerText.Trim().Humanize(LetterCasing.Title);
                    }

                    if (node.HasClass("tm_content") && !node.HasClass("tm_content_mb"))
                    {
                        if (lesson != null)
                        {
                            lessonGroup.Lessons.Add(lesson);
                        }

                        var childNodes = node.ChildNodes.Where(n => !string.IsNullOrWhiteSpace(n.InnerText.Trim()))
                            .ToArray();
                        lesson = new Lesson();
                        lesson.Num =
                            int.Parse(node.ChildNodes.FirstOrDefault(n => n.Name == "td")?.FirstChild.InnerText);
                        lesson.Time = node.QuerySelector(".tm_time").InnerText.Trim().Replace("&#8209;", "-");
                        var teachers = childNodes[1].InnerText.Trim().Split(",").Select(x => x.Trim() + ".")
                            .GroupBy(x => x).Select(x => x.Key).ToArray();
                        var teachersStr = string.Join(", ", teachers);
                        var group = childNodes[2].InnerText.Trim();
                        var course = childNodes[3].InnerText.Trim();
                        lesson.Teacher = $"{teachersStr} | {group} {course}";
                        lesson.Name = childNodes[4].InnerText.Trim();
                        lesson.Type = childNodes[5].InnerText.Trim();
                        lesson.Classroom = childNodes[6].InnerText.Trim();
                    }
                }

                if (lessonGroup != null)
                {
                    if (lesson != null)
                    {
                        lessonGroup.Lessons.Add(lesson);
                    }
                    lessonGroup.Title = $"{dayOfWeek} ({date})";
                    lessonGroup.Lessons.ForEach(l => l.Date = $"{dayOfWeek} ({date})");
                    lessonGroups.Add(lessonGroup);
                }
            }

            return lessonGroups;
        }

        public static LessonGroup ParseDayFromHtml(string html)
        {
            var lessonGroup = new LessonGroup()
            {
                Lessons = new()
            };
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var doc = htmlDoc.DocumentNode;

            var dayOfWeek = doc.QuerySelector(".tm_day").InnerText.Trim().Humanize(LetterCasing.Title);
            var date = doc.QuerySelector(".tm_date").InnerText.Trim().Humanize(LetterCasing.Title);
            lessonGroup.Title = $"{dayOfWeek} ({date})";
            var lessonNodes = doc.QuerySelectorAll(".tm_content_mb");
            foreach (var lessonNode in lessonNodes)
            {
                var lesson = new Lesson();
                var numNode = lessonNode.QuerySelector(".couple");
                lesson.Num = int.Parse(lessonNode.QuerySelector(".couple").InnerText.Trim());
                var timeNode = lessonNode.QuerySelector(".tm_time");
                lesson.Time = lessonNode.QuerySelector(".tm_time").InnerText.Trim().Replace("&#8209;", "-");
                var nameNode = lessonNode.QuerySelector(".subject");
                lesson.Name = lessonNode.QuerySelector(".subject").InnerText.Trim();
                if (lesson.Name.Length == 0) lesson.Name = null;

                var arr = nameNode.NextSibling.InnerText.Trim().Split("•");
                if (arr.Length >= 3)
                {
                    var teachers = arr[0].Trim().Split(",").Select(x => x.Trim() + ".")
                        .GroupBy(x => x).Select(x => x.Key).ToArray();
                    lesson.Teacher = string.Join(", ", teachers);
                    lesson.Type = arr[1].Trim().Humanize(LetterCasing.Title);
                    var classRooms = arr[2].Trim().Split(",").Select(x => x.Trim())
                        .GroupBy(x => x).Select(x => x.Key).ToArray();
                    lesson.Classroom = string.Join(", ", classRooms);
                }

                lesson.Date = lessonGroup.Title;
                lessonGroup.Lessons.Add(lesson);
            }

            return lessonGroup;
        }
    }
}