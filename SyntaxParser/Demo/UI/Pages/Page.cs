using SyntaxParser.Demo.Shared;

namespace SyntaxParser.Demo.UI.Pages
{
	public interface IPage
	{
		public string? Title { get; set; }
		public abstract void Show(object? arg = null);
	}

	public static class Page
	{
		public static string FullTitle(this IPage page) => $"{Context.AppName} - {page.Title}";
		public static void Show<TPage>(object? arg = null) where TPage : IPage, new()
		{
			IPage page = new TPage();
			while (!Context.GoingBack)
			{
				Console.Title = page.FullTitle();
				IO.Cls();
				page.Show(arg);
			}
			Context.HandleBack();
		}

		public static void Show<TPage>() where TPage : IPage, new() => Show<TPage>(null);
	}
}
