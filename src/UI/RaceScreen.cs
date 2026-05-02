using ConsolePrism.Components;
using ConsolePrism.Core;
using ConsolePrism.Themes;

namespace BuzzBets.UI;

using Models;
using Services;

internal sealed class RaceScreen(
	RaceEngine engine,
	DisplayService displayService,
	BettingService bettingService
)
{
	public async Task<RaceResult> Show()
	{
		ScreenHelper.RenderScreen(
			"Pre-Flight Checks",
			new ConsoleText(
				"Calibrating telemetry and initializing drone firmware...",
				Theme.Current.Colors.Info
			),
			bettingService,
			"Awaiting launch sequence..."
		);

		ConsoleHelper.WriteEmptyLines(1);
		ConsoleHelper.HideCursor();

		int startTop = Console.CursorTop;
		string[] frames = Spinner.Dots;
		int countdown = 10;

		for (int tick = 0; tick < 100; tick++)
		{
			if (tick > 0 && tick % 10 == 0)
			{
				countdown--;
			}

			Console.SetCursorPosition(0, startTop);

			ColorWriter.WriteColored(frames[tick % frames.Length], Theme.Current.Colors.Primary);

			ColorWriter.WriteColored(
				$"  T-Minus {countdown} seconds to launch...".PadRight(40),
				Theme.Current.Colors.Muted
			);

			await Task.Delay(100).ConfigureAwait(false);
		}

		Console.SetCursorPosition(0, startTop);
		ConsoleHelper.ClearCurrentLine();
		ColorWriter.WriteSuccessLine("All systems go! Launching...");

		await Task.Delay(1000).ConfigureAwait(false);

		ConsoleHelper.HideCursor();
		displayService.RenderRaceScreen();
		ConsoleHelper.ShowCursor();

		RaceResult result = await engine.StartRaceAsync().ConfigureAwait(false);

		await Task.Delay(2000).ConfigureAwait(false);

		return result;
	}
}
