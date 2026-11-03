using MealManager.Application.Abstractions;
using MealManager.Workers.Consuming;
using MealManager.Workers.Emails;
using MealManager.Workers.Transport;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using QuestPDF;
using QuestPDF.Infrastructure;
using RabbitMQ.Client;

namespace MealManager.Workers;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the async pipeline: transport (RabbitMQ when ConnectionStrings:rabbitmq exists,
    /// in-process channel fallback otherwise), the two worker services, the email sender (Resend when
    /// Resend:ApiKey exists, logging null sender otherwise) and — in broker mode — the consumer
    /// bridges plus a ready-tagged RabbitMQ health check.
    /// </summary>
    public static IServiceCollection AddBmmWorkers(this IServiceCollection services, IConfiguration configuration)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        services.Configure<ResendOptions>(configuration.GetSection("Resend"));

        services.AddSingleton<InProcessEventBus>();

        var rabbitConnection = configuration.GetConnectionString("rabbitmq");
        var useRabbit = !string.IsNullOrEmpty(rabbitConnection);
        if (useRabbit)
        {
            services.AddSingleton<IConnectionFactory>(_ => new ConnectionFactory
            {
                Uri = new Uri(rabbitConnection!),
                AutomaticRecoveryEnabled = true,
            });
            services.AddSingleton<RabbitMqEventPublisher>();
            services.AddSingleton<IEventPublisher>(sp => sp.GetRequiredService<RabbitMqEventPublisher>());
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<RabbitMqConsumerBridge>(sp,
                RabbitMqTopology.NotificationsQueue));
            services.AddHostedService(sp => ActivatorUtilities.CreateInstance<RabbitMqConsumerBridge>(sp,
                RabbitMqTopology.ReportsQueue));
            services.AddHealthChecks()
                .AddCheck<RabbitMqHealthCheck>("rabbitmq", tags: ["ready"]);
        }
        else
        {
            services.AddSingleton<IEventPublisher, InProcessEventPublisher>();
        }

        services.AddHttpClient<ResendEmailSender>((sp, client) =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ResendOptions>>().Value;
            client.BaseAddress = new Uri("https://api.resend.com");
            client.DefaultRequestHeaders.Authorization = new("Bearer", options.ApiKey);
        });
        if (!string.IsNullOrEmpty(configuration["Resend:ApiKey"]))
        {
            services.AddScoped<IEmailSender>(sp => sp.GetRequiredService<ResendEmailSender>());
        }
        else
        {
            services.AddScoped<IEmailSender, NullEmailSender>();
        }

        services.AddHostedService<NotificationWorker>();
        services.AddHostedService<ReportWorker>();
        return services;
    }

    /// <summary>Ready-tagged connectivity probe (broker mode only): open/ping a short-lived connection.</summary>
    private sealed class RabbitMqHealthCheck(IConnectionFactory connectionFactory) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
                return connection.IsOpen
                    ? HealthCheckResult.Healthy()
                    : HealthCheckResult.Degraded("RabbitMQ connection closed immediately");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Degraded($"RabbitMQ unreachable: {ex.Message}");
            }
        }
    }
}
