using System.Collections.Generic;

namespace timetable_telegram_bot.Models
{
    public class LessonGroup
    {
        public string Title { get; set; }
        public string ShortTitle { get; set; }
        public List<Lesson> Lessons { get; set; }
    }
}