using RdTracker.Core.Planning;

namespace RdTracker.Core.Models;

/// <summary>An immutable copy of all master data and jobs at one moment.</summary>
public sealed record RdSnapshot(
    IReadOnlyList<Brand> Brands,
    IReadOnlyList<Team> Teams,
    IReadOnlyList<Draftsman> Draftsmen,
    IReadOnlyList<ModelType> ModelTypes,
    IReadOnlyList<Project> Projects,
    IReadOnlyList<DesignJob> Jobs)
{
    public Project ProjectOf(DesignJob job) => Projects.First(p => p.Id == job.ProjectId);

    public Brand BrandOf(Project project) => Brands.First(b => b.Id == project.BrandId);

    public Draftsman DraftsmanOf(DesignJob job) => Draftsmen.First(d => d.Id == job.DraftsmanId);

    public Team TeamOf(Draftsman draftsman) => Teams.First(t => t.Id == draftsman.TeamId);

    public ModelType TypeOf(DesignJob job) => ModelTypes.First(m => m.Id == job.ModelTypeId);

    public JobView View(DesignJob job, DateOnly today)
    {
        var project = ProjectOf(job);
        var draftsman = DraftsmanOf(job);
        var type = TypeOf(job);
        return new JobView(
            job,
            project,
            BrandOf(project),
            draftsman,
            TeamOf(draftsman),
            type,
            JobMetrics.StatusOf(job, today),
            JobMetrics.PlannedHours(job, type),
            JobMetrics.DelayDays(job, today));
    }

    public IReadOnlyList<JobView> Views(DateOnly today) => Jobs.Select(j => View(j, today)).ToList();
}
