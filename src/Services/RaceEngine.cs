namespace BuzzBets.Services;

using Constants;
using Models;
using Models.Enums;

internal sealed class RaceEngine(List<Drone> drones)
{
	private readonly List<Incident> _incidents = [];
	private readonly List<Drone> _finishOrder = [];
	private readonly Lock _finishLock = new();

	public event Action<Drone, int>? OnCheckpointPassed;
	public event Action<Drone, Incident>? OnIncidentOccurred;
	public event Action<Drone, int>? OnDroneFinished;

	public async Task<RaceResult> StartRaceAsync()
	{
		IEnumerable<Task> tasks = drones.Select(RunDroneAsync);
		await Task.WhenAll(tasks).ConfigureAwait(false);
		return BuildResult();
	}

	private async Task RunDroneAsync(Drone drone)
	{
		drone.Status = DroneStatus.Racing;

		while (!drone.HasFinished)
		{
			await Task.Delay(GameConstants.TickIntervalMs).ConfigureAwait(false);
			ProcessTick(drone);
		}
	}

	private void ProcessTick(Drone drone)
	{
		switch (drone.Status)
		{
			case DroneStatus.DNF:
				return;
			case DroneStatus.Stalled:
			{
				drone.StallTicksRemaining--;

				if (drone.StallTicksRemaining <= 0)
				{
					drone.Status = DroneStatus.Racing;
				}

				return;
			}
			case DroneStatus.Waiting:
			case DroneStatus.Racing:
			case DroneStatus.Finished:
				break;
			default:
				throw new InvalidOperationException(
					$"Unhandled {nameof(DroneStatus)} value: {drone.Status}"
				);
		}

		Incident? incident = IncidentResolver.TryResolve(drone);

		if (incident != null)
		{
			lock (_finishLock)
			{
				_incidents.Add(incident);
			}

			OnIncidentOccurred?.Invoke(drone, incident);

			if (drone.Status == DroneStatus.DNF)
			{
				return;
			}
		}

		drone.DistanceCovered += CalculateDelta(drone);
		CheckCheckpoints(drone);
		CheckFinish(drone);
	}

	private static double CalculateDelta(Drone drone)
	{
		Random random = Random.Shared;
		double trackProgress = drone.DistanceCovered / GameConstants.TrackLengthKm;

		// Acceleration tapers off as the race progresses
		double accelerationInfluence = Math.Max(
			GameConstants.AccelerationTaperFloor,
			GameConstants.AccelerationTaperCeiling
				- trackProgress / GameConstants.AccelerationTaperStart
		);

		double composite =
			drone.Speed * GameConstants.SpeedWeight
			+ drone.Acceleration * GameConstants.AccelerationWeight * accelerationInfluence
			+ drone.Agility * GameConstants.AgilityWeight;

		double baseDelta = composite / GameConstants.MovementScaleFactor;

		// Agility tightens the random noise band
		double noiseReduction =
			drone.Agility / (double)GameConstants.StatMax * GameConstants.AgilityNoiseReduction;

		double noiseMin = GameConstants.RandomNoiseMin + noiseReduction;
		double noiseMax = GameConstants.RandomNoiseMax - noiseReduction;

		double noise = random.NextDouble() * (noiseMax - noiseMin) + noiseMin;

		return baseDelta * noise * drone.SpeedMultiplier;
	}

	private void CheckCheckpoints(Drone drone)
	{
		int checkpointReached = (int)(drone.DistanceCovered / GameConstants.CheckpointIntervalKm);
		checkpointReached = Math.Min(checkpointReached, GameConstants.CheckpointCount);

		if (checkpointReached <= drone.LastCheckpointReached)
		{
			return;
		}

		drone.LastCheckpointReached = checkpointReached;
		OnCheckpointPassed?.Invoke(drone, checkpointReached);
	}

	private void CheckFinish(Drone drone)
	{
		if (drone.DistanceCovered < GameConstants.TrackLengthKm)
		{
			return;
		}

		drone.DistanceCovered = GameConstants.TrackLengthKm;
		drone.Status = DroneStatus.Finished;
		drone.FinishTime = DateTime.Now;

		lock (_finishLock)
		{
			_finishOrder.Add(drone);
			OnDroneFinished?.Invoke(drone, _finishOrder.Count);
		}
	}

	private RaceResult BuildResult()
	{
		List<Drone> dnfs = [.. drones.Where(d => d.Status == DroneStatus.DNF)];

		List<Drone> fullOrder = [.. _finishOrder, .. dnfs];
		List<Drone> podium = [.. fullOrder.Take(3)];

		return new RaceResult(fullOrder, podium, _incidents);
	}
}
