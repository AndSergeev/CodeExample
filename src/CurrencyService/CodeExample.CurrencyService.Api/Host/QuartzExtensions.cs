using CodeExample.CurrencyService.Api.HostedServices;
using CodeExample.CurrencyService.Infrastructure.Configuration;
using CodeExample.Shared.Database;
using CodeExample.Shared.Web;
using Quartz;

namespace CodeExample.CurrencyService.Api.Host;

internal static class QuartzExtensions
{
    private const string SyncJobKeyName = "currency-rate-sync";
    private const string SyncJobGroup = "synchronization";
    private const string SyncTriggerKeyName = "currency-rate-sync-trigger";

    public static IServiceCollection AddCurrencyRateSynchronization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<CurrencySyncOptions>(configuration);
        services.AddOptions<CurrencySyncStartupOptions>(configuration);

        var connectionString = configuration.RequireConnectionString(
            Infrastructure.DependencyInjection.ConnectionStringName);
        var syncOptions = configuration.ReadOptions<CurrencySyncOptions>();

        services.AddQuartz(quartz =>
        {
            quartz.SchedulerName = "codeexample-currency-sync";
            quartz.SchedulerId = "AUTO";

            quartz.UsePersistentStore(store =>
            {
                store.UseProperties = false;

                store.UseClustering(clustering =>
                {
                    clustering.CheckinInterval = TimeSpan.FromSeconds(15);
                    clustering.CheckinMisfireThreshold = TimeSpan.FromSeconds(60);
                });

                store.UsePostgres(postgres =>
                {
                    postgres.ConnectionString = connectionString;
                    postgres.TablePrefix = "qrtz_";
                });
                store.UseNewtonsoftJsonSerializer();
            });

            var jobKey = new JobKey(SyncJobKeyName, SyncJobGroup);

            quartz.AddJob<CurrencyRateSyncJob>(jobKey, job => job
                .WithIdentity(jobKey)
                .WithDescription("Забирает курсы на день у ЦБ РФ.")
                .StoreDurably());

            quartz.AddTrigger(trigger => trigger
                .WithIdentity(SyncTriggerKeyName, SyncJobGroup)
                .ForJob(jobKey)
                .WithDescription($"Запускается по расписанию '{syncOptions.Cron}' ({syncOptions.TimeZoneId}).")
                .WithCronSchedule(syncOptions.Cron, cron => cron
                    .InTimeZone(ResolveTimeZone(syncOptions.TimeZoneId))
                    .WithMisfireHandlingInstructionDoNothing()));
        });

        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }

    private static TimeZoneInfo ResolveTimeZone(string timeZoneId)
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (Exception exception) when (exception is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            // Молчаливый переход на UTC сдвинул бы прогон на смещение пояса и никак себя
            // не выдал, поэтому настройка, которую нельзя применить, останавливает запуск.
            throw new InvalidOperationException(
                $"Часовой пояс '{timeZoneId}' не найден на этом хосте. " +
                "Укажите пояс из базы хозяина, например Europe/Moscow." ,
                exception);
        }
    }
}
