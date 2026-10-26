using SKRYBEK.Core.Enums;
using SKRYBEK.Core.Rules;
using SKRYBEK.Data.Repositories;
using SKRYBEK.Services.Logging;

namespace SKRYBEK.Services.Rozkaz;

public sealed class KalendarzUwagiRozkazuService(RozkazRepository repo)
{
    public async Task<DopisUwagKalendarzaResult> DopiszAsync(DateOnly data, string tresc)
    {
        var notatka = tresc?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(notatka))
            return DopisUwagKalendarzaResult.PustaTresc;

        var rozkaz = await repo.GetByDataAsync(data);
        if (rozkaz is null)
            return DopisUwagKalendarzaResult.BrakRozkazu;

        if (rozkaz.CzyZablokowany)
            return DopisUwagKalendarzaResult.Zablokowany;

        var polaczone = RozkazUwagiRules.DopiszNotatke(rozkaz.Uwagi, notatka);
        if (polaczone == (rozkaz.Uwagi ?? string.Empty).TrimEnd())
            return DopisUwagKalendarzaResult.JuzZawiera;

        await repo.UpdateUwagiAsync(rozkaz.Id, polaczone);
        SkrybekLog.Info($"Dopisano uwagę z kalendarza do rozkazu nr {rozkaz.NumerFormatowany} ({data:dd.MM.yyyy})");
        return DopisUwagKalendarzaResult.Dopisano;
    }
}
