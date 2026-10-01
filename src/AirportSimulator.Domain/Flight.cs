namespace AirportSimulator.Domain;

public sealed class Flight
{
    public Flight(string number, string origin, string destination, DateTime scheduledArrival, Aircraft aircraft, int passengers)
    {
        if (string.IsNullOrWhiteSpace(number)) throw new ArgumentException("Номер рейса обязателен.", nameof(number));
        if (passengers < 0 || passengers > aircraft.Capacity) throw new ArgumentOutOfRangeException(nameof(passengers));
        Number = number;
        Origin = origin;
        Destination = destination;
        ScheduledArrival = scheduledArrival;
        Aircraft = aircraft;
        Passengers = passengers;
    }

    public string Number { get; }
    public string Origin { get; }
    public string Destination { get; }
    public DateTime ScheduledArrival { get; }
    public DateTime ScheduledDeparture => ScheduledArrival.AddMinutes(40);
    public Aircraft Aircraft { get; }
    public int Passengers { get; }
    public FlightStatus Status { get; private set; } = FlightStatus.Scheduled;
    public int RemainingPhaseMinutes { get; private set; }
    public string? AssignedRunwayId { get; private set; }
    public string? AssignedGateId { get; private set; }
    public DateTime? ActualDeparture { get; private set; }
    public int DelayMinutes => ActualDeparture is null
        ? 0
        : Math.Max(0, (int)(ActualDeparture.Value - ScheduledDeparture).TotalMinutes);

    public void BeginApproach() => TransitionTo(FlightStatus.Approaching);

    public void BeginLanding(string runwayId, string gateId, int durationMinutes)
    {
        AssignedRunwayId = runwayId;
        AssignedGateId = gateId;
        TransitionTo(FlightStatus.Landing, durationMinutes);
    }

    public void ReachTaxiway()
    {
        AssignedRunwayId = null;
        TransitionTo(FlightStatus.TaxiingToGate, 4);
    }

    public void BeginService() => TransitionTo(FlightStatus.Servicing, 20);
    public void BeginBoarding() => TransitionTo(FlightStatus.Boarding, 10);
    public void WaitForTakeoff() => TransitionTo(FlightStatus.WaitingForTakeoff);

    public void BeginTakeoff(string runwayId, int durationMinutes)
    {
        AssignedRunwayId = runwayId;
        AssignedGateId = null;
        TransitionTo(FlightStatus.TakingOff, durationMinutes);
    }

    public void Complete(DateTime departureTime)
    {
        AssignedRunwayId = null;
        ActualDeparture = departureTime;
        TransitionTo(FlightStatus.Departed);
    }

    public bool AdvancePhaseMinute()
    {
        if (RemainingPhaseMinutes <= 0) return true;
        RemainingPhaseMinutes--;
        return RemainingPhaseMinutes == 0;
    }

    private void TransitionTo(FlightStatus status, int durationMinutes = 0)
    {
        Status = status;
        RemainingPhaseMinutes = durationMinutes;
    }
}
