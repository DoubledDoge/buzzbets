namespace BuzzBets.Models;

using Enums;

internal sealed class Drone(
	string name,
	int speed,
	int acceleration,
	int reliability,
	int agility,
	string archetype
)
{
	public string Name { get; } = name;
	public int Speed { get; } = speed;
	public int Acceleration { get; } = acceleration;
	public int Reliability { get; } = reliability;
	public int Agility { get; } = agility;
	public string Archetype { get; } = archetype;

	public double DistanceCovered { get; internal set; }
	public DroneStatus Status { get; internal set; } = DroneStatus.Waiting;
	public DateTime? FinishTime { get; internal set; }
	public int StallTicksRemaining { get; internal set; }
	public double SpeedMultiplier { get; internal set; } = 1.0;
	public int LastCheckpointReached { get; internal set; }

	public bool HasFinished => Status is DroneStatus.Finished or DroneStatus.DNF;

	public void Reset()
	{
		DistanceCovered = 0;
		Status = DroneStatus.Waiting;
		FinishTime = null;
		StallTicksRemaining = 0;
		SpeedMultiplier = 1.0;
		LastCheckpointReached = 0;
	}
}
