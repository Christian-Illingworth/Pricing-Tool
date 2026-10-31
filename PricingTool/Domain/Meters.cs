namespace PricingTool.Domain;

public static class Meters
{
    // Measurement classes C, E, F and G are half-hourly (research R15).
    private static readonly HashSet<string> HalfHourlyMeasurementClasses = new(StringComparer.OrdinalIgnoreCase) { "C", "E", "F", "G" };

    // Profile class 00, or a half-hourly measurement class (FR-018).
    public static bool IsHalfHourly(int? profileClass, string? measurementClass) =>
        profileClass == 0
        || (!string.IsNullOrWhiteSpace(measurementClass) && HalfHourlyMeasurementClasses.Contains(measurementClass.Trim()));
}
