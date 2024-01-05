using SyntaxParser.Demo;

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
