using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace XeniaBot.Core.Helpers;

public static class TimeHelper
{
    public static DateTimeOffset UnixEpoch => DateTimeOffset.FromUnixTimeSeconds(0);
    
    /// <param name="timestamp">Milliseconds since Unix UTC Epoch</param>
    public static string SinceTimestamp(long ts)
    {
        var past = DateTimeOffset.FromUnixTimeMilliseconds(ts);
        var current = DateTimeOffset.UtcNow;
        var diff = new DateTime((current - past).Ticks);

        var sb = new List<string>();
        if (diff.Year > 0)
            sb.Add($"{diff.Year} year" + (diff.Year > 1 ? "s": ""));
        if (diff.Month > 0)
            sb.Add($"{diff.Month} month" + (diff.Month > 1 ? "s" : ""));
        if (diff.Day > 0)
            sb.Add($"{diff.Day} day" + (diff.Day > 1 ? "s" : ""));
        if (diff.Hour > 0)
            sb.Add($"{diff.Hour} hour" + (diff.Hour > 1 ? "s" : ""));
        if (diff.Minute > 0)
            sb.Add($"{diff.Minute} minute" + (diff.Minute > 1 ? "s" : ""));
        if (diff.Second > 0)
            sb.Add($"{diff.Second} second" + (diff.Second > 1 ? "s" : ""));

        return string.Join(" ", sb);
    }

    public class DateDiffResult
    {
        public int Seconds;
        public int Minutes;
        public int Hours;
        public int Days;
        public int Months;
        public int Years;
    }
    public static DateDiffResult DateDiff(DateTimeOffset d1, DateTimeOffset d2)
    {
        var d1ts = d1.ToUnixTimeMilliseconds();
        var d2ts = d2.ToUnixTimeMilliseconds();

        var result = new DateDiffResult();

        // return zero for everything if are same.
        if (d1ts == d2ts)
            return result;

        // make sure d2 > d1
        if (d1ts > d2ts)
        {
            var dtemp = d2ts;
            d2ts = d1ts;
            d1ts = dtemp;
        }

        // ----------------
        // set result fields
        
        result.Years = d2.Year - d1.Year;
        result.Months = d2.Month - d1.Month;
        result.Days = d2.Day - d1.Day;
        var distance = d2ts - d1ts;
        result.Hours = (int)Math.Floor((distance % (1000 * 60 * 60 * 24)) / (1000f * 60 * 60));
        result.Minutes = (int)Math.Floor((distance % (1000 * 60 * 60)) / (1000f * 60));
        result.Seconds = (int)Math.Floor((distance % (1000 * 60)) / 1000f);

        // ----------------
        // negative handling
        
        // First if the day difference is negative
        // eg; d2 = 13 oct, d1 = 25 sept
        if (result.Days < 0)
        {
            result.Months -= 1;
            var remainder = Math.Floor(d1.Month / 12f);
            result.Days += DateTime.DaysInMonth((int)(d1.Year + remainder), (d1.Month + 1) % 12);
        }

        // If the month difference is negative
        if (result.Months < 0)
        {
            result.Months += 12;
            result.Months -= 1;
        }

        return result;
    }

    public static TimeSpan ParseFromString(string input)
    {
        int days = 0;
        int hours = 0;
        int minutes = 0;
        int seconds = 0;
        var dayRegex = new Regex(
            @"(([0-9]+)d(|ay|ays))", 
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        var dayMatch = dayRegex.Match(input);
        if (dayMatch.Groups.Count >= 4)
        {
            days = int.Parse(dayMatch.Groups[2].Value);
        }
        
        var hourRegex = new Regex(
            @"(([0-9]+)h(|r|rs|our|ours))",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        var hourMatch = hourRegex.Match(input);
        if (hourMatch.Groups.Count >= 4)
        {
            hours = int.Parse(hourMatch.Groups[2].Value);
        }
        
        var minuteRegex = new Regex(
            @"(([0-9]+)m(|in|ins|inute|inutes))",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        var minMatch = minuteRegex.Match(input);
        if (minMatch.Groups.Count >= 4)
        {
            minutes = int.Parse(minMatch.Groups[2].Value);
        }
        
        var secondRegex = new Regex(
            @"(([0-9]+)s(|ec|ecs|econds))",
            RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
        var secMatch = secondRegex.Match(input);
        if (secMatch.Groups.Count >= 4)
        {
            seconds = int.Parse(secMatch.Groups[2].Value);
        }

        return new TimeSpan(days, hours, minutes, seconds);
    }
}