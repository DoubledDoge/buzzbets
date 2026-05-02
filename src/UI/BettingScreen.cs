using ConsolePrism.Components;
using ConsolePrism.Core;
using ConsolePrism.Layout;
using ConsolePrism.Themes;
using TypeGuard.Console;

namespace BuzzBets.UI;

using Constants;
using Models;
using Models.Enums;
using Services;

internal sealed class BettingScreen(
	List<Drone> drones,
	Dictionary<Drone, decimal> payoutRate,
	BettingService bettingService
)
{
	private Table BuildTable()
	{
		string[] headers =
		[
			"#",
			"Name",
			"Speed",
			"Accel",
			"Reliability",
			"Agility",
			"Payout Rate",
			"Archetype",
		];

		TableCell[][] data =
		[
			.. drones.Select(
				(d, i) =>
				{
					decimal rate = payoutRate[d];

					ConsoleColor rateColor =
						rate < 6.0m ? Theme.Current.Colors.Success : Theme.Current.Colors.Warning;

					return new TableCell[]
					{
						(i + 1).ToString(CultureInfo.InvariantCulture),
						d.Name,
						d.Speed.ToString(CultureInfo.InvariantCulture),
						d.Acceleration.ToString(CultureInfo.InvariantCulture),
						d.Reliability.ToString(CultureInfo.InvariantCulture),
						d.Agility.ToString(CultureInfo.InvariantCulture),
						new($"{rate:F2}x", rateColor),
						d.Archetype,
					};
				}
			),
		];

		return new Table(headers, data);
	}

	private Drone SelectDrone()
	{
		ScreenHelper.RenderScreen(
			"Select a Drone",
			BuildTable(),
			bettingService,
			$"Please select a number between 1 and {drones.Count}."
		);

		int droneNum = Guard
			.Int("Enter drone number")
			.WithRange(1, drones.Count, $"Please enter a number between 1 and {drones.Count}.")
			.Get();

		return drones[droneNum - 1];
	}

	private BetType SelectBetType()
	{
		Row infoText = new Row(spacing: 1)
			.Add(
				new ConsoleText(
					"Win Only  - Your drone must finish in 1st place.",
					Theme.Current.Colors.Info
				)
			)
			.Add(
				new ConsoleText(
					"Place Bet - Your drone must finish in the top 3. (Reduced payout)",
					Theme.Current.Colors.Info
				)
			);

		ScreenHelper.RenderScreen(
			"Select Bet Type",
			infoText,
			bettingService,
			"Please select either 'Win Only' or 'Place Bet'."
		);

		string[] options = ["Win Only", "Place Bet"];
		int choice = new Menu(
			new string('-', Console.WindowWidth),
			MenuStyle.Interactive,
			string.Empty,
			options
		).Interact();

		return choice == 0 ? BetType.WinOnly : BetType.PlaceBet;
	}

	private void ShowCurrentBet()
	{
		if (!bettingService.HasActiveBet)
		{
			ScreenHelper.RenderScreen(
				"Current Bet",
				new Notification("No active bet placed yet.", true, NotificationLevel.Warning),
				bettingService,
				"Press any key to go back..."
			);

			ConsoleHelper.HideCursor();
			Console.ReadKey(intercept: true);
			return;
		}

		Drone selected = bettingService.SelectedDrone!;
		decimal potentialPayout =
			bettingService.BetType == BetType.WinOnly
				? bettingService.BetAmount * payoutRate[selected]
				: bettingService.BetAmount * payoutRate[selected] * GameConstants.ReturnsMultiplier;

		Row details = new Row(spacing: 1)
			.Add(
				new ConsoleText(
					$"Drone: {selected.Name}  ({selected.Archetype})",
					Theme.Current.Colors.Highlight
				)
			)
			.Add(
				new ConsoleText(
					$"Stake: {Program.CurrencySymbol} {bettingService.BetAmount:F2}",
					Theme.Current.Colors.Info
				)
			)
			.Add(new ConsoleText($"Bet Type: {bettingService.BetType}", Theme.Current.Colors.Info))
			.Add(
				new ConsoleText(
					$"Payout Rate: {payoutRate[selected]:F2}x",
					Theme.Current.Colors.Info
				)
			)
			.Add(
				new ConsoleText(
					$"Est. Payout: {Program.CurrencySymbol} {potentialPayout:F2}",
					Theme.Current.Colors.Success
				)
			);

		ScreenHelper.RenderScreen(
			"Current Bet",
			details,
			bettingService,
			"Press any key to go back..."
		);
		ConsoleHelper.HideCursor();
		Console.ReadKey(intercept: true);
	}

	public bool Show()
	{
		while (true)
		{
			string[] options = ["Place a Bet", "View Current Bet", "Start Race", "Exit"];
			Menu mainMenu = new(string.Empty, MenuStyle.Interactive, string.Empty, options);

			ScreenHelper.RenderScreen("Main Menu", mainMenu, bettingService);

			int choice = mainMenu.Interact();

			switch (choice)
			{
				case 0: // Place Bet
					Drone drone = SelectDrone();

					Row betDetails = new Row(spacing: 1)
						.Add(
							new ConsoleText($"Drone: {drone.Name}", Theme.Current.Colors.Highlight)
						)
						.Add(
							new ConsoleText(
								$"Archetype: {drone.Archetype}",
								Theme.Current.Colors.Info
							)
						)
						.Add(
							new ConsoleText(
								$"Payout Rate: {payoutRate[drone]:F2}x  ({Program.CurrencySymbol} 100 bet returns {Program.CurrencySymbol} {payoutRate[drone] * 100:F2})",
								Theme.Current.Colors.Info
							)
						);

					ScreenHelper.RenderScreen(
						"Place a Bet",
						betDetails,
						bettingService,
						$"Select a stake amount between {Program.CurrencySymbol} {GameConstants.MinBetFlat:F2} and {Program.CurrencySymbol} {bettingService.MaxBet:F2}."
					);

					decimal stake = Guard
						.Decimal($"Enter your stake amount ({Program.CurrencySymbol})")
						.WithRange(
							GameConstants.MinBetFlat,
							bettingService.MaxBet,
							$"Stake must be between {Program.CurrencySymbol} {GameConstants.MinBetFlat:F2} and {Program.CurrencySymbol} {bettingService.MaxBet:F2}."
						)
						.Get();

					BetType betType = SelectBetType();
					bool betPlaced = bettingService.PlaceBet(drone, stake, betType);

					if (betPlaced)
					{
						ScreenHelper.RenderScreen(
							"Bet Confirmed",
							new Notification(
								"Bet placed successfully!",
								true,
								NotificationLevel.Success
							),
							bettingService,
							"Press any key to continue..."
						);

						ConsoleHelper.HideCursor();
						Console.ReadKey(intercept: true);
					}
					break;

				case 1:
					ShowCurrentBet();
					break;

				case 2:
					if (bettingService.HasActiveBet)
					{
						return true;
					}

					ScreenHelper.RenderScreen(
						"No Active Bet",
						new Notification(
							"You must place a bet before starting the race!",
							true,
							NotificationLevel.Warning
						),
						bettingService,
						"Press any key to continue..."
					);

					ConsoleHelper.HideCursor();
					Console.ReadKey(intercept: true);
					break;

				case 3: // Exit
				case -1: // User pressed ESC
					Console.Clear();
					using (Spinner spinner = new(Spinner.Dots, "Exiting..."))
					{
						spinner.Start();
						Thread.Sleep(3000);
						spinner.Stop();
					}
					return false;
			}
		}
	}
}
