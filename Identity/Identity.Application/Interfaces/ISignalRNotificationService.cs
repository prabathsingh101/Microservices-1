namespace Identity.Application.Interfaces;

public interface ISignalRNotificationService
{
    Task SendForceLogoutAsync(string userId, CancellationToken ct);
}
