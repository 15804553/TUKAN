using BOBER.Data.Repositories;
using BOBER.Services.Settings;

namespace BOBER.Services.Tests.Settings;

public sealed class SettingsServiceKolorowanieTests
{
    [Fact]
    public async Task KolorowanieEdycjaPersonelu_DefaultsToFalse()
    {
        var service = new SettingsService(new MemoryUstawieniaRepository());

        Assert.False(await service.GetKolorowanieEdycjaPersoneluAsync());
    }

    [Fact]
    public async Task KolorowanieEdycjaPersonelu_RoundTrip()
    {
        var service = new SettingsService(new MemoryUstawieniaRepository());

        await service.SetKolorowanieEdycjaPersoneluAsync(true);

        Assert.True(await service.GetKolorowanieEdycjaPersoneluAsync());

        await service.SetKolorowanieEdycjaPersoneluAsync(false);

        Assert.False(await service.GetKolorowanieEdycjaPersoneluAsync());
    }

    private sealed class MemoryUstawieniaRepository : IUstawieniaRepository
    {
        private readonly Dictionary<string, string> _values = new(StringComparer.OrdinalIgnoreCase);

        public Task<string?> GetAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) ? v : null);

        public Task<int> GetIntAsync(string key, int defaultValue = 0, CancellationToken cancellationToken = default) =>
            Task.FromResult(_values.TryGetValue(key, out var v) && int.TryParse(v, out var n) ? n : defaultValue);

        public Task SetAsync(string key, string value, CancellationToken cancellationToken = default)
        {
            _values[key] = value;
            return Task.CompletedTask;
        }
    }
}
