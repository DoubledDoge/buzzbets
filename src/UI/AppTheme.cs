using ConsolePrism.Themes;

namespace BuzzBets.UI;

internal static class AppTheme
{
	public static void Initialize()
	{
		Theme buzzBetsTheme = new()
		{
			Colors = new ColorScheme
			{
				Primary = ConsoleColor.Gray,
				Muted = ConsoleColor.DarkGray,
				MenuBorder = ConsoleColor.DarkGray,
				TableBorder = ConsoleColor.DarkGray,
				ProgressBarIncomplete = ConsoleColor.DarkGray,
				TableData = ConsoleColor.Gray,
				MenuOption = ConsoleColor.Gray,

				Highlight = ConsoleColor.Cyan,
				MenuSelected = ConsoleColor.Cyan,
				MenuTitle = ConsoleColor.Cyan,
				TableHeader = ConsoleColor.Cyan,

				Success = ConsoleColor.Green,
				Error = ConsoleColor.Red,
				Warning = ConsoleColor.Yellow,
				Info = ConsoleColor.White,

				ProgressBarComplete = ConsoleColor.White,
				ProgressBarText = ConsoleColor.Gray,
			},
			Border = BorderStyle.Rounded,
		};

		Theme.Apply(buzzBetsTheme);
	}
}
