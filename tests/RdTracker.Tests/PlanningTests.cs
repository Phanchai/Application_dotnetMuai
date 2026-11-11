using RdTracker.Core.Models;
using RdTracker.Core.Planning;

namespace RdTracker.Tests;

public class WorkCalendarTests
{
    private static readonly DateOnly Monday = new(2026, 11, 9);
    private static readonly DateOnly Friday = new(2026, 11, 13);
    private static readonly DateOnly Saturday = new(2026, 11, 14);

    [Theory]
    [InlineData(1, 0)]   // under a day finishes the same day
    [InlineData(8, 0)]
    [InlineData(9, 1)]   // one extra hour spills into the next day
    [InlineData(40, 4)]  // a full week: Monday → Friday
    public void Finish_date_counts_eight_hour_days(int hours, int expectedOffset)
    {
        Assert.Equal(Monday.AddDays(expectedOffset), WorkCalendar.FinishDate(Monday, hours));
    }

    [Fact]
    public void Finish_date_skips_weekends()
    {
        // 16 h starting Friday: Friday + Monday.
        Assert.Equal(new DateOnly(2026, 11, 16), WorkCalendar.FinishDate(Friday, 16));
        // Starting on a Saturday really starts on Monday.
        Assert.Equal(new DateOnly(2026, 11, 16), WorkCalendar.FinishDate(Saturday, 8));
    }

    [Fact]
    public void Finish_date_rejects_non_positive_hours()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WorkCalendar.FinishDate(Monday, 0));
    }

    [Fact]
    public void Working_days_are_inclusive_and_exclude_weekends()
    {
        Assert.Equal(5, WorkCalendar.WorkingDays(Monday, Friday));
        Assert.Equal(5, WorkCalendar.WorkingDays(Monday, Saturday.AddDays(1)));
        Assert.Equal(0, WorkCalendar.WorkingDays(Friday, Monday));
        Assert.Equal(21, WorkCalendar.WorkingDaysInMonth(2026, 11));
    }
}

public class JobMetricsTests
{
    private static readonly DateOnly Today = Sample.Today;

    [Fact]
    public void Status_follows_dates()
    {
        var future = Sample.Job(Today.AddDays(2), Today.AddDays(5));
        var running = Sample.Job(Today.AddDays(-2), Today.AddDays(2), started: Today.AddDays(-2));
        var lateNotStarted = Sample.Job(Today.AddDays(-9), Today.AddDays(-1));
        var lateRunning = lateNotStarted with { StartedOn = Today.AddDays(-9) };
        var done = lateRunning with { ApprovedOn = Today };

        Assert.Equal(JobStatus.Planned, JobMetrics.StatusOf(future, Today));
        Assert.Equal(JobStatus.InProgress, JobMetrics.StatusOf(running, Today));
        Assert.Equal(JobStatus.Overdue, JobMetrics.StatusOf(lateNotStarted, Today));
        Assert.Equal(JobStatus.Overdue, JobMetrics.StatusOf(lateRunning, Today));
        Assert.Equal(JobStatus.Approved, JobMetrics.StatusOf(done, Today));
    }

    [Fact]
    public void A_job_due_today_is_not_overdue_yet()
    {
        var dueToday = Sample.Job(Today.AddDays(-3), Today, started: Today.AddDays(-3));
        Assert.Equal(JobStatus.InProgress, JobMetrics.StatusOf(dueToday, Today));
        Assert.Equal(0, JobMetrics.DelayDays(dueToday, Today));
    }

    [Fact]
    public void Delay_is_measured_to_approval_or_today()
    {
        var open = Sample.Job(Today.AddDays(-10), Today.AddDays(-4), started: Today.AddDays(-10));
        var approvedLate = open with { ApprovedOn = Today.AddDays(-2) };
        var approvedEarly = open with { ApprovedOn = Today.AddDays(-6) };

        Assert.Equal(4, JobMetrics.DelayDays(open, Today));
        Assert.Equal(2, JobMetrics.DelayDays(approvedLate, Today));
        Assert.Equal(0, JobMetrics.DelayDays(approvedEarly, Today));
        Assert.False(JobMetrics.IsOnTime(approvedLate));
        Assert.True(JobMetrics.IsOnTime(approvedEarly));
        Assert.False(JobMetrics.IsOnTime(open));
    }

    [Fact]
    public void Planned_hours_depend_on_kind_and_quantity()
    {
        Assert.Equal(40, JobMetrics.PlannedHours(Sample.Job(Today, Today), Sample.Wardrobe));
        Assert.Equal(80, JobMetrics.PlannedHours(Sample.Job(Today, Today, quantity: 2), Sample.Wardrobe));
        Assert.Equal(16, JobMetrics.PlannedHours(Sample.Job(Today, Today, kind: JobKind.Copy), Sample.Wardrobe));
    }
}

public class ReportsTests
{
    private static readonly DateOnly Today = Sample.Today;

    [Fact]
    public void Monthly_completion_covers_six_months_ending_now()
    {
        var jobs = new[]
        {
            Sample.Job(new(2026, 10, 1), new(2026, 10, 7), started: new(2026, 10, 1), approved: new(2026, 10, 7)),
            Sample.Job(new(2026, 11, 2), new(2026, 11, 6), started: new(2026, 11, 2)),
            Sample.Job(new(2026, 11, 9), new(2026, 11, 20)),
            Sample.Job(new(2025, 1, 1), new(2025, 1, 5)), // outside the window
        };

        var months = Reports.CompletionByMonth(jobs, Today);

        Assert.Equal(6, months.Count);
        Assert.Equal((2026, 6), (months[0].Year, months[0].Month));
        Assert.Equal(new MonthlyCompletion(2026, 10, Approved: 1, Open: 0), months[4]);
        Assert.Equal(new MonthlyCompletion(2026, 11, Approved: 0, Open: 2), months[5]);
    }

    [Fact]
    public void Workload_prorates_jobs_that_span_two_months()
    {
        // 40 h over Mon 26 Oct – Fri 6 Nov 2026 = 10 working days, 5 in each month.
        var job = Sample.Job(new(2026, 10, 26), new(2026, 11, 6));
        var data = Sample.Snapshot(job);

        var november = Reports.WorkloadFor(2026, 11, data).Single();
        var december = Reports.WorkloadFor(2026, 12, data).Single();

        Assert.Equal(20, november.PlannedHours);
        Assert.Equal(21 * 8, november.CapacityHours);
        Assert.Equal(1, november.OpenJobs);
        Assert.Equal(20.0 / 168, november.Utilisation, precision: 6);
        Assert.Equal(0, december.PlannedHours);
    }

    [Fact]
    public void Summary_counts_open_overdue_and_on_time_rate()
    {
        var onTime = Sample.Job(Today.AddDays(-20), Today.AddDays(-15), started: Today.AddDays(-20), approved: Today.AddDays(-15));
        var late = Sample.Job(Today.AddDays(-14), Today.AddDays(-10), started: Today.AddDays(-14), approved: Today.AddDays(-8));
        var overdue = Sample.Job(Today.AddDays(-7), Today.AddDays(-1), started: Today.AddDays(-7));
        var planned = Sample.Job(Today.AddDays(1), Today.AddDays(4));

        var summary = Reports.Summary(Sample.Snapshot(onTime, late, overdue, planned), Today);

        Assert.Equal(4, summary.Jobs);
        Assert.Equal(2, summary.OpenJobs);
        Assert.Equal(1, summary.Overdue);
        Assert.Equal(0.5, summary.OnTimeRate);
    }
}
