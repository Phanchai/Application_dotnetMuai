using System.Globalization;
using RdTracker.Core.Models;

namespace RdTracker.UI;

/// <summary>Culture-invariant formatting so the UI renders the same on every device.</summary>
public static class Format
{
    private static readonly CultureInfo Inv = CultureInfo.InvariantCulture;

    public static string Date(DateOnly date) => date.ToString("d MMM yyyy", Inv);

    public static string Date(DateOnly? date) => date is { } d ? Date(d) : "—";

    public static string ShortDate(DateOnly date) => date.ToString("d MMM", Inv);

    public static string Month(int year, int month) => new DateOnly(year, month, 1).ToString("MMMM yyyy", Inv);

    public static string MonthShort(int year, int month) => new DateOnly(year, month, 1).ToString("MMM", Inv);

    public static string Percent(double ratio) => Math.Round(ratio * 100).ToString("0", Inv) + "%";

    /// <summary>A number for use inside style/SVG attributes (always "." as decimal separator).</summary>
    public static string Num(double value) => value.ToString("0.###", Inv);

    public static string Status(JobStatus status) => status switch
    {
        JobStatus.InProgress => "In progress",
        _ => status.ToString(),
    };

    public static string StatusClass(JobStatus status) => status switch
    {
        JobStatus.Approved => "st-approved",
        JobStatus.InProgress => "st-progress",
        JobStatus.Overdue => "st-overdue",
        _ => "st-planned",
    };
}
