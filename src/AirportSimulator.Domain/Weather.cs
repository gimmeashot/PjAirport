namespace AirportSimulator.Domain;

public enum WeatherCondition
{
    Clear,
    Rain,
    Fog,
    Storm
}

public sealed record Weather(WeatherCondition Condition, int VisibilityMeters, int WindSpeedKph)
{
    public static Weather Clear { get; } = new(WeatherCondition.Clear, 10_000, 8);
    public bool AllowsFlightOperations => Condition != WeatherCondition.Storm;
    public int OperationDurationMinutes => Condition switch
    {
        WeatherCondition.Clear => 3,
        WeatherCondition.Rain => 4,
        WeatherCondition.Fog => 5,
        WeatherCondition.Storm => 0,
        _ => 3
    };
}

