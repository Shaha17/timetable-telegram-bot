using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Telegram.Bot;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using timetable_telegram_bot.Context;
using timetable_telegram_bot.Services;
using timetable_telegram_bot.Types;

namespace timetable_telegram_bot
{
    public class Bot
    {
        private const long AdminChatId = 311766503;
        private readonly ITelegramBotClient _bot;

        public Bot()
        {
            const string botToken = "5335802407:AAEfoIahmspFothQe8cSWo_oH_x13AM26Ik";
            _bot = new TelegramBotClient(botToken);
        }

        public Bot(string token)
        {
            _bot = new TelegramBotClient(token);
        }

        public ITelegramBotClient GetBotInstance()
        {
            return _bot;
        }

        public void Start()
        {
            var cts = new CancellationTokenSource();
            var cancellationToken = cts.Token;
            var receiverOptions = new ReceiverOptions();
            _bot.StartReceiving(
                HandleUpdateAsync,
                HandleErrorAsync,
                receiverOptions,
                cancellationToken
            );
        }

        private static async Task RegisterUserAsync(User user, long chatId)
        {
            await using var ctx = new TgBotContext();
            if (await ctx.Users.AnyAsync(u => u.ChatId == chatId)) return;

            var usr = new Models.User
            {
                Nickname = user.FirstName + " " + user.LastName,
                Username = user.Username,
                ChatId = chatId
            };
            ctx.Users.Add(usr);
            await ctx.SaveChangesAsync();
        }

