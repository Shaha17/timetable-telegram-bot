using System.Linq;

namespace timetable_telegram_bot
{
    public static class Utils
    {
        public static int GetFacultyNumByDirection(string dir)
        {
            dir = dir.ToLower();
            if (Configs.NaturalScienceDirections.Select(x => x.ToLower()).Contains(dir)) return 1;
            if (Configs.HumanitiesDirections.Select(x => x.ToLower()).Contains(dir)) return 2;

            if (Configs.Directions.ContainsKey(dir))
                return GetFacultyNumByDirection(Configs.Directions[dir]);
            return 0;
        }

        public static int GetDirectionNumByDirectionName(string dir)
        {
            dir = dir.ToLower();
            var naturScienceDirs = Configs.NaturalScienceDirections.Select(x => x.ToLower()).ToList();
            if (naturScienceDirs.Exists(x => x.Equals(dir)))
                return naturScienceDirs.FindIndex(x => x.Equals(dir)) + 1;

            var humanityDirs = Configs.HumanitiesDirections.Select(x => x.ToLower()).ToList();
            if (humanityDirs.Exists(x => x.Equals(dir)))
                return humanityDirs.FindIndex(x => x.Equals(dir)) + 1;

            if (Configs.Directions.ContainsKey(dir))
                return GetDirectionNumByDirectionName(Configs.Directions[dir]);

            return -1;
        }

        public static string GetDayStringFromDaynum(int dayNum)
        {
            return dayNum switch
            {
                1 => "Понедельник",
                2 => "Вторник",
                3 => "Среда",
                4 => "Четверг",
                5 => "Пятница",
                6 => "Суббота",
                7 => "Воскресенье",
            };
        }
        public static string GetShortDayStringFromDaynum(int dayNum)
        {
            return dayNum switch
            {
                1 => "ПН",
                2 => "ВТ",
                3 => "СР",
                4 => "ЧТ",
                5 => "ПТ",
                6 => "СБ",
                7 => "ВС",
            };
        }

        public static string GetUserPrefix(Telegram.Bot.Types.User usr, Telegram.Bot.Types.Message message)
        {
            return $"{usr.FirstName} {usr.LastName} ({usr.Username} {message.Chat.Id.ToString()})";
        }
    }
}