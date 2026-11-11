namespace RdTracker.Core.Models;

public sealed record Brand(int Id, string Code, string Name);

public sealed record Team(int Id, string Name, string Lead);

public sealed record Draftsman(int Id, string Name, int TeamId);

/// <summary>A furniture model family with its standard design effort.</summary>
/// <param name="StandardHours">Hours to design one new model from scratch.</param>
/// <param name="CopyHours">Hours to adapt an existing design (a "copy" job).</param>
public sealed record ModelType(int Id, string Code, string Name, string Group, int StandardHours, int CopyHours);

public sealed record Project(int Id, string Name, int BrandId, string Owner, DateOnly ReceivedOn, DateOnly DueOn);

public enum JobKind
{
    New,
    Copy,
}

public enum JobStatus
{
    Planned,
    InProgress,
    Overdue,
    Approved,
}

/// <summary>One design job: a draftsman drawing a model for a project, with a plan and the actual dates.</summary>
public sealed record DesignJob
{
    public int Id { get; init; }
    public int ProjectId { get; init; }
    public int DraftsmanId { get; init; }
    public int ModelTypeId { get; init; }
    public JobKind Kind { get; init; }
    public int Quantity { get; init; } = 1;
    public string Remark { get; init; } = "";
    public DateOnly PlannedStart { get; init; }
    public DateOnly PlannedFinish { get; init; }
    public DateOnly? StartedOn { get; init; }
    public DateOnly? ApprovedOn { get; init; }
}

/// <summary>A job joined with everything the UI shows next to it.</summary>
public sealed record JobView(
    DesignJob Job,
    Project Project,
    Brand Brand,
    Draftsman Draftsman,
    Team Team,
    ModelType Type,
    JobStatus Status,
    int PlannedHours,
    int DelayDays);

public enum Role
{
    Manager,
    Draftsman,
}

public sealed record UserAccount(string Username, string DisplayName, Role Role, int? DraftsmanId = null);
