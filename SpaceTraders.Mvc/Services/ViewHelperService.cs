using SpaceTraders.Models;

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

    public static double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        // Using the distance formula: sqrt((x2 - x1)^2 + (y2 - y1)^2)
        double deltaX = x2 - x1;
        double deltaY = y2 - y1;

        return Math.Sqrt((deltaX * deltaX) + (deltaY * deltaY));
    }

    public static IReadOnlyList<Waypoint> SortWaypoints(IReadOnlyList<Waypoint> waypoints, int currentX, int currentY)
    {
        return waypoints.OrderBy(w => CalculateDistance(w.X, w.Y, currentX, currentY)).ToList();
    }
}