using GrammarParser.Demo.Parsers.Sql;
using GrammarParser.Demo.Shared;

namespace GrammarParser.Demo.UI.Pages
{
    public class SqlParserPage : IPage
	{
		public string? Title { get; set; } = "SQL Parser";

		readonly SqlParser sqlParser = new();

		public void Show(object? arg = null)
		{
			while (true)
			{
				try
				{
					string sql = IO.Input<string>("Enter sql", ConsoleColor.DarkGray, ConsoleColor.White) ?? string.Empty;
					var results = sqlParser.Parse(sql);
					if (!results.Any())
					{
						IO.LogError("Error", "Failed to parse sql");
						continue;
					}
					if (results.Count() > 1)
					{
						IO.LogWarning("Warning", "Ambiguous grammar rule");
					}
					IO.PrintLn("Parsed results:", ConsoleColor.DarkGray);
					foreach (var result in results)
					{
						IO.PrintLn(result?.ToString());
					}
				}
				finally
				{
					IO.PrintLn();
				}
			}
		}
	}
}
