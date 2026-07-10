using Chomik.Data;
using Chomik.Data.Database;
using Chomik.Data.Repositories;
using Chomik.Services.Database;
using Chomik.Services.Personnel;
using Chomik.Services.Security;
using Chomik.Services.Settings;

namespace Chomik.Services;

public sealed class AppServices
{
    public AppServices()
    {
        DatabaseOptions = new DatabaseOptions();
        var connectionFactory = new AccessConnectionFactory(DatabaseOptions);
        var bootstrapper = new DatabaseBootstrapper(DatabaseOptions);

        var userRepository = new UserRepository(connectionFactory);
        var settingsRepository = new SettingsRepository(connectionFactory);
        var slownikRepository = new SlownikRepository(connectionFactory);
        var funkcjonariuszRepository = new FunkcjonariuszRepository(connectionFactory);

        Settings = new SettingsService(settingsRepository);
        UprawnieniaSlownik = new UprawnieniaSlownikService(slownikRepository);
        Auth = new AuthService(userRepository);
        UserAccounts = new UserAccountService(userRepository);
        Funkcjonariusze = new FunkcjonariuszService(funkcjonariuszRepository, slownikRepository);
        Database = new DatabaseService(DatabaseOptions, bootstrapper, funkcjonariuszRepository);
        ConnectionFactory = connectionFactory;
    }

    public ISettingsService Settings { get; }
    public UprawnieniaSlownikService UprawnieniaSlownik { get; }
    public IAuthService Auth { get; }
    public IUserAccountService UserAccounts { get; }
    public IFunkcjonariuszService Funkcjonariusze { get; }
    public DatabaseService Database { get; }
    public DatabaseOptions DatabaseOptions { get; }
    public AccessConnectionFactory ConnectionFactory { get; }
}
