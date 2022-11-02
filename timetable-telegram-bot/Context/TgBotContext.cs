using Microsoft.EntityFrameworkCore;
using timetable_telegram_bot.Models;
using User = timetable_telegram_bot.Models.User;

namespace timetable_telegram_bot.Context
{
    public class TgBotContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Message> Messages { get; set; }

        public TgBotContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // optionsBuilder.UseSqlite("Data source=TgBot.db");
            optionsBuilder.UseSqlite(
                "Data source=/Users/shahzod/RiderProjects/timetable-telegram-bot/timetable-telegram-bot/TgBot.db");
        }
    }
}