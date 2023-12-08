using SyntaxParser.Demo.Parsers.Sql;
using SyntaxParser.Demo.Shared;

namespace SyntaxParser.Demo.UI.Pages
{
    public class SqlParserPage : IPage
	{
		public string? Title { get; set; } = "SQL Parser";

		readonly SqlParser sqlParser = new();

		public void Show(object? arg = null)
		{
			IO.PrintLn(this.FullTitle());
			IO.PrintLn();
			while (true)
			{
				try
				{
					string sql = IO.Input<string>("Enter sql", ConsoleColor.DarkGray, ConsoleColor.White) ?? string.Empty;
					var results = sqlParser.Parse(sql).ToArray();
					if (results.Length == 0)
					{
						IO.LogError("Error", "Failed to parse sql");
						continue;
					}
					if (results.Length > 1)
					{
						IO.LogWarning("Warning", "Ambiguous syntax rule");
					}
					IO.PrintLn("Parsed results:", ConsoleColor.DarkGray);
					IO.PrintLn(string.Join(",\n", results));
				}
				finally
				{
					IO.PrintLn();
				}
			}
		}
	}
}
