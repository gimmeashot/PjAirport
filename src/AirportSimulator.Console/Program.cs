using AirportSimulator.Application;
using AirportSimulator.Domain;

Console.OutputEncoding = System.Text.Encoding.UTF8;
var startTime = new DateTime(2026, 10, 1, 8, 0, 0);
var airport = new Airport("Международный аэропорт", "CMA",
    [new Runway("01/19", 3_200), new Runway("08/26", 2_600)],
    [new Gate("A1"), new Gate("A2"), new Gate("B1")]);
var engine = new SimulationEngine(airport, startTime);
var aircraft = new Aircraft("RA-73001", "Airbus A320", 180, 2_100);
engine.AddFlight(new Flight("SU-101", "Москва", "Сочи", startTime.AddMinutes(2), aircraft, 154));
engine.AddFlight(new Flight("DP-204", "Санкт-Петербург", "Казань", startTime.AddMinutes(5), aircraft with { Registration = "RA-73002" }, 167));
engine.AddFlight(new Flight("S7-315", "Новосибирск", "Москва", startTime.AddMinutes(8), aircraft with { Registration = "RA-73003" }, 143));
engine.AddFlight(new Flight("U6-442", "Екатеринбург", "Самара", startTime.AddMinutes(12), aircraft with { Registration = "RA-73004" }, 176));

Console.WriteLine("============================================");
Console.WriteLine("       СИМУЛЯТОР АЭРОПОРТА — MVP");
Console.WriteLine("============================================");
PrintHelp();
PrintStatus(engine);

while (true)
{
    Console.Write("\nairport> ");
    var parts = (Console.ReadLine() ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
    var command = parts.FirstOrDefault()?.ToLowerInvariant();
    switch (command)
    {
        case "step":
            var minutes = parts.Length > 1 && int.TryParse(parts[1], out var value) ? value : 1;
            if (minutes <= 0) { Console.WriteLine("Количество минут должно быть больше нуля."); break; }
            var previousEventCount = engine.Events.Count;
            engine.Advance(minutes);
            foreach (var item in engine.Events.Skip(previousEventCount))
                Console.WriteLine($"[{item.Time:HH:mm}] {item.Message}");
            PrintStatus(engine);
            break;
        case "run":
            var duration = parts.Length > 1 && int.TryParse(parts[1], out var runValue) ? runValue : 60;
            if (duration <= 0) { Console.WriteLine("Количество минут должно быть больше нуля."); break; }
            engine.Advance(duration);
            PrintStatus(engine);
            Console.WriteLine($"Симуляция продвинута на {duration} мин. Команда events покажет журнал.");
            break;
        case "status": PrintStatus(engine); break;
        case "flights": PrintFlights(engine); break;
        case "weather": PrintWeather(engine); break;
        case "report": PrintReport(engine); break;
        case "events":
            foreach (var item in engine.Events.TakeLast(20)) Console.WriteLine($"[{item.Time:HH:mm}] {item.Message}");
            break;
        case "help": case "?": PrintHelp(); break;
        case "exit": case "quit": return;
        case "": break;
        default: Console.WriteLine("Неизвестная команда. Введите help."); break;
    }
}

static void PrintHelp() => Console.WriteLine("Команды: step [мин], run [мин], status, flights, weather, report, events, help, exit");

static void PrintStatus(SimulationEngine engine)
{
    var active = engine.Airport.Flights.Count(f => f.Status != FlightStatus.Scheduled && f.Status != FlightStatus.Departed);
    var departed = engine.Airport.Flights.Count(f => f.Status == FlightStatus.Departed);
    Console.WriteLine($"\n{engine.Airport.Code} | {engine.CurrentTime:dd.MM.yyyy HH:mm} | Активных: {active} | Вылетело: {departed}");
    Console.WriteLine("Полосы: " + string.Join(" | ", engine.Airport.Runways.Select(r => $"{r.Id}: {(r.IsAvailable ? "свободна" : r.OccupiedByFlight)}")));
    Console.WriteLine("Гейты:  " + string.Join(" | ", engine.Airport.Gates.Select(g => $"{g.Id}: {(g.IsAvailable ? "свободен" : g.OccupiedByFlight)}")));
    Console.WriteLine($"Погода:  {GetWeatherName(engine.Airport.Weather.Condition)}, видимость {engine.Airport.Weather.VisibilityMeters} м, ветер {engine.Airport.Weather.WindSpeedKph} км/ч");
}

static void PrintFlights(SimulationEngine engine)
{
    Console.WriteLine("Рейс     Прибытие  Маршрут                         Статус");
    Console.WriteLine(new string('-', 72));
    foreach (var flight in engine.Airport.Flights)
        Console.WriteLine($"{flight.Number,-8} {flight.ScheduledArrival:HH:mm}     {flight.Origin + " → " + flight.Destination,-31} {flight.Status}");
}

static void PrintWeather(SimulationEngine engine)
{
    var weather = engine.Airport.Weather;
    Console.WriteLine($"{GetWeatherName(weather.Condition)}: видимость {weather.VisibilityMeters} м, ветер {weather.WindSpeedKph} км/ч.");
    Console.WriteLine(weather.AllowsFlightOperations
        ? $"Полёты разрешены, длительность операции на полосе: {weather.OperationDurationMinutes} мин."
        : "Аэропорт временно не принимает и не выпускает самолёты.");
}

static void PrintReport(SimulationEngine engine)
{
    var report = engine.CreateReport();
    Console.WriteLine("ОТЧЁТ АЭРОПОРТА");
    Console.WriteLine($"Всего рейсов: {report.TotalFlights}; вылетело: {report.DepartedFlights}; активных: {report.ActiveFlights}; ожидается: {report.ScheduledFlights}");
    Console.WriteLine($"Обслужено пассажиров: {report.ProcessedPassengers}");
    Console.WriteLine($"Средняя задержка: {report.AverageDelayMinutes:F1} мин; максимальная: {report.MaximumDelayMinutes} мин");
}

static string GetWeatherName(WeatherCondition condition) => condition switch
{
    WeatherCondition.Clear => "Ясно",
    WeatherCondition.Rain => "Дождь",
    WeatherCondition.Fog => "Туман",
    WeatherCondition.Storm => "Шторм",
    _ => condition.ToString()
};
