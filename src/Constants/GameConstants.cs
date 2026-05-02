namespace BuzzBets.Constants;

internal static class GameConstants
{
	// Race
	public const int TrackLengthKm = 10;
	public const int CheckpointCount = 5;
	public const int TickIntervalMs = 500;
	public const int DroneCount = 12;
	public const double CheckpointIntervalKm = (double)TrackLengthKm / CheckpointCount;

	// Betting
	public const decimal StartingBalance = 1000m;
	public const decimal MinBetFlat = 10m;
	public const decimal MaxBetPercent = 0.50m;
	public const decimal ReturnsMultiplier = 0.40m;
	public const decimal TargetBookPercentage = 1.05m;

	// Stats
	public const int StatMin = 1;
	public const int StatMax = 10;

	// Incidents
	public const int StallMinTicks = 2;
	public const int StallMaxTicks = 4;
	public const double SpeedPenaltyMultiplier = 0.5;
	public const double IncidentChanceScale = 0.04;

	// Movement weights
	public const double SpeedWeight = 0.40;
	public const double AccelerationWeight = 0.25;
	public const double AgilityWeight = 0.15;
	public const double MovementScaleFactor = 18.0;

	// full influence below this track progress, fades to zero at 1.0
	public const double AccelerationTaperStart = 0.30;
	public const double AccelerationTaperFloor = 0.0;
	public const double AccelerationTaperCeiling = 1.0;

	// Random noise band per tick
	public const double RandomNoiseMin = 0.85;
	public const double RandomNoiseMax = 1.15;

	// agility 10 tightens the band by this much on each side
	public const double AgilityNoiseReduction = 0.10;
}
