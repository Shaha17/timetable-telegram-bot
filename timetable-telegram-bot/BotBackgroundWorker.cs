using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using timetable_telegram_bot.Context;
using timetable_telegram_bot.Services;
using timetable_telegram_bot.Models;

namespace timetable_telegram_bot
{
    public class BotBackgroundWorker
    {
        private readonly ITelegramBotClient _botClient;

        public BotBackgroundWorker(ITelegramBotClient bot)
        {
            _botClient = bot;
        }

        public async void Start()
        {
            while (true)
            {
                var hour = DateTime.Now.Hour;
                var min = DateTime.Now.Minute;
                var minToDelay = 60 - min;
                if (hour != 19)
                {
                    await Task.Delay(TimeSpan.FromMinutes(minToDelay));
                    continue;
                }

                var dayOfWeek = DateTime.Now.DayOfWeek;

                var subs = (await GetSubscribers()).GroupBy(u => u.SubscribedDir + " " + u.SubscribedCourse).ToList();
                if (dayOfWeek == DayOfWeek.Saturday)
                {
                }

                if (dayOfWeek == DayOfWeek.Sunday)
                {
                    //Todo send timetable for next week
                    await MassSendTimetableForNextWeek(subs);
                }
                else
                {
                    //Todo send timetable for next day
                    await MassSendTimetableForNextDay(subs);
                }

                await Task.Delay(TimeSpan.FromMinutes(minToDelay));
            }
        }

        private async Task MassSendTimetableForNextWeek(List<IGrouping<string, User>> subscribers)
        {
            int counter = 0;
            int currentFlow = 0;
            var tasks = new List<Task>();

            foreach (var group in subscribers)
            {
                var prms = group.Key.Split(" ");
                string dir = prms[0];
                int course = int.Parse(prms[1]);

                var timetable = await TimeTableService.GetTimeTableAsync(dir, course);
                timetable.ForEach(el =>
                    el.Lessons = el.Lessons.Where(l => !string.IsNullOrWhiteSpace(l.Name)).ToList());
                var sb = new StringBuilder();
                Console.WriteLine(
                    $"Рассылка расписания {Configs.Directions[dir]} {course.ToString()} на следующую неделю");
                sb.AppendLine($"Расписание {Configs.Directions[dir]} {course.ToString()} на следующую неделю")
                    .AppendLine();
                foreach (var lessonGroup in timetable)
                {
                    sb.AppendLine(TimeTableService.GetTimetableTextByDay(lessonGroup));

                    sb.AppendLine("\n=====================\n");
                }

                var text = sb.ToString();
                foreach (var user in group)
                {
                    if (counter / 30 != currentFlow)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        currentFlow++;
                    }

                    var task = SendMessageToChatIdAsync(user.ChatId, text);
                    tasks.Add(task);
                    counter++;
                }
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Рассылка на следующую неделю завершена");
        }

        private async Task MassSendTimetableForNextDay(List<IGrouping<string, User>> subscribers)
        {
            int counter = 0;
            int currentFlow = 0;
            int dayOfWeek = (int) DateTime.Now.DayOfWeek + 1;
            var tasks = new List<Task>();

            foreach (var group in subscribers)
            {
                var prms = group.Key.Split(" ");
                string dir = prms[0];
                int course = int.Parse(prms[1]);

                var timetable = await TimeTableService.GetTimeTableByDayAsync(dir, course, dayOfWeek);
                timetable.Lessons = timetable.Lessons.Where(l => !string.IsNullOrWhiteSpace(l.Name)).ToList();
                var sb = new StringBuilder();
                Console.WriteLine(
                    $"Рассылка расписания {Configs.Directions[dir]} {course.ToString()} на {DateTime.Now.AddDays(1).ToString("dd/MM/yyyy")}");
                sb.AppendLine(
                        $"Расписание {Configs.Directions[dir]} {course.ToString()} на {DateTime.Now.AddDays(1).ToString("dd/MM/yyyy")}")
                    .AppendLine();
                sb.AppendLine(TimeTableService.GetTimetableTextByDay(timetable));

                var text = sb.ToString();
                foreach (var user in group)
                {
                    if (counter / 30 != currentFlow)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));
                        currentFlow++;
                    }

                    var task = SendMessageToChatIdAsync(user.ChatId, text);
                    tasks.Add(task);
                    counter++;
                }
            }

            Task.WaitAll(tasks.ToArray());
            Console.WriteLine("Рассылка на завтра завершена");
        }

        private static async Task<List<Models.User>> GetSubscribers()
        {
            try
            {
                await using var ctx = new TgBotContext();
                return await ctx.Users.Where(u => u.SubscribedDir != null && u.SubscribedCourse != null).ToListAsync();
            }
            catch (Exception e)
            {
                return new List<User>();
            }
        }

        private async Task SendMessageToChatIdAsync(long chatId, string text)
        {
            var task = await _botClient.SendTextMessageAsync(chatId, text);
        }
    }
}