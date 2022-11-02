namespace timetable_telegram_bot.Models
{
    public class Lesson
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Teacher { get; set; }
        public int Num { get; set; }
        public string Classroom { get; set; }
        public string Type { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
    }
}