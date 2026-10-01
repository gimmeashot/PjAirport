using AirportSimulator.Domain;

namespace AirportSimulator.Application;

public sealed class SimulationEngine
{
    private readonly List<SimulationEvent> _events = [];
    private readonly Random _random;
    private DateTime _nextWeatherChange;

    public SimulationEngine(Airport airport, DateTime startTime, int randomSeed = 42)
    {
        Airport = airport;
        CurrentTime = startTime;
        _random = new Random(randomSeed);
        _nextWeatherChange = startTime.AddMinutes(30);
    }

    public Airport Airport { get; }
    public DateTime CurrentTime { get; private set; }
    public IReadOnlyList<SimulationEvent> Events => _events;

    public void AddFlight(Flight flight) => Airport.Flights.Add(flight);

    public SimulationReport CreateReport()
    {
        var departed = Airport.Flights.Where(f => f.Status == FlightStatus.Departed).ToArray();
        var active = Airport.Flights.Count(f => f.Status is not FlightStatus.Scheduled and not FlightStatus.Departed);
        return new SimulationReport(
            Airport.Flights.Count,
            departed.Length,
            active,
            Airport.Flights.Count(f => f.Status == FlightStatus.Scheduled),
            departed.Length == 0 ? 0 : departed.Average(f => f.DelayMinutes),
            departed.Length == 0 ? 0 : departed.Max(f => f.DelayMinutes),
            departed.Sum(f => f.Passengers));
    }

    public void Advance(int minutes)
    {
        if (minutes <= 0) throw new ArgumentOutOfRangeException(nameof(minutes));
        for (var i = 0; i < minutes; i++)
        {
            CurrentTime = CurrentTime.AddMinutes(1);
            ProcessMinute();
        }
    }

    private void ProcessMinute()
    {
        UpdateWeatherIfNeeded();
        foreach (var flight in Airport.Flights.Where(f => f.Status != FlightStatus.Departed))
        {
            switch (flight.Status)
            {
                case FlightStatus.Scheduled when flight.ScheduledArrival <= CurrentTime:
                    flight.BeginApproach();
                    Log($"{flight.Number}: вошёл в зону аэропорта.");
                    break;
                case FlightStatus.Approaching:
                    TryBeginLanding(flight);
                    break;
                case FlightStatus.Landing when flight.AdvancePhaseMinute():
                    ReleaseRunway(flight.AssignedRunwayId);
                    flight.ReachTaxiway();
                    Log($"{flight.Number}: освободил полосу и рулит к гейту {flight.AssignedGateId}.");
                    break;
                case FlightStatus.TaxiingToGate when flight.AdvancePhaseMinute():
                    flight.BeginService();
                    Log($"{flight.Number}: началось наземное обслуживание.");
                    break;
                case FlightStatus.Servicing when flight.AdvancePhaseMinute():
                    flight.BeginBoarding();
                    Log($"{flight.Number}: началась посадка пассажиров.");
                    break;
                case FlightStatus.Boarding when flight.AdvancePhaseMinute():
                    flight.WaitForTakeoff();
                    Log($"{flight.Number}: готов к вылету.");
                    break;
                case FlightStatus.WaitingForTakeoff:
                    TryBeginTakeoff(flight);
                    break;
                case FlightStatus.TakingOff when flight.AdvancePhaseMinute():
                    ReleaseRunway(flight.AssignedRunwayId);
                    flight.Complete(CurrentTime);
                    Log($"{flight.Number}: вылетел в {flight.Destination}.");
                    break;
            }
        }
    }

    private void TryBeginLanding(Flight flight)
    {
        if (!Airport.Weather.AllowsFlightOperations) return;
        var runway = Airport.Runways.FirstOrDefault(r => r.CanAccept(flight.Aircraft));
        var gate = Airport.Gates.FirstOrDefault(g => g.IsAvailable);
        if (runway is null || gate is null) return;
        runway.Occupy(flight.Number);
        gate.Occupy(flight.Number);
        flight.BeginLanding(runway.Id, gate.Id, Airport.Weather.OperationDurationMinutes);
        Log($"{flight.Number}: посадка на полосу {runway.Id}, назначен гейт {gate.Id}.");
    }

    private void TryBeginTakeoff(Flight flight)
    {
        if (!Airport.Weather.AllowsFlightOperations) return;
        var runway = Airport.Runways.FirstOrDefault(r => r.CanAccept(flight.Aircraft));
        if (runway is null) return;
        runway.Occupy(flight.Number);
        ReleaseGate(flight.AssignedGateId);
        flight.BeginTakeoff(runway.Id, Airport.Weather.OperationDurationMinutes);
        Log($"{flight.Number}: начал взлёт с полосы {runway.Id}.");
    }

    private void ReleaseRunway(string? id) => Airport.Runways.FirstOrDefault(r => r.Id == id)?.Release();
    private void ReleaseGate(string? id) => Airport.Gates.FirstOrDefault(g => g.Id == id)?.Release();
    private void Log(string message) => _events.Add(new SimulationEvent(CurrentTime, message));

    private void UpdateWeatherIfNeeded()
    {
        if (CurrentTime < _nextWeatherChange) return;

        var roll = _random.Next(100);
        var weather = roll switch
        {
            < 55 => new Weather(WeatherCondition.Clear, _random.Next(8_000, 15_001), _random.Next(2, 16)),
            < 75 => new Weather(WeatherCondition.Rain, _random.Next(3_000, 8_001), _random.Next(10, 31)),
            < 90 => new Weather(WeatherCondition.Fog, _random.Next(400, 2_001), _random.Next(0, 11)),
            _ => new Weather(WeatherCondition.Storm, _random.Next(1_000, 4_001), _random.Next(35, 71))
        };
        Airport.ChangeWeather(weather);
        _nextWeatherChange = CurrentTime.AddMinutes(30);
        Log($"Погода изменилась: {GetWeatherName(Airport.Weather.Condition)}, видимость {Airport.Weather.VisibilityMeters} м, ветер {Airport.Weather.WindSpeedKph} км/ч.");
    }

    private static string GetWeatherName(WeatherCondition condition) => condition switch
    {
        WeatherCondition.Clear => "ясно",
        WeatherCondition.Rain => "дождь",
        WeatherCondition.Fog => "туман",
        WeatherCondition.Storm => "шторм",
        _ => condition.ToString()
    };
}
