using SyntaxParser.Demo;
using SyntaxParser.Demo.Shared;
using SyntaxParser.Demo.UI.Pages;

namespace SyntaxParser
{
	public class Program
	{
		static void Main(string[] args)
		{
			Parser.DebugMode = true;

			SyntaxParserDemo.Run();
		}
	}
}
