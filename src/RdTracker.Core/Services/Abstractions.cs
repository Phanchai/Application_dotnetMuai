using RdTracker.Core.Models;

namespace RdTracker.Core.Services;

public interface IClock
{
    DateOnly Today { get; }
}

public sealed class SystemClock : IClock
{
    public DateOnly Today => DateOnly.FromDateTime(DateTime.Now);
}

/// <summary>Thrown when a change breaks a business rule; the message is safe to show to the user.</summary>
public sealed class RdValidationException(string message) : Exception(message);

/// <summary>
/// The data seam of the app. The demo uses <see cref="InMemoryRdDataService"/>;
/// a REST client for a real backend would implement the same interface.
/// </summary>
public interface IRdDataService
{
    Task<RdSnapshot> GetSnapshotAsync();

    /// <summary>Creates the job when <c>Id</c> is 0, otherwise replaces the existing one.</summary>
    Task<DesignJob> SaveJobAsync(DesignJob job);

    Task DeleteJobAsync(int id);

    Task<DesignJob> StartJobAsync(int id, DateOnly on);

    Task<DesignJob> ApproveJobAsync(int id, DateOnly on);

    Task<Brand> AddBrandAsync(string code, string name);

    Task DeleteBrandAsync(int id);
}

public interface IAuthService
{
    Task<UserAccount?> SignInAsync(string username, string password);
}
