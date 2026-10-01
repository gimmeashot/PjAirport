using AirportSimulator.Application;
using AirportSimulator.Domain;

namespace AirportSimulator.Tests;

public sealed class SimulationEngineTests
{
    [Fact]
    public void Flight_completes_full_airport_cycle()
    {
        var start = new DateTime(2026, 10, 1, 8, 0, 0);
        var airport = CreateAirport();
        var engine = new SimulationEngine(airport, start);
        var flight = CreateFlight("T-001", start.AddMinutes(1));
        engine.AddFlight(flight);
        engine.Advance(60);
        Assert.Equal(FlightStatus.Departed, flight.Status);
        Assert.True(airport.Runways[0].IsAvailable);
        Assert.True(airport.Gates[0].IsAvailable);
    }

    [Fact]
    public void Second_flight_waits_when_resources_are_occupied()
    {
        var start = new DateTime(2026, 10, 1, 8, 0, 0);
        var airport = CreateAirport();
        var engine = new SimulationEngine(airport, start);
        var first = CreateFlight("T-001", start.AddMinutes(1));
        var second = CreateFlight("T-002", start.AddMinutes(1));
        engine.AddFlight(first);
        engine.AddFlight(second);
        engine.Advance(3);
        Assert.Equal(FlightStatus.Landing, first.Status);
        Assert.Equal(FlightStatus.Approaching, second.Status);
    }

    [Fact]
    public void Weather_changes_after_thirty_minutes_and_is_logged()
    {
        var start = new DateTime(2026, 10, 1, 8, 0, 0);
        var engine = new SimulationEngine(CreateAirport(), start, randomSeed: 42);

        engine.Advance(30);

        Assert.Contains(engine.Events, item => item.Message.StartsWith("Погода изменилась:"));
        Assert.NotEqual(Weather.Clear, engine.Airport.Weather);
    }

    [Fact]
    public void Report_contains_completed_flight_statistics()
    {
        var start = new DateTime(2026, 10, 1, 8, 0, 0);
        var engine = new SimulationEngine(CreateAirport(), start);
        engine.AddFlight(CreateFlight("T-001", start.AddMinutes(1)));

        engine.Advance(90);
        var report = engine.CreateReport();

        Assert.Equal(1, report.DepartedFlights);
        Assert.Equal(100, report.ProcessedPassengers);
        Assert.True(report.AverageDelayMinutes >= 0);
    }

    private static Airport CreateAirport() => new("Test", "TST", [new Runway("01", 3_000)], [new Gate("A1")]);
    private static Flight CreateFlight(string number, DateTime arrival) =>
        new(number, "AAA", "BBB", arrival, new Aircraft($"REG-{number}", "A320", 180, 2_000), 100);
}
