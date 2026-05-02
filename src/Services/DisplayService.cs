using ConsolePrism.Components;
using ConsolePrism.Core;
using ConsolePrism.Layout;
using ConsolePrism.Themes;

namespace BuzzBets.Services;

using Constants;
using Models;
using Models.Enums;
using UI;

internal sealed class DisplayService : IDisposable
{
	private readonly List<Drone> _drones;
	private readonly SemaphoreSlim _consoleLock = new(1, 1);
	private readonly BettingService _bettingService;
	private readonly Dictionary<Drone, decimal> _payoutRates;
	private bool _disposed;
	private bool _layoutDrawn;
	private readonly int _eventLogHeight;

	private readonly List<(string Text, ConsoleColor Color)> _eventLog = [];

	private static readonly Theme PlayerHighlightTheme = new()
	{
		Colors = new ColorScheme
		{
			ProgressBarComplete = ConsoleColor.Cyan,
			ProgressBarText = ConsoleColor.Cyan,
			ProgressBarIncomplete = ConsoleColor.DarkGray,
		},
	};

	public DisplayService(
		RaceEngine engine,
		List<Drone> drones,
		BettingService bettingService,
		Dictionary<Drone, decimal> payoutRates
	)
	{
		_drones = drones;
		_bettingService = bettingService;
		_payoutRates = payoutRates;

		_eventLogHeight = _drones.Count;

		for (int i = 0; i < _eventLogHeight; i++)
		{
			_eventLog.Add((" ", ConsoleColor.White));
		}

		_eventLog.Add(
			(">> RACE START! All systems go, drones have launched!", Theme.Current.Colors.Highlight)
		);

		engine.OnCheckpointPassed += HandleCheckpointPassed;
		engine.OnIncidentOccurred += HandleIncidentOccurred;
		engine.OnDroneFinished += HandleDroneFinished;
	}

	public void RenderRaceScreen()
	{
		const int gap = 2;
		int leaderboardWidth = (Console.WindowWidth - gap) / 2;
		int eventLogWidth = Console.WindowWidth - leaderboardWidth - gap;

		if (!_layoutDrawn)
		{
			ConsoleHelper.HideCursor();

			string blankLeaderboard = string.Join(
				Environment.NewLine,
				Enumerable.Repeat(" ", _drones.Count)
			);
			string blankEventLog = string.Join(
				Environment.NewLine,
				Enumerable.Repeat(" ", _eventLogHeight)
			);

			Row layout = new Row(spacing: 1)
				.Add(
					new Column(gap: gap)
						.Add(
							new Panel(
								"Leaderboard",
								new ConsoleText(blankLeaderboard),
								width: leaderboardWidth
							)
						)
						.Add(
							new Panel(
								"Race Events",
								new ConsoleText(blankEventLog),
								width: eventLogWidth
							)
						)
				)
				.Add(
					new Panel("Live Bet Tracker", new ConsoleText(" "), width: Console.WindowWidth)
				);

			ScreenHelper.RenderScreen("Live Race", layout, _bettingService);

			_layoutDrawn = true;

			DrawBetTrackerDirect(1, _drones.Count + 8, Console.WindowWidth - 2);
		}

		DrawLeaderboardDirect(1, 5, leaderboardWidth - 2);
		DrawEventLogDirect(leaderboardWidth + gap + 1, 5, eventLogWidth - 2);
	}

	private void DrawLeaderboardDirect(int startX, int startY, int innerWidth)
	{
		List<Drone> sorted = [.. _drones.OrderByDescending(d => d.DistanceCovered)];
		int maxNameLength = _drones.Max(d => d.Name.Length);
		int fixedLabelWidth = 4 + maxNameLength + 6;
		int uniformBarWidth = Math.Max(10, innerWidth - fixedLabelWidth - 12);

		for (int i = 0; i < sorted.Count; i++)
		{
			Console.SetCursorPosition(startX, startY + i);

			Drone drone = sorted[i];
			string status = drone.Status switch
			{
				DroneStatus.DNF => " [DNF]",
				DroneStatus.Finished => " [FIN]",
				DroneStatus.Stalled => " [STA]",
				_ => string.Empty,
			};

			string label = $"{i + 1, 2}. {drone.Name}{status}";
			int current = (int)Math.Round(drone.DistanceCovered);

			ProgressBar bar = new(
				current,
				label.PadRight(fixedLabelWidth),
				GameConstants.TrackLengthKm,
				inPlace: true,
				uniformBarWidth
			)
			{
				Theme =
					_bettingService.HasActiveBet && drone == _bettingService.SelectedDrone
						? PlayerHighlightTheme
						: Theme.Current,
			};

			bar.Render();
		}
	}

