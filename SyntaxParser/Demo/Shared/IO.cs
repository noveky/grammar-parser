namespace SyntaxParser.Demo.Shared
{
	internal static class IO
	{
		public const string colon = ": ";

		static readonly object ioLock = new();

		public static void Cls()
		{
			lock (ioLock)
			{
				Console.Clear();
				Console.WriteLine("\x1b[3J");
				Console.Clear();
			}
		}

		public static void Print(object? obj = null, ConsoleColor? color = null)
		{
			lock (ioLock)
			{
				if (color is not null) Console.ForegroundColor = (ConsoleColor)color;
				Console.Write(obj);
				Console.ResetColor();
			}
		}
		public static void PrintLn(object? obj = null, ConsoleColor? color = null) =>
			Print($"{obj}\n", color);

		public static void LogWarning(string message) =>
			PrintLn(message, ConsoleColor.DarkYellow);

		public static void LogWarning(string? prompt, string message) =>
			LogWarning($"{prompt}{colon}{message}");

		public static void LogError(string message) =>
			PrintLn(message, ConsoleColor.DarkRed);

		public static void LogError(Exception ex) =>
			LogError(ex.Message);

		public static void LogError(string? prompt, string message) =>
			LogError($"{prompt}{colon}{message}");

		public static void LogError(string? prompt, Exception ex) =>
			LogError(prompt, ex.Message);

		public static T? Input<T>(string? prompt = null, ConsoleColor? promptColor = null, ConsoleColor? inputColor = null, bool isSecure = false)
		{
			lock (ioLock)
			{
				while (true)
				{
					try
					{
						Print($"{prompt}{colon}", promptColor);

						Console.ForegroundColor = inputColor ?? ConsoleColor.White;
						string? inputStr;
						if (isSecure)
						{
							inputStr = string.Empty;
							ConsoleKey key;
							do
							{
								var keyInfo = Console.ReadKey(intercept: true);
								if (Context.GoingBack)
								{
									PrintLn();
									return default;
								}
								key = keyInfo.Key;
								if (key == ConsoleKey.Backspace && inputStr.Length > 0)
								{
									Print("\b \b", inputColor);
									inputStr = inputStr[0..^1];
								}
								else if (!char.IsControl(keyInfo.KeyChar))
								{
									Print("*", inputColor);
									inputStr += keyInfo.KeyChar;
								}
							} while (key != ConsoleKey.Enter);
							PrintLn();
						}
						else
						{
							var line = Console.ReadLine();
							if (Context.GoingBack)
							{
								PrintLn();
								return default;
							}
							inputStr = line;
						}
						try { return inputStr is null ? default : (T?)Convert.ChangeType(inputStr, typeof(T)); }
						catch { throw; }
						finally { Console.ResetColor(); }
					}
					catch (Exception ex) { LogError(ex); }
				}
			}
		}

		public static T? SecureInput<T>(string? prompt = null, ConsoleColor? promptColor = null, ConsoleColor? inputColor = null) =>
			Input<T>(prompt, promptColor, inputColor, true);

		public static bool AskYesNo(string? prompt = null)
		{
			if (prompt is not null)
			{
				prompt += "? ";
			}
			string? input = Input<string>($"{prompt}[Y/n]");
			return input is "Y";
		}

		public static T? InputField<T>(string fieldName, ConsoleColor? fieldNameColor = null, ConsoleColor? inputColor = null, bool isSecure = false)
		{
			PrintLn($"{fieldName}{colon}", fieldNameColor);
			return Input<T>("| ", ConsoleColor.DarkGray, inputColor ?? ConsoleColor.White, isSecure);
		}

		public static T? SecureField<T>(string fieldName, ConsoleColor? fieldNameColor = null) =>
			InputField<T>(fieldName, fieldNameColor, null, true);

		public static void List(IEnumerable<object> objects, ConsoleColor? color = null) =>
			objects.ToList().ForEach(obj => PrintLn(obj, color));

		public static void Pause(string? prompt = null)
		{
			Print(prompt ?? "请按任意键继续. . . ");
			Console.ReadKey();
		}

		public static void PrintDivider(int length = 32) =>
			PrintLn(new string('-', length));

		public static bool Try(Action action, Action? after = null)
		{
			try
			{
				action();
				return true;
			}
			catch (Exception ex)
			{
				LogError(ex);
				return false;
			}
			finally { (after ?? (() => PrintLn())).Invoke(); }
		}
	}
}
