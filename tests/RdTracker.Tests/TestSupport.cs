using RdTracker.Core.Models;
using RdTracker.Core.Services;

namespace RdTracker.Tests;

public sealed class FixedClock(DateOnly today) : IClock
{
    public DateOnly Today { get; } = today;
}

public static class Sample
{
    /// <summary>Wednesday 11 November 2026.</summary>
    public static readonly DateOnly Today = new(2026, 11, 11);

    public static readonly ModelType Wardrobe = new(1, "WR", "Wardrobe", "Bedroom", 40, 16);

    public static DesignJob Job(
        DateOnly start,
        DateOnly finish,
        DateOnly? started = null,
        DateOnly? approved = null,
        JobKind kind = JobKind.New,
        int quantity = 1,
        int draftsmanId = 1) => new()
        {
            Id = 1,
            ProjectId = 1,
            DraftsmanId = draftsmanId,
            ModelTypeId = 1,
            Kind = kind,
            Quantity = quantity,
            PlannedStart = start,
            PlannedFinish = finish,
            StartedOn = started,
            ApprovedOn = approved,
        };

    /// <summary>One brand, team, draftsman, model type and project — plus the given jobs.</summary>
    public static RdSnapshot Snapshot(params DesignJob[] jobs) => new(
        [new Brand(1, "HML", "Homeline")],
        [new Team(1, "Team A", "Lead")],
        [new Draftsman(1, "Anan K.", 1)],
        [Wardrobe],
        [new Project(1, "Project", 1, "Owner", Today.AddDays(-30), Today.AddDays(30))],
        jobs);
}
