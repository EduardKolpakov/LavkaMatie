using System;
using System.IO;
using Avalonia;
using KLALIK.Data;
using KLALIK.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace KLALIK;

internal static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        ServiceProvider = BuildServices();
        try
        {
            var initializer = ServiceProvider.GetRequiredService<DatabaseInitializer>();
            initializer.MigrateAndSeedAsync().GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Ошибка базы данных: " + ex.Message);
            Console.Error.WriteLine(
                "Убедитесь, что PostgreSQL запущен и в appsettings.json указаны верные Host, Database, Username, Password.");
            return;
        }

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    private static IServiceProvider BuildServices()
    {
        var basePath = AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var cs = sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("В appsettings.json не задан ConnectionStrings:DefaultConnection.");
            options.UseNpgsql(cs).UseSnakeCaseNamingConvention();
        });
        services.AddSingleton<AuthSession>();
        services.AddSingleton<DatabaseInitializer>();
        services.AddTransient<MainWindow>();
        return services.BuildServiceProvider();
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
