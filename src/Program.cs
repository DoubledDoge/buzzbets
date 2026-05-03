using ConsolePrism.Components;
using ConsolePrism.Core;
using ConsolePrism.Layout;
using ConsolePrism.Themes;

namespace BuzzBets;

using Data;
using Models;
using Services;
using UI;

internal static class Program
{
	internal static readonly string CurrencySymbol = CultureInfo
		.CurrentCulture
		.NumberFormat
		.CurrencySymbol;

	private static async Task Main()
	{
		Console.Title = "BuzzBets Drone Racing";

		AppTheme.Initialize();

		ConsoleHelper.SetEncoding(Encoding.UTF8);
		string path = Path.Join(AppContext.BaseDirectory, "Data", "drones.json");
		List<Drone> drones = DroneRoster.LoadFromJson(path);
		Dictionary<Drone, decimal> payoutRate = ReturnsCalculator.Calculate(drones);
		BettingService bettingService = new();

		ShowGreeterScreen();

		bool isPlaying = true;

		while (isPlaying)
		{
			BettingScreen bettingScreen = new(drones, payoutRate, bettingService);
			bool startRace = bettingScreen.Show();

			if (!startRace)
			{
				break;
			}

			RaceEngine engine = new(drones);
			using DisplayService displayService = new(engine, drones, bettingService, payoutRate);
			RaceScreen raceScreen = new(engine, displayService, bettingService);

			RaceResult result = await raceScreen.Show().ConfigureAwait(false);
			bettingService.Settle(result, payoutRate);

			PodiumScreen podiumScreen = new(result, bettingService, payoutRate);
			isPlaying = podiumScreen.Show();

			foreach (Drone drone in drones)
			{
				drone.Reset();
			}
		}

		ShowFarewellScreen();
	}

	private static void ShowGreeterScreen()
	{
		Console.Clear();

		string title = string.Join(
			Environment.NewLine,
			"██████╗ ██╗   ██╗███████╗███████╗██████╗ ███████╗████████╗███████╗",
			"██╔══██╗██║   ██║╚══███╔╝╚══███╔╝██╔══██╗██╔════╝╚══██╔══╝██╔════╝",
			"██████╔╝██║   ██║  ███╔╝   ███╔╝ ██████╔╝█████╗     ██║   ███████╗",
			"██╔══██╗██║   ██║ ███╔╝   ███╔╝  ██╔══██╗██╔══╝     ██║   ╚════██║",
			"██████╔╝╚██████╔╝███████╗███████╗██████╔╝███████╗   ██║   ███████║",
			"╚═════╝  ╚═════╝ ╚══════╝╚══════╝╚═════╝ ╚══════╝   ╚═╝   ╚══════╝"
		);

		const string subtitle = "Welcome to BuzzBets Drone Racing!";

		new Row(spacing: 1)
			.Add(new ConsoleText(title, Theme.Current.Colors.Info))
			.Add(new ConsoleText(subtitle))
			.Add(new ConsoleText("Press any key to continue...", Theme.Current.Colors.Muted))
			.Render();

		ConsoleHelper.HideCursor();
		Console.ReadKey(intercept: true);
		Console.Clear();
	}

	private static void ShowFarewellScreen()
	{
		Console.Clear();

		string message = string.Join(
			Environment.NewLine,
			"Thanks for playing BuzzBets Drone Racing!",
			"May your betting odds ever be in your favour."
		);

		new Row(spacing: 1)
			.Add(new ConsoleText("See You Next Time", Theme.Current.Colors.Info))
			.Add(new ConsoleText(message))
			.Add(new ConsoleText("Press any key to exit...", Theme.Current.Colors.Muted))
			.Render();

		ConsoleHelper.HideCursor();
		Console.ReadKey(intercept: true);
		Console.Clear();
	}
}
