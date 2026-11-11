using RdTracker.Core.Models;

namespace RdTracker.Core.Planning;

public sealed record MonthlyCompletion(int Year, int Month, int Approved, int Open)
{
    public int Total => Approved + Open;
}

public sealed record Workload(Draftsman Draftsman, int OpenJobs, int PlannedHours, int CapacityHours)
{
    public double Utilisation => CapacityHours == 0 ? 0 : (double)PlannedHours / CapacityHours;
}

public sealed record DashboardSummary(int Projects, int Jobs, int OpenJobs, int Overdue, int Draftsmen, double OnTimeRate);

public static class Reports
{
    /// <summary>Jobs per month of planned finish, for the <paramref name="months"/> months ending with the current one.</summary>
    public static IReadOnlyList<MonthlyCompletion> CompletionByMonth(IEnumerable<DesignJob> jobs, DateOnly today, int months = 6)
    {
        var list = jobs.ToList();
        var result = new List<MonthlyCompletion>(months);
        var first = new DateOnly(today.Year, today.Month, 1).AddMonths(-(months - 1));

        for (var i = 0; i < months; i++)
        {
            var month = first.AddMonths(i);
            var inMonth = list.Where(j => j.PlannedFinish.Year == month.Year && j.PlannedFinish.Month == month.Month).ToList();
            var approved = inMonth.Count(j => j.ApprovedOn is not null);
            result.Add(new MonthlyCompletion(month.Year, month.Month, approved, inMonth.Count - approved));
        }

        return result;
    }

    /// <summary>
    /// Planned hours per draftsman for one month. A job that spans two months
    /// contributes to each in proportion to its working days there.
    /// </summary>
    public static IReadOnlyList<Workload> WorkloadFor(int year, int month, RdSnapshot data)
    {
        var monthStart = new DateOnly(year, month, 1);
        var monthEnd = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
        var capacity = WorkCalendar.WorkingDaysInMonth(year, month) * WorkCalendar.HoursPerDay;

        return data.Draftsmen.Select(draftsman =>
        {
            var hours = 0.0;
            var open = 0;
            foreach (var job in data.Jobs.Where(j => j.DraftsmanId == draftsman.Id))
            {
                var from = job.PlannedStart > monthStart ? job.PlannedStart : monthStart;
                var to = job.PlannedFinish < monthEnd ? job.PlannedFinish : monthEnd;
                var overlap = WorkCalendar.WorkingDays(from, to);
                if (overlap == 0)
                {
                    continue;
                }

                var span = Math.Max(1, WorkCalendar.WorkingDays(job.PlannedStart, job.PlannedFinish));
                hours += JobMetrics.PlannedHours(job, data.TypeOf(job)) * (double)overlap / span;
                if (job.ApprovedOn is null)
                {
                    open++;
                }
            }

            return new Workload(draftsman, open, (int)Math.Round(hours), capacity);
        }).ToList();
    }

    public static DashboardSummary Summary(RdSnapshot data, DateOnly today)
    {
        var approved = data.Jobs.Where(j => j.ApprovedOn is not null).ToList();
        var onTime = approved.Count == 0 ? 0 : (double)approved.Count(JobMetrics.IsOnTime) / approved.Count;

        return new DashboardSummary(
            Projects: data.Projects.Count,
            Jobs: data.Jobs.Count,
            OpenJobs: data.Jobs.Count - approved.Count,
            Overdue: data.Jobs.Count(j => JobMetrics.StatusOf(j, today) == JobStatus.Overdue),
            Draftsmen: data.Draftsmen.Count,
            OnTimeRate: onTime);
    }
}
