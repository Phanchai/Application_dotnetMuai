using RdTracker.Core.Models;
using RdTracker.Core.Planning;
using RdTracker.Core.Services;

namespace RdTracker.Tests;

public class InMemoryRdDataServiceTests
{
    private static readonly DateOnly Today = Sample.Today;

    private static InMemoryRdDataService NewService(params DesignJob[] jobs) => new(Sample.Snapshot(jobs));

    [Fact]
    public async Task Creating_a_job_assigns_the_next_id()
    {
        var service = NewService(Sample.Job(Today, Today) with { Id = 7 });

        var created = await service.SaveJobAsync(Sample.Job(Today, Today.AddDays(2)) with { Id = 0 });

        Assert.Equal(8, created.Id);
        Assert.Equal(2, (await service.GetSnapshotAsync()).Jobs.Count);
    }

    [Fact]
    public async Task Updating_replaces_the_job_and_unknown_ids_fail()
    {
        var service = NewService(Sample.Job(Today, Today));

        await service.SaveJobAsync(Sample.Job(Today, Today) with { Remark = "changed" });
        Assert.Equal("changed", (await service.GetSnapshotAsync()).Jobs.Single().Remark);

        await Assert.ThrowsAsync<RdValidationException>(
            () => service.SaveJobAsync(Sample.Job(Today, Today) with { Id = 99 }));
    }

    [Fact]
    public async Task Invalid_jobs_are_rejected_with_a_readable_message()
    {
        var service = NewService();
        var valid = Sample.Job(Today, Today.AddDays(1)) with { Id = 0 };

        var backwards = await Assert.ThrowsAsync<RdValidationException>(
            () => service.SaveJobAsync(valid with { PlannedFinish = Today.AddDays(-1) }));
        Assert.Contains("finish", backwards.Message);

        await Assert.ThrowsAsync<RdValidationException>(() => service.SaveJobAsync(valid with { Quantity = 0 }));
        await Assert.ThrowsAsync<RdValidationException>(() => service.SaveJobAsync(valid with { ProjectId = 42 }));
        await Assert.ThrowsAsync<RdValidationException>(() => service.SaveJobAsync(valid with { DraftsmanId = 42 }));
        await Assert.ThrowsAsync<RdValidationException>(() => service.SaveJobAsync(valid with { ModelTypeId = 42 }));
        await Assert.ThrowsAsync<RdValidationException>(() => service.SaveJobAsync(valid with { ApprovedOn = Today }));
        Assert.Empty((await service.GetSnapshotAsync()).Jobs);
    }

    [Fact]
    public async Task Start_then_approve_follows_the_workflow()
    {
        var service = NewService(Sample.Job(Today, Today.AddDays(3)));

        await Assert.ThrowsAsync<RdValidationException>(() => service.ApproveJobAsync(1, Today));

        var started = await service.StartJobAsync(1, Today);
        Assert.Equal(Today, started.StartedOn);
        await Assert.ThrowsAsync<RdValidationException>(() => service.StartJobAsync(1, Today));
        await Assert.ThrowsAsync<RdValidationException>(() => service.ApproveJobAsync(1, Today.AddDays(-1)));

        var approved = await service.ApproveJobAsync(1, Today.AddDays(2));
        Assert.Equal(JobStatus.Approved, JobMetrics.StatusOf(approved, Today.AddDays(2)));
        await Assert.ThrowsAsync<RdValidationException>(() => service.ApproveJobAsync(1, Today.AddDays(3)));
    }

    [Fact]
    public async Task Brands_validate_codes_and_cannot_be_deleted_while_in_use()
    {
        var service = NewService();

        var brand = await service.AddBrandAsync(" abc ", " New Brand ");
        Assert.Equal(("ABC", "New Brand"), (brand.Code, brand.Name));

        await Assert.ThrowsAsync<RdValidationException>(() => service.AddBrandAsync("abc", "Duplicate"));
        await Assert.ThrowsAsync<RdValidationException>(() => service.AddBrandAsync("x", "Too short"));
        await Assert.ThrowsAsync<RdValidationException>(() => service.AddBrandAsync("OK1", "  "));

        var inUse = await Assert.ThrowsAsync<RdValidationException>(() => service.DeleteBrandAsync(1));
        Assert.Contains("used by 1 project", inUse.Message);

        await service.DeleteBrandAsync(brand.Id);
        Assert.Single((await service.GetSnapshotAsync()).Brands);
    }

