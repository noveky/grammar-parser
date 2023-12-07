using GrammarParser.Demo.Shared;
using GrammarParser.Demo.UI.Pages;

namespace GrammarParser.Demo
{
	public class GrammarParserDemo
	{
		public static void Run()
		{
			Context.AppName = "Grammar Parser Demo";
			GrammarParser.LogDebug = true;

			Page.Show<SqlParserPage>();
		}
	}
}
