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
					var resultIter = sqlParser.Parse(sql).GetEnumerator();
					if (!resultIter.MoveNext())
					{
						IO.LogError("Error", "Syntax error");
						continue;
					}
					IO.PrintLn("Parsed result:", ConsoleColor.DarkGray);
					IO.PrintLn(resultIter.Current);
					if (resultIter.MoveNext())
					{
						IO.LogWarning("Warning", "Ambiguous statement");
						IO.PrintLn("Other possible results:", ConsoleColor.DarkGray);
						do
						{
							IO.PrintLn(string.Join(",\n", resultIter.Current));
						}
						while (resultIter.MoveNext());
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
