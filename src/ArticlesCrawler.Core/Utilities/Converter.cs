using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace ArticlesCrawler.Core.Utilities
{
    public static class Converter
    {
        public static DateTime ViStringToDateTime(string strTime) 
        {
            // Define the regex pattern to match the date and time
            string pattern = @"(\d{1,2}\/\d{1,2}\/\d{4})[^\d]*(\d{2}:\d{2})";
            Regex regex = new Regex(pattern);
            Match match = regex.Match(strTime);

            // Create a custom culture with Vietnamese settings
            CultureInfo vietnameseCulture = new CultureInfo("vi-VN");

            // Parse the string into a DateTime object using the custom culture
            DateTime dt = DateTime.MinValue;
            if (match.Success)
            {
                // Parse the matched string into a DateTime object
                DateTime.TryParseExact(match.Value.Replace(",", ""), "d/M/yyyy HH:mm", vietnameseCulture, DateTimeStyles.None, out dt);
            }
            return dt;
        }
    }
}