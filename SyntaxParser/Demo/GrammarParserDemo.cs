using SyntaxParser.Demo.Shared;
using SyntaxParser.Demo.UI.Pages;

namespace SyntaxParser.Demo
{
	public class SyntaxParserDemo
	{
		public static void Run()
		{
			Context.AppName = "Syntax Parser Demo";
			Parser.LogDebug = false;

			Page.Show<SqlParserPage>();
		}
	}
}
