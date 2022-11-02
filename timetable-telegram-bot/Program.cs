using System;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;

namespace timetable_telegram_bot
{
    class Program
    {
        static void Main(string[] args)
        {
            var bot = new Bot();
            bot.Start();
            Console.WriteLine("Запущен бот " + bot.GetBotInstance().GetMeAsync().Result.FirstName);
            
            Thread botTasksThread = new Thread(ThreadForBotTasks);
            var botClient = bot.GetBotInstance();
            botTasksThread.Start(botClient);
            
            Console.ReadLine();
        }
        static void ThreadForBotTasks(object bot)
        {
            if (bot is not ITelegramBotClient botClient) return;

            new BotBackgroundWorker(botClient).Start();
        }
    }
}