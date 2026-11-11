using RdTracker.Core.Models;

namespace RdTracker.Core.Services;

/// <summary>Demo accounts. Passwords are intentionally public — this is sample data.</summary>
public sealed class DemoAuthService : IAuthService
{
    public static readonly IReadOnlyList<(UserAccount Account, string Password)> Accounts =
    [
        (new UserAccount("manager", "Narin S.", Role.Manager), "manager1234"),
        (new UserAccount("draftsman", "Anan K.", Role.Draftsman, DraftsmanId: 1), "draftsman1234"),
    ];

    public Task<UserAccount?> SignInAsync(string username, string password)
    {
        var name = (username ?? "").Trim();
        var match = Accounts.FirstOrDefault(a =>
            string.Equals(a.Account.Username, name, StringComparison.OrdinalIgnoreCase) && a.Password == password);
        return Task.FromResult<UserAccount?>(match.Account);
    }
}

/// <summary>Who is signed in, and what they are allowed to do.</summary>
public sealed class SessionState
{
    public UserAccount? User { get; private set; }

    public event Action? Changed;

    public bool IsManager => User?.Role == Role.Manager;

    public void SignIn(UserAccount user)
    {
        User = user;
        Changed?.Invoke();
    }

    public void SignOut()
    {
        User = null;
        Changed?.Invoke();
    }

    /// <summary>Managers plan and edit every job.</summary>
    public bool CanEdit(DesignJob job) => IsManager;

    /// <summary>A draftsman may start their own jobs; managers may start any.</summary>
    public bool CanStart(DesignJob job) =>
        job.StartedOn is null && (IsManager || (User?.DraftsmanId is { } id && id == job.DraftsmanId));

    /// <summary>Only managers approve finished drawings.</summary>
    public bool CanApprove(DesignJob job) => IsManager && job.StartedOn is not null && job.ApprovedOn is null;
}
