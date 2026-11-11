using RdTracker.Core.Models;
using RdTracker.Core.Planning;

namespace RdTracker.Core.Services;

/// <summary>
/// Deterministic sample data for the fictional Acme Panel Co. R&amp;D department.
/// Dates are generated relative to "today" so the demo always looks current.
/// </summary>
public static class DemoData
{
    public static RdSnapshot Create(DateOnly today, int seed = 42)
    {
        var brands = new List<Brand>
        {
            new(1, "HML", "Homeline"),
            new(2, "NDL", "Nordic Living"),
            new(3, "UBN", "UrbanNest"),
            new(4, "CSW", "CasaWood"),
            new(5, "ACB", "Acme Basics"),
        };

        var teams = new List<Team>
        {
            new(1, "Living & Bedroom", "Narin S."),
            new(2, "Kitchen & Office", "Dara P."),
            new(3, "Export Projects", "Kenji M."),
        };

        var draftsmen = new List<Draftsman>
        {
            new(1, "Anan K.", 1),
            new(2, "Bua T.", 1),
            new(3, "Chai W.", 1),
            new(4, "Dao R.", 2),
            new(5, "Ek N.", 2),
            new(6, "Fah L.", 2),
            new(7, "Gun P.", 3),
            new(8, "Hana J.", 3),
        };

        var modelTypes = new List<ModelType>
        {
            new(1, "WR", "Wardrobe", "Bedroom", 40, 16),
            new(2, "BD", "Bed frame", "Bedroom", 32, 12),
            new(3, "TV", "TV cabinet", "Living", 32, 12),
            new(4, "SH", "Shelf unit", "Living", 16, 6),
            new(5, "DK", "Office desk", "Office", 24, 8),
            new(6, "KT", "Kitchen set", "Kitchen", 56, 24),
        };

        var projects = new List<Project>
        {
            new(1, "Autumn Bedroom Collection", 1, "Mali C.", today.AddDays(-80), today.AddDays(20)),
            new(2, "Compact Living Series", 3, "Mali C.", today.AddDays(-70), today.AddDays(35)),
            new(3, "Nordic Oak Refresh", 2, "Tom H.", today.AddDays(-60), today.AddDays(45)),
            new(4, "Modular Kitchen 2027", 4, "Tom H.", today.AddDays(-50), today.AddDays(60)),
            new(5, "Home Office Essentials", 5, "Ploy S.", today.AddDays(-45), today.AddDays(25)),
            new(6, "Export Flat-pack Range", 2, "Ploy S.", today.AddDays(-30), today.AddDays(75)),
        };

        var rng = new Random(seed);
        var jobs = new List<DesignJob>();
        var remarks = new[] { "", "", "", "Customer revision", "Match existing décor", "Rush for showroom", "Hardware change" };

        foreach (var draftsman in draftsmen)
        {
            var cursor = today.AddDays(-75 + draftsman.Id * 2);
            var horizon = today.AddDays(35);

            while (cursor < horizon)
            {
                var type = modelTypes[rng.Next(modelTypes.Count)];
                var kind = rng.NextDouble() < 0.3 ? JobKind.Copy : JobKind.New;
                var quantity = rng.NextDouble() < 0.2 ? 2 : 1;
                var hours = (kind == JobKind.Copy ? type.CopyHours : type.StandardHours) * quantity;
                var start = WorkCalendar.NextWorkingDay(cursor);
                var finish = WorkCalendar.FinishDate(start, hours);

                DateOnly? startedOn = null;
                DateOnly? approvedOn = null;

                if (finish < today)
                {
                    startedOn = start;
                    var delay = rng.NextDouble() < 0.75 ? 0 : rng.Next(1, 5);
                    var approved = finish.AddDays(delay);
                    // Anything that would be approved "in the future" is still open, i.e. overdue.
                    approvedOn = approved <= today ? approved : null;
                }
                else if (start <= today)
                {
                    startedOn = rng.NextDouble() < 0.85 ? start : null;
                }

                jobs.Add(new DesignJob
                {
                    Id = jobs.Count + 1,
                    ProjectId = projects[rng.Next(projects.Count)].Id,
                    DraftsmanId = draftsman.Id,
                    ModelTypeId = type.Id,
                    Kind = kind,
                    Quantity = quantity,
                    Remark = remarks[rng.Next(remarks.Length)],
                    PlannedStart = start,
                    PlannedFinish = finish,
                    StartedOn = startedOn,
                    ApprovedOn = approvedOn,
                });

                cursor = finish.AddDays(1);
            }
        }

        // Two draftsmen are always running late, so the demo shows an overdue job every day.
        foreach (var draftsmanId in new[] { 3, 6 })
        {
            var index = jobs.FindLastIndex(j => j.DraftsmanId == draftsmanId && j.PlannedFinish < today);
            if (index >= 0)
            {
                jobs[index] = jobs[index] with { ApprovedOn = null };
            }
        }

        return new RdSnapshot(brands, teams, draftsmen, modelTypes, projects, jobs);
    }
}
