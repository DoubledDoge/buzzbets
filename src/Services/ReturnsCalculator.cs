namespace BuzzBets.Services;

using Constants;
using Models;

internal static class ReturnsCalculator
{
	public static Dictionary<Drone, decimal> Calculate(List<Drone> drones)
	{
		Dictionary<Drone, decimal> composites = drones.ToDictionary(d => d, CompositeScore);
		decimal totalComposite = composites.Values.Sum();

		return drones.ToDictionary(
			d => d,
			d =>
			{
				decimal trueReturn = totalComposite / composites[d];
				decimal houseReturn = trueReturn / GameConstants.TargetBookPercentage;
				return Math.Round(houseReturn, 2);
			}
		);
	}

	private static decimal CompositeScore(Drone drone) =>
		drone.Speed * 0.40m
		+ drone.Acceleration * 0.25m
		+ drone.Reliability * 0.20m
		+ drone.Agility * 0.15m;
}
