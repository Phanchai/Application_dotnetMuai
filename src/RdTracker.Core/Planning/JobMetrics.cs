using RdTracker.Core.Models;

namespace RdTracker.Core.Planning;

public static class JobMetrics
{
    public static int PlannedHours(DesignJob job, ModelType type) =>
        (job.Kind == JobKind.Copy ? type.CopyHours : type.StandardHours) * job.Quantity;

    /// <summary>
    /// Approved wins; otherwise a job past its planned finish is overdue,
    /// whether or not anyone has started it.
    /// </summary>
    public static JobStatus StatusOf(DesignJob job, DateOnly today)
    {
        if (job.ApprovedOn is not null)
        {
            return JobStatus.Approved;
        }

        if (today > job.PlannedFinish)
        {
            return JobStatus.Overdue;
        }

        return job.StartedOn is not null ? JobStatus.InProgress : JobStatus.Planned;
    }

    /// <summary>Calendar days past the planned finish (to approval, or to today while open).</summary>
    public static int DelayDays(DesignJob job, DateOnly today)
    {
        var end = job.ApprovedOn ?? today;
        return Math.Max(0, end.DayNumber - job.PlannedFinish.DayNumber);
    }

    public static bool IsOnTime(DesignJob job) =>
        job.ApprovedOn is { } approved && approved <= job.PlannedFinish;
}
