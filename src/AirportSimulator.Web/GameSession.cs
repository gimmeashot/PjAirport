using AirportSimulator.Application;
using AirportSimulator.Domain;

namespace AirportSimulator.Web;

public sealed class GameSession
{
    private readonly HashSet<string> _paidFlights = [];
    private readonly HashSet<string> _priorityFlights = [];

    public GameSession() => Reset();
    public SimulationEngine Engine { get; private set; } = null!;
    public DateTime ShiftEndsAt { get; private set; }
    public int Balance { get; private set; }
    public int Reputation { get; private set; }
    public int Score { get; private set; }
    public int TargetDepartures { get; } = 10;
    public bool IsOver { get; private set; }
    public bool IsWon { get; private set; }
    public string ResultMessage { get; private set; } = string.Empty;
    public const int PriorityCost = 2_500;

    public void Advance(int minutes)
    {
        if (minutes <= 0 || IsOver) return;

        for (var minute = 0; minute < minutes && !IsOver; minute++)
        {
            Engine.Advance(1);
            Balance -= 60;
            ProcessCompletedFlights();
            CheckGameEnd();
        }
    }

    public bool CanPrioritize(Flight flight) =>
        !IsOver && Balance >= PriorityCost && !_priorityFlights.Contains(flight.Number) &&
        flight.Status is FlightStatus.Scheduled or FlightStatus.Approaching;

    public bool IsPriority(Flight flight) => _priorityFlights.Contains(flight.Number);

    public void Prioritize(Flight flight)
    {
        if (!CanPrioritize(flight)) return;
        Balance -= PriorityCost;
        _priorityFlights.Add(flight.Number);
        Engine.Airport.Flights.Remove(flight);
        Engine.Airport.Flights.Insert(0, flight);
    }

    public void Reset()
    {
        var startTime = new DateTime(2026, 10, 1, 8, 0, 0);
        _paidFlights.Clear();
        _priorityFlights.Clear();
        ShiftEndsAt = startTime.AddHours(4);
        Balance = 30_000;
        Reputation = 80;
        Score = 0;
        IsOver = false;
        IsWon = false;
        ResultMessage = string.Empty;
        var airport = new Airport("Международный аэропорт", "CMA",
            [new Runway("01/19", 3_200), new Runway("08/26", 2_600)],
            [new Gate("A1"), new Gate("A2"), new Gate("B1")]);
        var aircraft = new Aircraft("RA-73001", "Airbus A320", 180, 2_100);
        var boeing = new Aircraft("RA-73111", "Boeing 737-800", 189, 2_200);
        var regional = new Aircraft("RA-89051", "Sukhoi Superjet 100", 103, 1_800);
        Engine = new SimulationEngine(airport, startTime);
        Engine.AddFlight(new Flight("SU-101", "Москва", "Сочи", startTime.AddMinutes(2), aircraft, 154));
        Engine.AddFlight(new Flight("DP-204", "Санкт-Петербург", "Казань", startTime.AddMinutes(5), aircraft with { Registration = "RA-73002" }, 167));
        Engine.AddFlight(new Flight("S7-315", "Новосибирск", "Москва", startTime.AddMinutes(8), aircraft with { Registration = "RA-73003" }, 143));
        Engine.AddFlight(new Flight("U6-442", "Екатеринбург", "Самара", startTime.AddMinutes(12), aircraft with { Registration = "RA-73004" }, 176));
        Engine.AddFlight(new Flight("FV-507", "Сочи", "Санкт-Петербург", startTime.AddMinutes(16), boeing, 181));
        Engine.AddFlight(new Flight("N4-218", "Минеральные Воды", "Москва", startTime.AddMinutes(20), boeing with { Registration = "RA-73112" }, 172));
        Engine.AddFlight(new Flight("5N-633", "Казань", "Мурманск", startTime.AddMinutes(24), boeing with { Registration = "RA-73113" }, 164));
        Engine.AddFlight(new Flight("EO-781", "Уфа", "Калининград", startTime.AddMinutes(28), boeing with { Registration = "RA-73114" }, 179));
        Engine.AddFlight(new Flight("SU-1420", "Москва", "Пермь", startTime.AddMinutes(33), regional, 96));
        Engine.AddFlight(new Flight("RT-595", "Нижний Новгород", "Саратов", startTime.AddMinutes(38), regional with { Registration = "RA-89052" }, 88));
        Engine.AddFlight(new Flight("WZ-1045", "Омск", "Екатеринбург", startTime.AddMinutes(43), regional with { Registration = "RA-89053" }, 101));
        Engine.AddFlight(new Flight("IO-312", "Иркутск", "Красноярск", startTime.AddMinutes(48), regional with { Registration = "RA-89054" }, 92));
    }

    private void ProcessCompletedFlights()
    {
        foreach (var flight in Engine.Airport.Flights.Where(f => f.Status == FlightStatus.Departed && _paidFlights.Add(f.Number)))
        {
            Balance += 2_000 + flight.Passengers * 25 - flight.DelayMinutes * 120;
            Score += Math.Max(100, 1_000 - flight.DelayMinutes * 25);
            var reputationChange = flight.DelayMinutes switch
            {
                <= 5 => 2,
                <= 20 => 0,
                _ => -2
            };
            Reputation = Math.Clamp(Reputation + reputationChange, 0, 100);
        }
    }

    private void CheckGameEnd()
    {
        if (Balance <= 0 || Reputation <= 0)
        {
            IsOver = true;
            ResultMessage = Balance <= 0 ? "Бюджет аэропорта исчерпан." : "Репутация аэропорта упала до нуля.";
            return;
        }

        if (Engine.CurrentTime < ShiftEndsAt) return;

        IsOver = true;
        IsWon = Engine.CreateReport().DepartedFlights >= TargetDepartures && Reputation >= 50;
        ResultMessage = IsWon
            ? "Смена завершена успешно. План выполнен."
            : "Смена завершена. План по рейсам или репутации не выполнен.";
    }
}
