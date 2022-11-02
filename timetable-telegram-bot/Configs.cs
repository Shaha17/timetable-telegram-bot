using System.Collections.Generic;

namespace timetable_telegram_bot
{
    public class Configs
    {
        public static string TimeTableUrl = "https://www.msu.tj/ru/timetable";

        public static Dictionary<string, string> Directions = new()
        {
            {"pmi", "ПМИ"},
            {"geo", "Геология"},
            {"hfmm", "ХФММ"},
            {"mo", "МО"},
            {"gmu", "ГМУ"},
            {"lin", "Лингвистика"},
        };

        public static List<string> HumanitiesDirections = new()
            {Directions["mo"], Directions["gmu"], Directions["lin"]};

        public static List<string> HumanitiesDirectionsShort = new()
            {"mo", "gmu", "lin"};

        public static List<string> NaturalScienceDirections = new()
            {Directions["pmi"], Directions["geo"], Directions["hfmm"]};

        public static List<string> NaturalScienceDirectionsShort = new()
            {"pmi", "geo", "hfmm"};
    }
}