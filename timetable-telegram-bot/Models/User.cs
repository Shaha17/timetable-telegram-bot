using System.ComponentModel.DataAnnotations;

namespace timetable_telegram_bot.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        public string Nickname { get; set; }
        public long ChatId { get; set; }
        public string SubscribedDir { get; set; }
        public int? SubscribedCourse { get; set; }
        public string Username { get; set; }
    }
}