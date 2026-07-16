using Chomik.Core.Enums;

namespace Chomik.Core.Security;

public sealed class SessionUser
{
    public required string Login { get; init; }
    public required UserRole Role { get; init; }
    public int? ShiftNumber { get; init; }

    public bool CanViewSensitiveData => Role is UserRole.Zmiana1 or UserRole.Zmiana2
        or UserRole.Zmiana3 or UserRole.DcaJrg;

    public bool CanViewAllShifts => Role is UserRole.Pa or UserRole.DcaJrg;

    public bool CanEditGeneralViewDates => Role is UserRole.Pa;

    public bool CanEditGeneralViewStopien => Role is UserRole.Pa;

    public bool IsPaUser => Role is UserRole.Pa;

    public bool IsAdministrator => Role is UserRole.Administrator;

    public bool CanViewGeneralView => !IsAdministrator;

    public bool CanCreatePersonnelList => IsPaUser || IsShiftScoped;

    /// <summary>PA ma widok ogólny domyślnie; przycisk nawigacji jest ukryty.</summary>
    public bool ShowGeneralViewNavButton => CanViewGeneralView && !IsPaUser;

    public bool IsDcaJrgUser => Role is UserRole.DcaJrg;

    public bool HideTelefonInGeneralView => IsShiftScoped || IsDcaJrgUser;

    public bool HideGeneralViewShiftColumn => IsShiftScoped;

    public bool CanEditGeneralViewShift => Role is UserRole.DcaJrg;

    public bool CanManagePermissionTypes => Role is UserRole.DcaJrg;

    public bool CanEditPersonnel => Role is UserRole.Zmiana1 or UserRole.Zmiana2
        or UserRole.Zmiana3;

    public bool CanResetShiftPasswords => Role is UserRole.DcaJrg;

    public bool CanResetAllPasswords => Role is UserRole.Administrator;

    public bool CanManageSettings => Role is UserRole.DcaJrg;

    public bool CanCustomizeGeneralViewColumns => IsShiftScoped || IsDcaJrgUser;

    public bool ShowSettingsNavButton =>
        CanManageSettings || CanCustomizeGeneralViewColumns || CanManageExportPaths;

    public bool IsShiftScoped => Role is UserRole.Zmiana1 or UserRole.Zmiana2
        or UserRole.Zmiana3;

    public bool CanManageUrlopPlan => IsShiftScoped;

    public bool CanViewGrafikNurkowy => IsShiftScoped || IsDcaJrgUser;

    public bool CanApproveGrafikNurkowy => IsDcaJrgUser;

    public bool CanManageExportPaths => IsAdministrator;

    public bool CanAccessShift(int shiftNumber) =>
        CanViewAllShifts || (IsShiftScoped && ShiftNumber == shiftNumber);
}
