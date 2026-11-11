using Bunit;
using Microsoft.Extensions.DependencyInjection;
using RdTracker.Core.Models;
using RdTracker.Core.Services;
using RdTracker.UI.Components;
using RdTracker.UI.Pages;

namespace RdTracker.Tests;

public class UiTests : BunitContext
{
    private readonly RdSnapshot _data = DemoData.Create(Sample.Today);
    private readonly SessionState _session = new();

    public UiTests()
    {
        Services.AddSingleton<IClock>(new FixedClock(Sample.Today));
        Services.AddSingleton<IRdDataService>(new InMemoryRdDataService(_data));
        Services.AddSingleton<IAuthService, DemoAuthService>();
        Services.AddSingleton(_session);
    }

    [Fact]
    public void Demo_button_signs_the_manager_in()
    {
        var login = Render<Login>();

        login.Find(".demo-manager").Click();

        Assert.Equal(Role.Manager, _session.User?.Role);
    }

    [Fact]
    public void Wrong_password_shows_an_error_and_stays_signed_out()
    {
        var login = Render<Login>();

        login.Find("#username").Change("manager");
        login.Find("#password").Change("nope");
        login.Find("form").Submit();

        Assert.Contains("Incorrect username or password", login.Find(".alert").TextContent);
        Assert.Null(_session.User);
    }

    [Fact]
    public void Jobs_page_lists_every_job_and_filters_by_status()
    {
        var views = _data.Views(Sample.Today);
        var overdue = views.Count(v => v.Status == JobStatus.Overdue);

        var page = Render<Jobs>();
        Assert.Equal(views.Count, page.FindAll("tr.job-row").Count);

        page.Find("#filter-status").Change(nameof(JobStatus.Overdue));

        Assert.Equal(overdue, page.FindAll("tr.job-row").Count);
        Assert.Contains($"{overdue} of {views.Count} jobs", page.Find("#job-count").TextContent);
    }

    [Fact]
    public void Only_managers_see_the_new_job_button()
    {
        _session.SignIn(new UserAccount("draftsman", "Anan K.", Role.Draftsman, DraftsmanId: 1));
        Assert.Empty(Render<Jobs>().FindAll("a[href='jobs/new']"));

        _session.SignIn(new UserAccount("manager", "Narin S.", Role.Manager));
        Assert.Single(Render<Jobs>().FindAll("a[href='jobs/new']"));
    }
}