	private void DrawEventLogDirect(int startX, int startY, int innerWidth)
	{
		List<(string Text, ConsoleColor Color)> visibleLines =
		[
			.. _eventLog.Skip(Math.Max(0, _eventLog.Count - _eventLogHeight)).Take(_eventLogHeight),
		];

		for (int i = 0; i < visibleLines.Count; i++)
		{
			Console.SetCursorPosition(startX, startY + i);
			string text = visibleLines[i].Text;

			text = text.Length > innerWidth ? text[..innerWidth] : text.PadRight(innerWidth);

			ColorWriter.WriteColored(text, visibleLines[i].Color);
		}
	}

	private void DrawBetTrackerDirect(int startX, int startY, int innerWidth)
	{
		Console.SetCursorPosition(startX, startY);

		if (!_bettingService.HasActiveBet)
		{
			ColorWriter.WriteColored(
				ConsoleHelper.PadCenter("No active bet placed.", innerWidth),
				Theme.Current.Colors.Muted
			);
			return;
		}

		Drone selected = _bettingService.SelectedDrone!;
		decimal payoutRate = _payoutRates[selected];

		decimal potentialPayout =
			_bettingService.BetType == BetType.WinOnly
				? _bettingService.BetAmount * payoutRate
				: _bettingService.BetAmount * payoutRate * GameConstants.ReturnsMultiplier;

		string target = _bettingService.BetType == BetType.WinOnly ? "Win Only" : "Top 3";
		string trackerText =
			$" Target: {selected.Name} ({target})  |  Stake: {Program.CurrencySymbol}{_bettingService.BetAmount:F2}  |  Rate: {payoutRate:F2}x  |  Payout: {Program.CurrencySymbol}{potentialPayout:F2} ";

		ColorWriter.WriteHighlight(ConsoleHelper.PadCenter(trackerText, innerWidth));
	}

	private async void HandleCheckpointPassed(Drone drone, int checkpoint)
	{
		await _consoleLock.WaitAsync().ConfigureAwait(false);
		try
		{
			_eventLog.Add(
				(
					$">> {drone.Name} passed checkpoint {checkpoint}/{GameConstants.CheckpointCount}",
					ConsoleColor.White
				)
			);
			RenderRaceScreen();
		}
		finally
		{
			_consoleLock.Release();
		}
	}

	private async void HandleIncidentOccurred(Drone drone, Incident incident)
	{
		await _consoleLock.WaitAsync().ConfigureAwait(false);
		try
		{
			ConsoleColor color = incident.Type switch
			{
				IncidentType.DNF => Theme.Current.Colors.Error,
				IncidentType.SpeedPenalty => Theme.Current.Colors.Warning,
				IncidentType.TemporaryStall => Theme.Current.Colors.Info,
				_ => ConsoleColor.White,
			};

			string message = incident.Type switch
			{
				IncidentType.DNF => $"!! {drone.Name} has been eliminated!",
				IncidentType.SpeedPenalty => $">> {drone.Name} suffered a speed penalty!",
				IncidentType.TemporaryStall => $">> {drone.Name} has stalled!",
				_ => $">> {drone.Name} experienced an incident.",
			};

			_eventLog.Add((message, color));
			RenderRaceScreen();
		}
		finally
		{
			_consoleLock.Release();
		}
	}

	private async void HandleDroneFinished(Drone drone, int position)
	{
		await _consoleLock.WaitAsync().ConfigureAwait(false);
		try
		{
			_eventLog.Add(
				($">> {drone.Name} finished in position {position}!", Theme.Current.Colors.Success)
			);
			RenderRaceScreen();
		}
		finally
		{
			_consoleLock.Release();
		}
	}

	private void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			_consoleLock.Dispose();
		}

		_disposed = true;
	}

	public void Dispose() => Dispose(disposing: true);
}
