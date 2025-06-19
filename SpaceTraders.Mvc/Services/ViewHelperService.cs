namespace SpaceTraders.Mvc.Services;

public static class ViewHelperService
{
    public static string MinimalHumanReadableTimeSpan(TimeSpan timeSpan)
    {
        if (timeSpan == TimeSpan.Zero)
            return "0s";

        if (timeSpan.Days > 0)
            return $"{timeSpan.Days}d";
        if (timeSpan.Hours > 0)
            return $"{timeSpan.Hours}h";
        if (timeSpan.Minutes > 0)
            return $"{timeSpan.Minutes}m";
        return $"{timeSpan.Seconds}s";
    }

    public static string HumanReadableTimeSpan(TimeSpan t)
    {
        if (t.TotalSeconds <= 1)
        {
            return $@"{t:s\.ff} seconds";
        }
        if (t.TotalMinutes <= 1)
        {
            return $@"{t:%s} seconds";
        }
        if (t.TotalHours <= 1)
        {
            return $@"{t:%m} minutes";
        }
        if (t.TotalDays <= 1)
        {
            return $@"{t:%h} hours";
        }

        return $@"{t:%d} days";
    }

    public static string ReadableCreditValue(long l)
    {
        if (l > 1000000000) // billion
        {
            return (l / 1000000000) + "b";
        }
        if (l > 1000000) // million
        {
            return (l / 1000000) + "m";
        }
        if (l > 1000) // thousand
        {
            return (l / 1000) + "k";
        }
        return l.ToString();
    }
}