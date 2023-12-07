namespace GrammarParser.Demo.Shared
{
	internal static class Context
	{
		public static string? AppName { get; set; }

		public static bool GoingBack { get; private set; } = false;
		public static void Back() => GoingBack = true;
		public static void HandleBack() => GoingBack = false;
	}
}
