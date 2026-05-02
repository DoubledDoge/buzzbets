using ConsolePrism.Components;
using ConsolePrism.Layout;
using ConsolePrism.Themes;

namespace BuzzBets.UI;

using Models;
using Models.Enums;
using Services;

internal sealed class PodiumScreen(
	RaceResult result,
	BettingService bettingService,
	Dictionary<Drone, decimal> payoutRates
)
{
	private static readonly string[] Medals = ["1ST", "2ND", "3RD"];

	private Panel BuildPodiumPanel()
	{
		Row podiumRows = new(spacing: 0);

		for (int i = 0; i < result.Podium.Count; i++)
		{
			podiumRows.Add(
				new ConsoleText(
					$"  {Medals[i]}  {result.Podium[i].Name}",
					i switch
					{
						0 => Theme.Current.Colors.Warning,
						1 => Theme.Current.Colors.Muted,
						_ => Theme.Current.Colors.Info,
					}
				)
			);
		}

		if (result.Podium.Count == 0)
		{
			podiumRows.Add(
				new ConsoleText("  No drones finished the race.", Theme.Current.Colors.Muted)
			);
		}

		return new Panel("Race Results", podiumRows, horizontalPadding: 2, verticalPadding: 1);
	}

	private Panel BuildResultPanel()
	{
		Drone? selected = bettingService.LastSelectedDrone;

		if (selected == null)
		{
			return new Panel(
				"Your Bet",
				new ConsoleText("No bet was placed.", Theme.Current.Colors.Muted),
				horizontalPadding: 2,
				verticalPadding: 1
			);
		}

		bool won = bettingService.LastBetType switch
		{
			BetType.WinOnly => result.Podium.FirstOrDefault() == selected,
			BetType.PlaceBet => result.Podium.Contains(selected),
			_ => false,
		};

		string finishPosition = result.FinishOrder.Contains(selected)
			? $"{result.FinishOrder.IndexOf(selected) + 1}"
			: "DNF";

		ConsoleColor resultColor = won ? Theme.Current.Colors.Success : Theme.Current.Colors.Error;

		Row details = new(spacing: 0);
		details
			.Add(new ConsoleText($"  Drone      : {selected.Name}"))
			.Add(
				new ConsoleText($"  Archetype  : {selected.Archetype}", Theme.Current.Colors.Muted)
			)
			.Add(new ConsoleText($"  Finished   : {finishPosition}"))
			.Add(new ConsoleText($"  Bet Type   : {bettingService.LastBetType}"))
			.Add(new ConsoleText($"  Payout Rate: {payoutRates[selected]:F2}x"))
			.Add(
				new ConsoleText(
					$"  Stake      : {Program.CurrencySymbol} {bettingService.LastBetAmount:F2}"
				)
			)
			.Add(new ConsoleText($"  Result     : {(won ? "Won" : "Lost")}", resultColor))
			.Add(
				new ConsoleText(
					$"  Payout     : {Program.CurrencySymbol} {bettingService.LastPayout:F2}",
					resultColor
				)
			)
			.Add(
				new ConsoleText(
					$"  Balance    : {Program.CurrencySymbol} {bettingService.Balance:F2}",
					Theme.Current.Colors.Success
				)
			);

		return new Panel("Your Bet", details, horizontalPadding: 2, verticalPadding: 1);
	}

	public bool Show()
	{
		Row layout = new Row(spacing: 1).Add(BuildPodiumPanel()).Add(BuildResultPanel());

		ScreenHelper.RenderScreen("Race Complete", layout, bettingService);

		string[] choices = ["Play Again", "Exit"];
		int choice = new Menu(
			"  What's Next?",
			MenuStyle.Interactive,
			string.Empty,
			choices
		).Interact();

		return choice == 0;
	}
}
