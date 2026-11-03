using System.Diagnostics.Metrics;

namespace MealManager.Application.Common;

/// <summary>
/// Domain metrics via System.Diagnostics.Metrics (skill bmm-observability) — BCL-only, bridged to
/// Prometheus by prometheus-net's MeterAdapter. Counters carry a kind tag where one counter spans
/// several write paths.
/// </summary>
public static class BmmMetrics
{
    public const string MeterName = "Bmm";

    private static readonly Meter Meter = new(MeterName);

    public static readonly Counter<long> MealsRecorded = Meter.CreateCounter<long>(
        "bmm_meals_recorded_total", description: "Meal entries recorded (upsert batches).");

    public static readonly Counter<long> ExpensesRecorded = Meter.CreateCounter<long>(
        "bmm_expenses_recorded_total", description: "Money writes recorded (tagged by kind).");

    public static readonly Histogram<double> SummaryCalculatedDuration = Meter.CreateHistogram<double>(
        "bmm_summary_calculated_duration_seconds", unit: "s", description: "MessGrain summary latency (hot path).");

    public static readonly Counter<long> MessagesConsumed = Meter.CreateCounter<long>(
        "bmm_rabbitmq_messages_consumed_total", description: "Worker messages consumed.");

    public static readonly Counter<long> MessageFailures = Meter.CreateCounter<long>(
        "bmm_rabbitmq_message_failures_total", description: "Worker message handling failures.");

    public static void Expense(string kind) =>
        ExpensesRecorded.Add(1, new KeyValuePair<string, object?>("kind", kind));
}
