namespace BuzzBets.Models;

internal sealed record RaceResult(
	List<Drone> FinishOrder,
	List<Drone> Podium,
	List<Incident> Incidents
);
