namespace Tukan.App.Services.GuestAudit;

/// <summary>Fasada audytu Gościa — używana z hooków po zalogowaniu.</summary>
public sealed class GuestAuditFacade
{
    private readonly GuestAuditLogService _log;
    private readonly GuestAuditSettingsService _settings;

    public GuestAuditFacade(GuestAuditLogService log, GuestAuditSettingsService settings)
    {
        _log = log;
        _settings = settings;
    }

    public GuestAuditLogService Log => _log;
    public GuestAuditSettingsService Settings => _settings;

    public bool IsGuestSession { get; private set; }
    public int? GuestShiftNumber { get; private set; }

    public void ActivateGuestSession(int shiftNumber)
    {
        IsGuestSession = true;
        GuestShiftNumber = shiftNumber;
    }

    public void ClearSession()
    {
        IsGuestSession = false;
        GuestShiftNumber = null;
    }

    public async Task TryAppendAsync(
        GuestAuditModule module,
        string message,
        CancellationToken cancellationToken = default)
    {
        if (!IsGuestSession || GuestShiftNumber is not int shift)
            return;

        var scope = await _settings.GetScopeAsync(shift, cancellationToken);
        if (!scope.IsEnabled(module))
            return;

        await _log.AppendAsync(shift, message, cancellationToken);
    }

    public Task<bool> IsUrlopPlanLockedAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default) =>
        _settings.GetUrlopPlanLockedAsync(shiftNumber, cancellationToken);

    public Task<bool> CanGuestManageGrafikAsync(
        int shiftNumber,
        CancellationToken cancellationToken = default) =>
        _settings.GetGuestCanManageGrafikAsync(shiftNumber, cancellationToken);
}
