using BOBER.Data;
using BOBER.Data.Database;
using BOBER.Data.Repositories;
using BOBER.Services.Auth;
using BOBER.Services.Database;
using BOBER.Services.Export;
using BOBER.Services.Grafik;
using BOBER.Services.GrafikNurkowy;
using BOBER.Services.Kalendarz;
using BOBER.Services.Personnel;
using BOBER.Services.Settings;
using BOBER.Services.Urlop;

namespace BOBER.Services;

/// <summary>Kompozycja serwisów i repozytoriów (ręczne DI) — jedna instancja na sesję aplikacji.</summary>
public sealed class AppServices
{
    public AppServices()
    {
        BoberOptions = new BoberDatabaseOptions();
        ChomikOptions = new ChomikDatabaseOptions();

        var boberFactory = new BoberConnectionFactory(BoberOptions);
        var chomikFactory = new ChomikConnectionFactory(ChomikOptions);
        var bootstrapper = new DatabaseBootstrapper(BoberOptions);

        var authRepository = new AuthRepository(boberFactory);
        var grafikRepository = new GrafikRepository(boberFactory);
        var grafikNotatkaRepository = new GrafikNotatkaRepository(boberFactory);
        var grafikUwagaMiesiecznaRepository = new GrafikUwagaMiesiecznaRepository(boberFactory);
        var urlopPlanRepository = new UrlopPlanRepository(boberFactory);
        var grafikNurkowyRepository = new GrafikNurkowyRepository(boberFactory);
        var kalendarzRepository = new KalendarzRepository(boberFactory);
        var kolejnoscRepository = new KolejnoscRepository(boberFactory);
        var koloryRepository = new KoloryRepository(boberFactory);
        var ustawieniaRepository = new UstawieniaRepository(boberFactory);
        var chomikRepository = new ChomikRepository(chomikFactory);

        Chomik = chomikRepository;
        Auth = new AuthService(authRepository, chomikRepository);
        Settings = new SettingsService(ustawieniaRepository);
        Kolory = koloryRepository;
        Kolejnosc = kolejnoscRepository;

        var calendarEngine = new ShiftCalendarEngine(ustawieniaRepository);
        Grafik = new GrafikService(grafikRepository, grafikNotatkaRepository, grafikUwagaMiesiecznaRepository);
        Calendar = calendarEngine;
        Funkcjonariusze = new FunkcjonariuszService(chomikRepository, kolejnoscRepository);
        Export = new ExportService();
        UrlopPlan = new UrlopPlanService(
            urlopPlanRepository,
            grafikRepository,
            calendarEngine,
            Funkcjonariusze,
            Settings,
            new UrlopPlanValidator(),
            new UrlopPlanExcelService());
        GrafikNurkowy = new GrafikNurkowyService(
            grafikRepository,
            grafikNurkowyRepository,
            calendarEngine,
            Funkcjonariusze,
            Settings,
            new GrafikNurkowyExcelService());
        Kalendarz = new KalendarzService(kalendarzRepository, koloryRepository, calendarEngine);
        Database = new DatabaseService(bootstrapper, ChomikOptions);
    }

    public BoberDatabaseOptions BoberOptions { get; }
    public ChomikDatabaseOptions ChomikOptions { get; }
    public IChomikRepository Chomik { get; }
    public IAuthService Auth { get; }
    public ISettingsService Settings { get; }
    public IKoloryRepository Kolory { get; }
    public IKolejnoscRepository Kolejnosc { get; }
    public IGrafikService Grafik { get; }
    public ShiftCalendarEngine Calendar { get; }
    public IFunkcjonariuszService Funkcjonariusze { get; }
    public ExportService Export { get; }
    public IUrlopPlanService UrlopPlan { get; }
    public IGrafikNurkowyService GrafikNurkowy { get; }
    public IKalendarzService Kalendarz { get; }
    public DatabaseService Database { get; }
}
