using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SyntaxParser
{
	public class Parser
	{
		public static bool LogDebug { get; set; } = false;

		public ISyntaxNode? RootSyntaxNode { get; set; }
		public bool IgnoreCase { get; set; } = false;
		public string? IgnorePattern { get; set; }

		public RegexOptions RegexOptions => IgnoreCase? RegexOptions.IgnoreCase : RegexOptions.None;

		public static TokenNode NewToken(string? name, string regex, Func<Token, object?>? builder = null)
		{
			name = $"{name}Token";
			TokenType tokenType = new(name, regex);
			return new TokenNode(name, tokenType, builder);
		}

		public IEnumerable<object?> Parse(string str)
		{
			if (RootSyntaxNode is null) throw new InvalidOperationException();
			var stream = new InputStream(this, str);
			Debug.WriteLineIf(LogDebug, "Start to parse input string", "Parser");
			foreach (var result in RootSyntaxNode.Parse(stream))
			{
				if (!stream.AtEnd)
				{
					Debug.WriteLineIf(LogDebug, $"Discard result `{result}` for unparsed portion: \"{stream[(stream.Index + 1)..]}\"", "Parser");
					continue;
				}
				Debug.WriteLineIf(LogDebug, $"Accept result `{result}`", "Parser");
				yield return result;
			}
		}
	}

	public class InputStream
	{
		readonly string str;
		public Parser Parser { get; }
		public char this[int index] => str[index];
		public string this[Range range] => str[range];
		public int Index { get; set; } = 0;
		public InputStream(Parser parser, string str)
		{
			Parser = parser;
			this.str = str;
		}
		public bool AtBegin => Index <= 0 &&
			(Index == 0 ? true : throw new IndexOutOfRangeException());
		public bool AtEnd => Index >= str.Length &&
			(Index == str.Length ? true : throw new IndexOutOfRangeException());
		public char? Current => AtEnd ? null : str[Index];
		public string? Rest => AtEnd ? null : this[Index..];
		public Token? NextToken(TokenType? type)
		{
			if (type is null || AtEnd || Rest is null) return null;
			if (Parser.IgnorePattern is not null) Index += Regex.Match(Rest, $"^{Parser.IgnorePattern}", Parser.RegexOptions).Length;
			var match = Regex.Match(Rest, $"^{type.RegexPattern}", Parser.RegexOptions);
			if (!match.Success) return null;
			var token = new Token(type, match.Value);
			Index += match.Length;
			return token;
		}
	}

	public class InstanceName
	{
		readonly Type? type;
		public string? Value { get; set; }
		public override string? ToString() => Value ?? type?.Name;
		public InstanceName(Type type, string? name = null)
		{
			this.type = type;
			Value = name;
		}
	}

	public interface INameable
	{
		public InstanceName Name { get; set; }
	}

	public class TokenType : INameable
	{
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}(\"{RegexPattern}\")";
		public string RegexPattern { get; }
		public TokenType(string? name, string regex)
		{
			Name = new(GetType(), name);
			RegexPattern = regex;
		}
	}

	public class Token
	{
		public override string? ToString() => $"{Type.Name}(\"{Value}\")";
		public TokenType Type { get; set; }
		public string Value { get; set; }
		public Token(TokenType type, string value)
		{
			Type = type;
			Value = value;
		}
	}

	public interface ISyntaxNode : INameable
	{
		public IEnumerable<object?> Parse(InputStream stream);
	}

	public class EmptyNode : ISyntaxNode
	{
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}";
		public Func<object?>? Builder { get; set; }
		public EmptyNode(string? name = null, Func<object?>? builder = null)
		{
			Name = new(GetType(), name);
			Builder = builder;
		}
		public EmptyNode() : this(null) { }
		public IEnumerable<object?> Parse(InputStream stream)
		{
			Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{null}`");
			yield return null;
		}
	}

	public class TokenNode : ISyntaxNode
	{
		readonly List<TokenType> coverTokenTypes = new();
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}";
		public TokenType? TokenType { get; set; }
		public Func<Token, object?>? Builder { get; set; }
		public TokenNode(string? name = null, TokenType? tokenType = null, Func<Token, object?>? builder = null)
		{
			Name = new(GetType(), name);
			TokenType = tokenType;
			Builder = builder;
		}
		public TokenNode() : this(null) { }
		public void CoverBy(params TokenNode[]? nodes)
		{
			nodes?.Where(node => node.TokenType is not null).ToList().ForEach(node => coverTokenTypes.Add(node.TokenType!));
		}
		public IEnumerable<object?> Parse(InputStream stream)
		{
			var token = stream.NextToken(TokenType);
			if (token is null)
			{
				Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields nothing");
				yield break;
			}
			foreach (var type in coverTokenTypes)
			{
				if (Regex.IsMatch(token.Value, type.RegexPattern, stream.Parser.RegexOptions)) yield break;
			}
			var result = Builder?.Invoke(token);
			Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{result}`");
			yield return result;
		}
	}

	public class SequenceNode : ISyntaxNode
	{
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}({string.Join(", ", Children.Select(ch => ch.Name))})";
		public List<ISyntaxNode> Children { get; set; } = new();
		public Func<object?[], object?>? Builder { get; set; }
		public SequenceNode(string? name = null) => Name = new(GetType(), name);
		public SequenceNode() : this(null) { }
		public TChild NewChild<TChild>(TChild child) where TChild : ISyntaxNode
		{
			Children.Add(child);
			return child;
		}
		public void SetChildren(params ISyntaxNode[] children) => Children = children.ToList();
		IEnumerable<IEnumerable<object?>> ParseRecursive(InputStream stream, int childIndex)
		{
			if (childIndex >= Children.Count) yield break;
			var ch = Children[childIndex];
			Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.ParseRecursive` calls `{ch}.Parse` at child {childIndex} `{Children[childIndex]}`");
			foreach (var chResult in ch.Parse(stream))
			{
				int checkpoint = stream.Index;
				var chResultAsArray = new[] { chResult };
				if (childIndex == Children.Count - 1)
				{
					Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.ParseRecursive` yields `{chResult}` at child {childIndex} `{Children[childIndex]}`");
					yield return chResultAsArray;
				}
				else
				{
					stream.Index = checkpoint;
					foreach (var restResults in ParseRecursive(stream, childIndex + 1))
					{
						Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.ParseRecursive` yields `{chResult}` at child {childIndex} `{Children[childIndex]}`");
						yield return chResultAsArray.Concat(restResults);
					}
				}
			}
		}
		public IEnumerable<object?> Parse(InputStream stream)
		{
			foreach (var childrenResults in ParseRecursive(stream, 0))
			{
				Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{childrenResults}`");
				yield return Builder?.Invoke(childrenResults.ToArray());
			}
		}
	}

	public class MultipleNode : ISyntaxNode
	{
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}[{string.Join(" | ", Children.Select(ch => ch.Name))}]";
		public List<ISyntaxNode> Children { get; set; } = new();
		public Func<object?, object?>? Converter { get; set; } = null;
		public MultipleNode(string? name = null) => Name = new(GetType(), name);
		public MultipleNode() : this(null) { }
		string? RenameChild(ISyntaxNode child, int index) =>
			child.Name.Value ??= $"{Name}_{child.Name.Value ?? index.ToString()}";
		public TChild NewChild<TChild>(TChild child) where TChild : ISyntaxNode
		{
			RenameChild(child, Children.Count);
			Children.Add(child);
			return child;
		}
		public TChild NewChild<TChild>(string? name = null) where TChild : ISyntaxNode, new()
		{
			TChild child = new() { Name = new(typeof(TChild), name) };
			return NewChild(child);
		}
		public void SetChildren(params ISyntaxNode[] children)
		{
			Children = children.ToList();
			_ = Children.Select(RenameChild);
		}
		public IEnumerable<object?> Parse(InputStream stream)
		{
			var checkpoint = stream.Index;
			foreach (var ch in Children)
			{
				stream.Index = checkpoint;
				Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` calls `{ch}.Parse`");
				foreach (var result in ch.Parse(stream))
				{
					Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{result}`");
					yield return Converter is null ? result : Converter.Invoke(result);
				}
			}
		}
	}
}
