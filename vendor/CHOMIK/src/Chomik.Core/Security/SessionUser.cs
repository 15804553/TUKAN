using Chomik.Core.Enums;

namespace Chomik.Core.Security;

public sealed class SessionUser
{
    public required string Login { get; init; }
    public required UserRole Role { get; init; }
    public int? ShiftNumber { get; init; }

    public bool IsGuest => Role is UserRole.Gosc1 or UserRole.Gosc2 or UserRole.Gosc3;

    public bool IsShiftAccount => Role is UserRole.Zmiana1 or UserRole.Zmiana2
        or UserRole.Zmiana3;

    public bool CanViewSensitiveData => IsShiftAccount || IsDcaJrgUser;

    public bool CanViewAllShifts => Role is UserRole.Pa or UserRole.DcaJrg;

    public bool CanEditGeneralViewDates => Role is UserRole.Pa;

    public bool CanEditGeneralViewStopien => Role is UserRole.Pa;

    public bool IsPaUser => Role is UserRole.Pa;

    public bool IsAdministrator => Role is UserRole.Administrator;

    public bool CanViewGeneralView => !IsAdministrator && !IsGuest;

    public bool CanCreatePersonnelList => IsPaUser || IsShiftAccount;

    /// <summary>PA ma widok ogólny domyślnie; przycisk nawigacji jest ukryty.</summary>
    public bool ShowGeneralViewNavButton => CanViewGeneralView && !IsPaUser;

    public bool IsDcaJrgUser => Role is UserRole.DcaJrg;

    public bool HideTelefonInGeneralView => IsShiftScoped || IsDcaJrgUser;

    public bool HideGeneralViewShiftColumn => IsShiftScoped;

    public bool CanEditGeneralViewShift => Role is UserRole.DcaJrg;

    public bool CanManagePermissionTypes => Role is UserRole.DcaJrg;

    public bool CanEditPersonnel => IsShiftAccount || IsGuest;

    public bool CanResetShiftPasswords => Role is UserRole.DcaJrg;

    public bool CanResetAllPasswords => Role is UserRole.Administrator;

    public bool CanManageSettings => Role is UserRole.DcaJrg;

    public bool CanCustomizeGeneralViewColumns => IsShiftAccount || IsDcaJrgUser;

    public bool ShowSettingsNavButton =>
        CanManageSettings || CanCustomizeGeneralViewColumns || CanManageExportPaths || IsGuest;

    /// <summary>Konto ograniczone do jednej zmiany (Zmiana 1–3 lub Gość 1–3).</summary>
    public bool IsShiftScoped => IsShiftAccount || IsGuest;

    /// <summary>Widok Planu urlopów; edycja Gościa zależy od blokady w ustawieniach.</summary>
    public bool CanManageUrlopPlan => IsShiftScoped;

    public bool CanViewGrafikNurkowy => IsShiftAccount || IsDcaJrgUser;

    public bool CanApproveGrafikNurkowy => IsDcaJrgUser;

    public bool CanViewKalendarz => IsShiftAccount || IsDcaJrgUser;

    public bool CanEditKalendarz => IsDcaJrgUser;

    public bool CanManageExportPaths => IsAdministrator;

    public bool CanAccessShift(int shiftNumber) =>
        CanViewAllShifts || (IsShiftScoped && ShiftNumber == shiftNumber);
}