    [Fact]
    public async Task Snapshots_are_copies()
    {
        var service = NewService(Sample.Job(Today, Today));
        var before = await service.GetSnapshotAsync();

        await service.DeleteJobAsync(1);

        Assert.Single(before.Jobs);
        Assert.Empty((await service.GetSnapshotAsync()).Jobs);
    }
}

public class DemoDataTests
{
    private static readonly DateOnly Today = Sample.Today;

    [Fact]
    public void Is_deterministic_for_a_given_day_and_seed()
    {
        var a = DemoData.Create(Today);
        var b = DemoData.Create(Today);

        Assert.Equal(a.Jobs, b.Jobs);
        Assert.NotEqual(a.Jobs, DemoData.Create(Today, seed: 7).Jobs);
    }

    [Fact]
    public void Every_job_is_internally_consistent()
    {
        var data = DemoData.Create(Today);

        Assert.Equal(data.Jobs.Count, data.Jobs.Select(j => j.Id).Distinct().Count());
        foreach (var view in data.Views(Today))
        {
            var job = view.Job;
            Assert.True(WorkCalendar.IsWorkingDay(job.PlannedStart));
            Assert.True(job.PlannedFinish >= job.PlannedStart);
            Assert.Equal(WorkCalendar.FinishDate(job.PlannedStart, view.PlannedHours), job.PlannedFinish);
            if (job.ApprovedOn is { } approved)
            {
                Assert.NotNull(job.StartedOn);
                Assert.True(approved >= job.StartedOn && approved <= Today);
            }
        }
    }

    [Fact]
    public void No_draftsman_has_overlapping_plans()
    {
        var data = DemoData.Create(Today);

        foreach (var group in data.Jobs.GroupBy(j => j.DraftsmanId))
        {
            var ordered = group.OrderBy(j => j.PlannedStart).ToList();
            for (var i = 1; i < ordered.Count; i++)
            {
                Assert.True(ordered[i].PlannedStart > ordered[i - 1].PlannedFinish);
            }
        }
    }

    [Fact]
    public void Shows_every_status_so_the_demo_looks_alive()
    {
        var statuses = DemoData.Create(Today).Views(Today).Select(v => v.Status).ToHashSet();

        Assert.Equal(4, statuses.Count);
    }

    [Fact]
    public async Task Demo_data_passes_the_stores_own_validation()
    {
        var data = DemoData.Create(Today);
        var service = new InMemoryRdDataService(data);

        foreach (var job in data.Jobs)
        {
            await service.SaveJobAsync(job);
        }
    }
}

public class AuthTests
{
    [Fact]
    public async Task Demo_accounts_sign_in_case_insensitively()
    {
        var auth = new DemoAuthService();

        Assert.Equal(Role.Manager, (await auth.SignInAsync(" Manager ", "manager1234"))?.Role);
        Assert.Equal(1, (await auth.SignInAsync("draftsman", "draftsman1234"))?.DraftsmanId);
        Assert.Null(await auth.SignInAsync("manager", "wrong"));
        Assert.Null(await auth.SignInAsync("nobody", "manager1234"));
    }

    [Fact]
    public void Permissions_depend_on_role_and_job_state()
    {
        var session = new SessionState();
        var mine = Sample.Job(Sample.Today, Sample.Today, draftsmanId: 1);
        var theirs = mine with { DraftsmanId = 2 };
        var started = mine with { StartedOn = Sample.Today };

        Assert.False(session.CanStart(mine));

        session.SignIn(new UserAccount("draftsman", "Anan K.", Role.Draftsman, DraftsmanId: 1));
        Assert.True(session.CanStart(mine));
        Assert.False(session.CanStart(theirs));
        Assert.False(session.CanStart(started));
        Assert.False(session.CanApprove(started));
        Assert.False(session.CanEdit(mine));

        session.SignIn(new UserAccount("manager", "Narin S.", Role.Manager));
        Assert.True(session.CanStart(theirs));
        Assert.True(session.CanApprove(started));
        Assert.False(session.CanApprove(mine));
        Assert.True(session.CanEdit(mine));
    }

    [Fact]
    public void Session_raises_changed_on_sign_in_and_out()
    {
        var session = new SessionState();
        var events = 0;
        session.Changed += () => events++;

        session.SignIn(new UserAccount("manager", "Narin S.", Role.Manager));
        session.SignOut();

        Assert.Equal(2, events);
        Assert.Null(session.User);
    }
}
