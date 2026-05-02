namespace BuzzBets.Services;

using Constants;
using Models;
using Models.Enums;

internal static class IncidentResolver
{
	private static readonly Random Random = new();

	public static Incident? TryResolve(Drone drone)
	{
		double incidentChance =
			(1.0 - drone.Reliability / (double)GameConstants.StatMax)
			* GameConstants.IncidentChanceScale;

		if (Random.NextDouble() > incidentChance)
		{
			return null;
		}

		IncidentType type = RollIncidentType();
		ApplyEffect(drone, type);

		return new Incident(
			Drone: drone,
			Type: type,
			AtDistance: drone.DistanceCovered,
			OccurredAt: DateTime.Now
		);
	}

	private static IncidentType RollIncidentType() =>
		Random.NextDouble() switch
		{
			< 0.33 => IncidentType.DNF,
			< 0.66 => IncidentType.SpeedPenalty,
			_ => IncidentType.TemporaryStall,
		};

	private static void ApplyEffect(Drone drone, IncidentType type)
	{
		switch (type)
		{
			case IncidentType.DNF:
				drone.Status = DroneStatus.DNF;
				break;

			case IncidentType.SpeedPenalty:
				drone.SpeedMultiplier = GameConstants.SpeedPenaltyMultiplier;
				break;

			case IncidentType.TemporaryStall:
				drone.Status = DroneStatus.Stalled;
				drone.StallTicksRemaining = Random.Next(
					GameConstants.StallMinTicks,
					GameConstants.StallMaxTicks + 1
				);
				break;
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, null);
		}
	}
}
