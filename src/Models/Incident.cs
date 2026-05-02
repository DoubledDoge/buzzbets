namespace BuzzBets.Models;

using Enums;

internal sealed record Incident(
	Drone Drone,
	IncidentType Type,
	double AtDistance,
	DateTime OccurredAt
);
