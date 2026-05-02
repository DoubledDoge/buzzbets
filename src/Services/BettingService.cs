namespace BuzzBets.Services;

using Constants;
using Models;
using Models.Enums;

internal sealed class BettingService
{
	public decimal Balance { get; private set; } = GameConstants.StartingBalance;
	public Drone? SelectedDrone { get; private set; }
	public decimal BetAmount { get; private set; }
	public BetType BetType { get; private set; }
	public bool HasActiveBet => SelectedDrone != null;

	public Drone? LastSelectedDrone { get; private set; }
	public decimal LastBetAmount { get; private set; }
	public BetType LastBetType { get; private set; }
	public decimal LastPayout { get; private set; }

	public bool PlaceBet(Drone drone, decimal amount, BetType betType)
	{
		if (!IsValidBet(amount))
		{
			return false;
		}

		SelectedDrone = drone;
		BetAmount = amount;
		BetType = betType;
		return true;
	}

	public void Settle(RaceResult result, Dictionary<Drone, decimal> payoutRates)
	{
		if (!HasActiveBet)
		{
			return;
		}

		decimal payout = CalculatePayout(result, payoutRates);

		LastSelectedDrone = SelectedDrone;
		LastBetAmount = BetAmount;
		LastBetType = BetType;
		LastPayout = payout;

		Balance += payout - BetAmount;
		ClearBet();
	}

	private bool IsValidBet(decimal amount)
	{
		decimal maxBet = Math.Floor(Balance * GameConstants.MaxBetPercent);
		return amount >= GameConstants.MinBetFlat && amount <= maxBet;
	}

	public decimal MaxBet => Math.Floor(Balance * GameConstants.MaxBetPercent);

	private decimal CalculatePayout(RaceResult result, Dictionary<Drone, decimal> payoutRates)
	{
		if (SelectedDrone == null)
		{
			return 0m;
		}

		decimal droneRates = payoutRates[SelectedDrone];

		return BetType switch
		{
			BetType.WinOnly => result.Podium.FirstOrDefault() == SelectedDrone
				? BetAmount * droneRates
				: 0m,

			BetType.PlaceBet => result.Podium.Contains(SelectedDrone)
				? BetAmount * droneRates * GameConstants.ReturnsMultiplier
				: 0m,

			_ => throw new InvalidOperationException(
				$"Unhandled {nameof(BetType)} value: {BetType}"
			),
		};
	}

	private void ClearBet()
	{
		SelectedDrone = null;
		BetAmount = 0m;
		BetType = default;
	}
}
