using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualBasic.CompilerServices;
using timetable_telegram_bot.Models;

namespace timetable_telegram_bot.Services
{
    public class TimeTableService
    {
        public static async Task<LessonGroup> GetTimeTableByDayAsync(string dirStr, int course, int dayOfWeek)
        {
            int fac = Utils.GetFacultyNumByDirection(Configs.Directions[dirStr]);
            int dir = Utils.GetDirectionNumByDirectionName(Configs.Directions[dirStr]);

            var html = await GetHtmlTimetableByDay(course, dir, fac, dayOfWeek);
            var timetable = TimetableParser.ParseDayFromHtml(html);

            return timetable;
        }

        public static string GetTimetableTextByDay(LessonGroup timetable)
        {
            var sb = new StringBuilder();
            sb.AppendLine(timetable.Title);
            if (timetable.Lessons.Count == 0)
            {
                sb.AppendLine("Выходной");
            }
            else
            {
                foreach (var lesson in timetable.Lessons)
                {
                    sb.AppendLine(
                        $"\t{lesson.Num.ToString()}) {lesson.Time} {lesson.Name} ({lesson.Teacher} {lesson.Type}) {lesson.Classroom}");
                }
            }

            sb.AppendLine("=======================");
            return sb.ToString();
        }

        public static async Task<List<LessonGroup>> GetTimeTableAsync(string dirStr, int course)
        {
            var timetableList = new List<LessonGroup>();
            int fac = Utils.GetFacultyNumByDirection(Configs.Directions[dirStr]);
            int dir = Utils.GetDirectionNumByDirectionName(Configs.Directions[dirStr]);

            for (int i = 1; i <= 7; i++)
            {
                int day = i;
                var html = await GetHtmlTimetableByDay(course, dir, fac, day);
                var lessonGroup = TimetableParser.ParseDayFromHtml(html);
                timetableList.Add(lessonGroup);
            }

            return timetableList;
        }

        public static async Task<List<LessonGroup>> GetTeachersTimetable(string teacher)
        {
            var html = await GetTeachersTimetableHtml(teacher);
            var lessonGroup = TimetableParser.ParseTeachersTimetableFromHtml(html);
            return lessonGroup;
        }

        private static async Task<string> GetTeachersTimetableHtml(string teacher)
        {
            var uriBuilder = new UriBuilder(Configs.TimeTableUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["type"] = "teacher";
            query["search"] = teacher;
            uriBuilder.Query = query.ToString() ?? string.Empty;
            var url = uriBuilder.ToString();
            var htmlString = new WebClient().DownloadString(url);

            return await Task.FromResult(htmlString);
        }

        private static async Task<string> GetHtmlTimetableByDay(int course, int dir, int fac, int day)
        {
            var uriBuilder = new UriBuilder(Configs.TimeTableUrl);
            var query = HttpUtility.ParseQueryString(uriBuilder.Query);
            query["faculty"] = fac.ToString();
            query["direction"] = dir.ToString();
            query["course"] = course.ToString();
            query["day"] = day.ToString();
            uriBuilder.Query = query.ToString() ?? string.Empty;
            var url = uriBuilder.ToString();

            var htmlString = new WebClient().DownloadString(url);

            return await Task.FromResult(htmlString);
        }
    }
}