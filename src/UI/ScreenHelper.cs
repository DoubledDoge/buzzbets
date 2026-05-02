using ConsolePrism.Interfaces;
using ConsolePrism.Layout;

namespace BuzzBets.UI;

using Services;

internal static class ScreenHelper
{
	public static void RenderScreen(
		string screenTitle,
		IRenderable content,
		BettingService bettingService,
		string rightFooterText = ""
	)
	{
		string leftFooter = $"Balance: {Program.CurrencySymbol} {bettingService.Balance:F2}";

		if (bettingService.HasActiveBet)
		{
			leftFooter +=
				$"  |  Active Bet: {Program.CurrencySymbol} {bettingService.BetAmount:F2} on {bettingService.SelectedDrone?.Name}";
		}

		string brandedTitle = $"BUZZBETS // {screenTitle}";

		new AppShell(brandedTitle, content, leftFooter, rightFooterText).Render();
	}
}