        private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update,
            CancellationToken cancellationToken)
        {
            // Console.WriteLine(Newtonsoft.Json.JsonConvert.SerializeObject(update.Message.Text));
            if (update.Type == UpdateType.Message)
            {
                Console.WriteLine(
                    $"{DateTime.Now:yyyy/MM/dd HH:mm:ss} {update.Message?.From?.FirstName} {update.Message?.From?.LastName} ({update.Message?.From?.Username} {update.Message?.Chat.Id.ToString()}): {update.Message?.Text}");
                var mess = new Models.Message
                {
                    Date = DateTime.Now,
                    Fullname = $"{update.Message?.From?.FirstName} {update.Message?.From?.LastName}",
                    Username = update.Message?.From?.Username,
                    ChatId = update.Message!.Chat.Id,
                    Text = update.Message.Text
                };
                await using var ctx = new TgBotContext();
                var addMessageTask = ctx.Messages.AddAsync(mess, cancellationToken);
                await HandleMessage(botClient, update.Message);
                await addMessageTask;
                await ctx.SaveChangesAsync(cancellationToken);
                return;
            }

            if (update.Type == UpdateType.CallbackQuery)
                try
                {
                    await HandleCallbackQuery(botClient, update.CallbackQuery);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
        }

        private static async Task HandleMessage(ITelegramBotClient botClient, Message message)
        {
            if (string.IsNullOrWhiteSpace(message.Text)) return;
            RegisterUserAsync(message.From, message.Chat.Id);

            var messageText = message.Text;
            if (messageText == MessageCommands.START)
            {
                const string text = "Выберите действие";
                var ikm = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Получить расписание",
                            $"{MessageCommands.GET_TIMETABLE}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Подписаться", $"{MessageCommands.SUBSCRIBE}")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("Отписаться", $"{MessageCommands.UNSUBSCRIBE}")
                    }
                });
                await botClient.SendTextMessageAsync(message.Chat, text, replyMarkup: ikm);
                return;
            }

            if (message.Chat.Id == AdminChatId)
                // if (messageText.StartsWith("sendautodelete"))
                // {
                //     var prms = messageText.Split(" ");
                //     var sendTo = prms[1];
                //     var timeInSeconds = int.Parse(prms[2]);
                //     var text = string.Join(" ", prms.TakeLast(prms.Length - 3));
                //     var msgToDelete = await botClient.SendTextMessageAsync(sendTo, text);
                //
                //     await Task.Delay(timeInSeconds * 1000);
                //     await botClient.DeleteMessageAsync(msgToDelete.Chat.Id, msgToDelete.MessageId);
                //     return;
                // }

                if (messageText.StartsWith("send"))
                {
                    var prms = messageText.Split(" ");
                    var sendTo = prms[1];
                    var text = string.Join(" ", prms.TakeLast(prms.Length - 2));
                    await botClient.SendTextMessageAsync(sendTo, text);
                }

            // await botClient.SendTextMessageAsync(message.Chat, $"Набери {MessageCommands.START}");
        }

        private static async Task HandleCallbackQuery(ITelegramBotClient botClient, CallbackQuery callbackQuery)
        {
            var chatId = callbackQuery.Message!.Chat.Id;
            if (callbackQuery.Data!.StartsWith(MessageCommands.SUBSCRIBE))
            {
                var param = callbackQuery.Data.Split(" ").ToArray();
                if (param.Length == 1)
                {
                    var text = "Выберите курс";
                    var imkArr = new List<InlineKeyboardButton>();
                    imkArr.ToArray();
                    var imk = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("1", $"{callbackQuery.Data} 1"),
                            InlineKeyboardButton.WithCallbackData("2", $"{callbackQuery.Data} 2")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("3", $"{callbackQuery.Data} 3"),
                            InlineKeyboardButton.WithCallbackData("4", $"{callbackQuery.Data} 4")
                        }
                    });
                    var messageToEdit = callbackQuery.Message!.MessageId;

                    if (messageToEdit <= 0)
                        await botClient.SendTextMessageAsync(chatId, text, replyMarkup: imk);
                    else
                        await botClient.EditMessageTextAsync(chatId,
                            messageToEdit, text, replyMarkup: imk);
                    return;
                }

                if (param.Length == 2)
                {
                    var text = "Выберите направление";
                    var humBtns = Configs.HumanitiesDirectionsShort.Select(d => InlineKeyboardButton.WithCallbackData(
                        Configs.Directions[d],
                        $"{callbackQuery.Data} {d}"));
                    var natBtns = Configs.NaturalScienceDirectionsShort.Select(d =>
                        InlineKeyboardButton.WithCallbackData(
                            Configs.Directions[d],
                            $"{callbackQuery.Data} {d}"));
                    var imk = new InlineKeyboardMarkup(new[]
                    {
                        natBtns,
                        humBtns
                    });
                    var messageToEdit = callbackQuery.Message!.MessageId;

                    if (messageToEdit <= 0)
                        await botClient.SendTextMessageAsync(chatId, text, replyMarkup: imk);
                    else
                        await botClient.EditMessageTextAsync(chatId,
                            messageToEdit, text, replyMarkup: imk);

                    return;
                }

                if (param.Length == 3)
                {
                    using (var ctx = new TgBotContext())
                    {
                        var user = new Models.User
                        {
                            Username = callbackQuery.From.Username,
                            Nickname = $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}",
                            ChatId = callbackQuery.Message!.Chat.Id,
                            SubscribedCourse = int.Parse(param[1]),
                            SubscribedDir = param[2]
                        };
                        var usr = await ctx.Users.FirstOrDefaultAsync(u => u.ChatId == user.ChatId);
                        var usrPrefix = Utils.GetUserPrefix(callbackQuery.From, callbackQuery.Message);
                        if (usr == null)
                        {
                            ctx.Users.Add(user);
                            Console.WriteLine($"{usrPrefix} подписался на рассылку {param[2]} {param[1]}");
                        }
                        else
                        {
                            usr.Username = user.Username;
                            usr.Nickname = user.Nickname;
                            usr.SubscribedCourse = user.SubscribedCourse;
                            usr.SubscribedDir = user.SubscribedDir;
                            Console.WriteLine($"{usrPrefix} обновил подписку рассылки на {param[2]} {param[1]}");
                        }


                        await ctx.SaveChangesAsync();
                    }


                    var text = $"Вы успешно подписаны на {Configs.Directions[param[2]]} {param[1]}";

                    var messageToEdit = callbackQuery.Message!.MessageId;

                    if (messageToEdit <= 0)
                        await botClient.SendTextMessageAsync(chatId, text);
                    else
                        await botClient.EditMessageTextAsync(chatId,
                            messageToEdit, text);

                    return;
                }
            }

            if (callbackQuery.Data!.StartsWith(MessageCommands.UNSUBSCRIBE))
            {
                using (var ctx = new TgBotContext())
                {
                    var usr = await ctx.Users.FirstOrDefaultAsync(u => u.ChatId == chatId);
                    if (usr != null)
                    {
                        usr.SubscribedCourse = null;
                        usr.SubscribedDir = null;
                        await ctx.SaveChangesAsync();
                    }
                }

                var text = "Вы успешно отписаны";
                var usrPrefix = Utils.GetUserPrefix(callbackQuery.From, callbackQuery.Message);
                Console.WriteLine($"{usrPrefix} отписался от рассылки");
                var messageToEdit = callbackQuery.Message!.MessageId;

                if (messageToEdit <= 0)
                    await botClient.SendTextMessageAsync(callbackQuery.Message!.Chat.Id, text);
                else
                    await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id,
                        callbackQuery.Message.MessageId, text);
            }

            if (callbackQuery.Data!.StartsWith(MessageCommands.GET_TIMETABLE))
            {
                var param = callbackQuery.Data.Split(" ").ToArray();
                if (param.Length == 1)
                {
                    var text = "Выберите курс";
                    var imk = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("1", $"{callbackQuery.Data} 1"),
                            InlineKeyboardButton.WithCallbackData("2", $"{callbackQuery.Data} 2")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("3", $"{callbackQuery.Data} 3"),
                            InlineKeyboardButton.WithCallbackData("4", $"{callbackQuery.Data} 4")
                        }
                    });
                    var messageToEdit = callbackQuery.Message!.MessageId;

                    if (messageToEdit <= 0)
                        await botClient.SendTextMessageAsync(chatId, text, replyMarkup: imk);
                    else
                        await botClient.EditMessageTextAsync(chatId,
                            messageToEdit, text, replyMarkup: imk);

                    return;
                }

                if (param.Length == 2)
                {
                    var text = "Выберите направление";
                    var humBtns = Configs.HumanitiesDirectionsShort.Select(d => InlineKeyboardButton.WithCallbackData(
                        Configs.Directions[d],
                        $"{callbackQuery.Data} {d}"));
                    var natBtns = Configs.NaturalScienceDirectionsShort.Select(d =>
                        InlineKeyboardButton.WithCallbackData(
                            Configs.Directions[d],
                            $"{callbackQuery.Data} {d}"));
                    var imk = new InlineKeyboardMarkup(new[]
                    {
                        natBtns,
                        humBtns
                    });
                    var messageToEdit = callbackQuery.Message!.MessageId;

                    if (messageToEdit <= 0)
                        await botClient.SendTextMessageAsync(chatId, text, replyMarkup: imk);
                    else
                        await botClient.EditMessageTextAsync(chatId,
                            messageToEdit, text, replyMarkup: imk);

                    return;
                }

                if (param.Length == 3)
                {
                    var text = "Выберите";
                    var imk = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Расписание на неделю", $"{callbackQuery.Data} week")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("Расписание на сегодня",
                                $"{callbackQuery.Data} day")
                        }
                    });
                    var messageToEdit = callbackQuery.Message!.MessageId;

                    if (messageToEdit <= 0)
                        await botClient.SendTextMessageAsync(chatId, text, replyMarkup: imk);
                    else
                        await botClient.EditMessageTextAsync(chatId,
                            messageToEdit, text, replyMarkup: imk);

                    return;
                }

                if (param.Length == 4)
                {
                    var day = param[3];
                    var dirStr = param[2];
                    var course = int.Parse(param[1]);

                    if (day == "week")
                    {
                        await SendTimetableForWeek(botClient, dirStr, course, callbackQuery);
                    }
                    else if (day == "day")
                    {
                        var dayOfWeek = (int) DateTime.Now.DayOfWeek;
                        await SendTimetableForDay(botClient, dayOfWeek, dirStr, course, callbackQuery);
                    }
                }
            }
        }

        private static async Task SendTimetableForDay(ITelegramBotClient botClient, int dayOfWeek, string dirStr,
            int course, CallbackQuery callbackQuery)
        {
            var timetable = await TimeTableService.GetTimeTableByDayAsync(dirStr, course, dayOfWeek);
            timetable.Lessons = timetable.Lessons.Where(l => !string.IsNullOrWhiteSpace(l.Name)).ToList();

            var usrPrefix = Utils.GetUserPrefix(callbackQuery.From, callbackQuery.Message);
            Console.WriteLine(
                $"{usrPrefix} запросил(а) Расписание {Configs.Directions[dirStr]} {course.ToString()} на {DateTime.Now.ToString("dd/MM/yyyy")}");

            var sb = new StringBuilder();
            sb.AppendLine(
                    $"Расписание {Configs.Directions[dirStr]} {course.ToString()} на {DateTime.Now.ToString("dd/MM/yyyy")}")
                .AppendLine();
            sb.AppendLine(TimeTableService.GetTimetableTextByDay(timetable));

            var text = sb.ToString();
            var messageToEdit = callbackQuery.Message!.MessageId;

            if (messageToEdit <= 0)
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text);
            else
                await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id,
                    messageToEdit, text);
        }

        private static async Task SendTimetableForWeek(ITelegramBotClient botClient, string dirStr,
            int course, CallbackQuery callbackQuery)
        {
            var timetable = await TimeTableService.GetTimeTableAsync(dirStr, course);
            timetable.ForEach(el =>
                el.Lessons = el.Lessons.Where(l => !string.IsNullOrWhiteSpace(l.Name)).ToList());
            var sb = new StringBuilder();
            var usrPrefix = Utils.GetUserPrefix(callbackQuery.From, callbackQuery.Message);
            Console.WriteLine(
                $"{usrPrefix} запросил(а) Расписание {Configs.Directions[dirStr]} {course.ToString()}");
            sb.AppendLine($"Расписание {Configs.Directions[dirStr]} {course.ToString()}").AppendLine();
            foreach (var group in timetable) sb.AppendLine(TimeTableService.GetTimetableTextByDay(group)).AppendLine();

            var text = sb.ToString();


            var messageToEdit = callbackQuery.Message!.MessageId;

            if (messageToEdit <= 0)
                await botClient.SendTextMessageAsync(callbackQuery.Message.Chat.Id, text);
            else
                await botClient.EditMessageTextAsync(callbackQuery.Message.Chat.Id,
                    messageToEdit, text);
        }


        private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception,
            CancellationToken cancellationToken)
        {
            Console.WriteLine(JsonConvert.SerializeObject(exception));
            return Task.CompletedTask;
        }
    }
}