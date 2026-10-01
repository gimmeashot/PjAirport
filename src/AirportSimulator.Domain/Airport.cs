namespace AirportSimulator.Domain;

public sealed class Airport(string name, string code, IEnumerable<Runway> runways, IEnumerable<Gate> gates)
{
    public string Name { get; } = name;
    public string Code { get; } = code;
    public List<Runway> Runways { get; } = [.. runways];
    public List<Gate> Gates { get; } = [.. gates];
    public List<Flight> Flights { get; } = [];
    public Weather Weather { get; private set; } = Weather.Clear;
    public void ChangeWeather(Weather weather) => Weather = weather;
}
