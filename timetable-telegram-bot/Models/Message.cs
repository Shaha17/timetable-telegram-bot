using System;

namespace timetable_telegram_bot.Models
{
    public class Message
    {
        public long Id { get; set; }
        public DateTime Date { get; set; }
        public string Fullname { get; set; }
        public string Username { get; set; }
        public long ChatId { get; set; }
        public string Text { get; set; }
        
        
    }
}