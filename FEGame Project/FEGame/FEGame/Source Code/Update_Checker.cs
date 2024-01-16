//#if WINDOWS || MONOMAC
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace FEGame
{
    class Update_Checker
    {
        internal readonly static string GAME_DOWNLOAD = "yoursite.herethough/yourgame";
        readonly static string UPDATE_URL = "http://put.yoursite.herethough/yourgame/check_update.php";
        const int UPDATE_CHECK_TIMEOUT_SECONDS = 5;
        readonly static string UPDATE_REGEX = @"[^0-9.,\s\r\n]";

        internal static Tuple<Version, DateTime> check_for_update()
        {
            string update_data = NetConnection.webPost(UPDATE_URL, timeout: new TimeSpan(0, 0, UPDATE_CHECK_TIMEOUT_SECONDS));
            if (string.IsNullOrEmpty(update_data))
                return null;
            update_data = Regex.Replace(update_data, UPDATE_REGEX, "");

            var result = parse_update_data(update_data);
            return result;
        }

        private static Tuple<Version, DateTime> parse_update_data(string update_data)
        {
            string[] result = update_data.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            if (result.Length < 2)
                return null;

            int test;
            // Confirm version length
            string[] version = result[0].Split('.');
            if (version.Length != 4 || version.All(x => !int.TryParse(x, out test)))
                return null;
            // Confirm date length
            string[] date = result[1].Split(new string[] { ", " }, StringSplitOptions.RemoveEmptyEntries);
            if (date.Length != 3 || date.All(x => !int.TryParse(x, out test)))
                return null;

            return new Tuple<Version, DateTime>(
                new Version(Convert.ToInt32(version[0]), Convert.ToInt32(version[1]),
                    Convert.ToInt32(version[2]), Convert.ToInt32(version[3])),
                new DateTime(Convert.ToInt32(date[0]), Convert.ToInt32(date[1]), Convert.ToInt32(date[2])));
        }

        internal static bool test_connection()
        {
            return NetConnection.test_connection_dns(UPDATE_URL);
        }
    }
}
//#endif