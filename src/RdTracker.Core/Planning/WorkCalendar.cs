namespace RdTracker.Core.Planning;

/// <summary>Working-day arithmetic: Monday–Friday, eight hours a day.</summary>
public static class WorkCalendar
{
    public const int HoursPerDay = 8;

    public static bool IsWorkingDay(DateOnly day) =>
        day.DayOfWeek is not (DayOfWeek.Saturday or DayOfWeek.Sunday);

    /// <summary>The given day if it is a working day, otherwise the next one.</summary>
    public static DateOnly NextWorkingDay(DateOnly day)
    {
        while (!IsWorkingDay(day))
        {
            day = day.AddDays(1);
        }

        return day;
    }

    /// <summary>The day on which <paramref name="hours"/> of work finishes when started on <paramref name="start"/>.</summary>
    public static DateOnly FinishDate(DateOnly start, int hours)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(hours);

        var day = NextWorkingDay(start);
        var daysNeeded = (hours + HoursPerDay - 1) / HoursPerDay;
        for (var i = 1; i < daysNeeded; i++)
        {
            day = NextWorkingDay(day.AddDays(1));
        }

        return day;
    }

    /// <summary>Number of working days in the inclusive range; zero when the range is empty.</summary>
    public static int WorkingDays(DateOnly from, DateOnly to)
    {
        var count = 0;
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            if (IsWorkingDay(day))
            {
                count++;
            }
        }

        return count;
    }

    public static int WorkingDaysInMonth(int year, int month) =>
        WorkingDays(new DateOnly(year, month, 1), new DateOnly(year, month, DateTime.DaysInMonth(year, month)));
}
