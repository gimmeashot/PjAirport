namespace AirportSimulator.Domain;

public sealed class Runway(string id, int lengthMeters)
{
    public string Id { get; } = id;
    public int LengthMeters { get; } = lengthMeters;
    public string? OccupiedByFlight { get; private set; }
    public bool IsAvailable => OccupiedByFlight is null;
    public bool CanAccept(Aircraft aircraft) => IsAvailable && LengthMeters >= aircraft.RequiredRunwayLengthMeters;
    public void Occupy(string flightNumber) => OccupiedByFlight = flightNumber;
    public void Release() => OccupiedByFlight = null;
}

