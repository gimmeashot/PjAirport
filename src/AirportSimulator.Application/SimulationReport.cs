namespace AirportSimulator.Application;

public sealed record SimulationReport(
    int TotalFlights,
    int DepartedFlights,
    int ActiveFlights,
    int ScheduledFlights,
    double AverageDelayMinutes,
    int MaximumDelayMinutes,
    int ProcessedPassengers);

