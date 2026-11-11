using RdTracker.Core.Models;

namespace RdTracker.Core.Services;

/// <summary>Thread-safe in-memory store that enforces the same rules a real backend would.</summary>
public sealed class InMemoryRdDataService : IRdDataService
{
    private readonly object _gate = new();
    private readonly List<Brand> _brands;
    private readonly List<Team> _teams;
    private readonly List<Draftsman> _draftsmen;
    private readonly List<ModelType> _modelTypes;
    private readonly List<Project> _projects;
    private readonly List<DesignJob> _jobs;

    public InMemoryRdDataService(RdSnapshot seed)
    {
        _brands = seed.Brands.ToList();
        _teams = seed.Teams.ToList();
        _draftsmen = seed.Draftsmen.ToList();
        _modelTypes = seed.ModelTypes.ToList();
        _projects = seed.Projects.ToList();
        _jobs = seed.Jobs.ToList();
    }

    public Task<RdSnapshot> GetSnapshotAsync()
    {
        lock (_gate)
        {
            return Task.FromResult(new RdSnapshot(
                _brands.ToList(),
                _teams.ToList(),
                _draftsmen.ToList(),
                _modelTypes.ToList(),
                _projects.ToList(),
                _jobs.ToList()));
        }
    }

    public Task<DesignJob> SaveJobAsync(DesignJob job)
    {
        lock (_gate)
        {
            Validate(job);

            if (job.Id == 0)
            {
                var created = job with { Id = _jobs.Count == 0 ? 1 : _jobs.Max(j => j.Id) + 1 };
                _jobs.Add(created);
                return Task.FromResult(created);
            }

            var index = IndexOf(job.Id);
            _jobs[index] = job;
            return Task.FromResult(job);
        }
    }

    public Task DeleteJobAsync(int id)
    {
        lock (_gate)
        {
            _jobs.RemoveAt(IndexOf(id));
            return Task.CompletedTask;
        }
    }

    public Task<DesignJob> StartJobAsync(int id, DateOnly on)
    {
        lock (_gate)
        {
            var index = IndexOf(id);
            var job = _jobs[index];
            if (job.StartedOn is not null)
            {
                throw new RdValidationException("This job has already been started.");
            }

            return Task.FromResult(_jobs[index] = job with { StartedOn = on });
        }
    }

    public Task<DesignJob> ApproveJobAsync(int id, DateOnly on)
    {
        lock (_gate)
        {
            var index = IndexOf(id);
            var job = _jobs[index];
            if (job.ApprovedOn is not null)
            {
                throw new RdValidationException("This job has already been approved.");
            }

            if (job.StartedOn is not { } started)
            {
                throw new RdValidationException("A job must be started before it can be approved.");
            }

            if (on < started)
            {
                throw new RdValidationException("The approval date cannot be before the start date.");
            }

            return Task.FromResult(_jobs[index] = job with { ApprovedOn = on });
        }
    }

    public Task<Brand> AddBrandAsync(string code, string name)
    {
        code = (code ?? "").Trim().ToUpperInvariant();
        name = (name ?? "").Trim();
        if (code.Length is < 2 or > 6 || !code.All(char.IsLetterOrDigit))
        {
            throw new RdValidationException("Brand code must be 2–6 letters or digits.");
        }

        if (name.Length == 0)
        {
            throw new RdValidationException("Brand name is required.");
        }

        lock (_gate)
        {
            if (_brands.Any(b => b.Code == code))
            {
                throw new RdValidationException($"Brand code {code} already exists.");
            }

            var brand = new Brand(_brands.Count == 0 ? 1 : _brands.Max(b => b.Id) + 1, code, name);
            _brands.Add(brand);
            return Task.FromResult(brand);
        }
    }

    public Task DeleteBrandAsync(int id)
    {
        lock (_gate)
        {
            var brand = _brands.FirstOrDefault(b => b.Id == id)
                ?? throw new RdValidationException("Brand not found.");
            var used = _projects.Count(p => p.BrandId == id);
            if (used > 0)
            {
                throw new RdValidationException($"{brand.Name} is used by {used} project(s) and cannot be deleted.");
            }

            _brands.Remove(brand);
            return Task.CompletedTask;
        }
    }

    private int IndexOf(int jobId)
    {
        var index = _jobs.FindIndex(j => j.Id == jobId);
        return index >= 0 ? index : throw new RdValidationException($"Job #{jobId} was not found.");
    }

    private void Validate(DesignJob job)
    {
        if (_projects.All(p => p.Id != job.ProjectId))
        {
            throw new RdValidationException("Choose a project.");
        }

        if (_draftsmen.All(d => d.Id != job.DraftsmanId))
        {
            throw new RdValidationException("Choose a draftsman.");
        }

        if (_modelTypes.All(m => m.Id != job.ModelTypeId))
        {
            throw new RdValidationException("Choose a model type.");
        }

        if (job.Quantity is < 1 or > 99)
        {
            throw new RdValidationException("Quantity must be between 1 and 99.");
        }

        if (job.PlannedFinish < job.PlannedStart)
        {
            throw new RdValidationException("The planned finish cannot be before the planned start.");
        }

        if (job.ApprovedOn is { } approved && (job.StartedOn is not { } started || approved < started))
        {
            throw new RdValidationException("An approved job needs a start date on or before its approval date.");
        }
    }
}
