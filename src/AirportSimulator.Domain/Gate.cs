namespace AirportSimulator.Domain;

public sealed class Gate(string id)
{
    public string Id { get; } = id;
    public string? OccupiedByFlight { get; private set; }
    public bool IsAvailable => OccupiedByFlight is null;
    public void Occupy(string flightNumber) => OccupiedByFlight = flightNumber;
    public void Release() => OccupiedByFlight = null;
}

