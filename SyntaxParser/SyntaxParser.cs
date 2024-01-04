using SyntaxParser.Shared;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SyntaxParser
{
	public class Parser
	{
		public static bool LogDebug { get; set; } = false;

		public ISyntaxNode? RootSyntaxNode { get; set; }
		public bool IgnoreCase { get; set; } = false;
		public string? SkipPattern { get; set; }

		public RegexOptions RegexOptions => IgnoreCase? RegexOptions.IgnoreCase : RegexOptions.None;

		public static TokenNode<T> NewToken<T>(string? name, string regex, Func<Token, object?>? builder = null)
		{
			TokenType tokenType = new(name, regex);
			return new TokenNode<T>(name, tokenType, builder);
		}
		public static ITokenNode NewToken(string? name, string regex, Func<Token, object?>? builder = null) =>
			NewToken<object?>(name, regex, builder);

		public IEnumerable<object?> Parse(string str)
		{
			if (RootSyntaxNode is null) throw new InvalidOperationException();

			var stream = new InputStream(this, str);
			Debug.WriteLineIf(LogDebug, "Start to parse input string", "Parser");
			foreach (var result in RootSyntaxNode.Parse(stream))
			{
				if (!stream.AtEnd)
				{
					Debug.WriteLineIf(LogDebug, $"Discard result `{result}` for unparsed portion: \"{stream.Rest}\"", "Parser");
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
		public string Rest => AtEnd ? string.Empty : this[Index..];
		public Token? NextToken(TokenType? type)
		{
			if (type is null || AtEnd) return null;

			if (Parser.SkipPattern is not null)
			{
				Index += Regex.Match(Rest, $"^{Parser.SkipPattern}", Parser.RegexOptions).Length;
			}

			var match = Regex.Match(Rest, $"^{type.RegexPattern}", Parser.RegexOptions);
			if (!match.Success)
			{
				return null;
			}
			foreach (var coverType in type.CoverTypes)
			{
				if (Regex.IsMatch(match.Value, coverType.RegexPattern, Parser.RegexOptions))
				{
					return null;
				}
			}

			Index += match.Length;

			return new Token(type, match.Value);
		}
	}

	public class Name
	{
		readonly Type? type;
		public string? Value { get; set; }
		public override string? ToString() => Value ?? type?.Name;
		public Name(Type type, string? name = null)
		{
			this.type = type;
			Value = name;
		}
	}

	public interface INameable
	{
		public Name Name { get; set; }
	}

	public class TokenType : INameable
	{
		public List<TokenType> CoverTypes { get; } = new();
		public Name Name { get; set; }
		public override string? ToString() => $"{Name}(\"{RegexPattern}\")";
		public string RegexPattern { get; }
		public TokenType(string? name, string regex)
		{
			Name = new(GetType(), name);
			RegexPattern = regex;
		}
		public void CoverBy(IEnumerable<TokenType> types) => CoverTypes.AddRange(types);
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

	public static class Syntax
	{
		public static EmptyNode Empty(string? name = "ε") => new(name);

		public static SequenceNode<T> Seq<T>(string? name = null, params ISyntaxNode[] children) =>
			new(name, children);
		public static SequenceNode<T> Seq<T>(params ISyntaxNode[] children) => Seq<T>(null, children);

		public static MultipleNode<T> Multi<T>(string? name = null, params ISyntaxNode[] branches) =>
			new(name, branches);
		public static MultipleNode<T> Multi<T>(params ISyntaxNode[] branches) => Multi<T>(null, branches);

		public static class Sugar
		{
			public static ISyntaxNode<IEnumerable<T>> List<T>(string? name, ISyntaxNode<T> element, ISyntaxNode? separator = null)
			{
				var restElements = Multi<IEnumerable<T>>();
				{
					_ = restElements.AddBranch(Empty());
				}
				{
					var __ = restElements.AddBranch(separator is null ? Seq<IEnumerable<T>>(element, restElements) : Seq<IEnumerable<T>>(separator, element, restElements));
					__.Builder = a => a.At(element).PrependTo(a.At(restElements));
				}

				var elements = Multi<IEnumerable<T>>(name);
				{
					var __ = elements.NewSeqBranch(element, restElements);
					__.Builder = a => a.At(element).PrependTo(a.At(restElements));
				}

				return elements;
			}

			public static ISyntaxNode<IEnumerable<T>> ListOf<T>(ISyntaxNode<T> element, ISyntaxNode? separator = null) => List(null, element, separator);
		}
	}

	public interface ISyntaxNode : INameable
	{
		public IEnumerable<object?> Parse(InputStream stream);
	}
	public interface ISyntaxNode<T> : ISyntaxNode { }

	public class EmptyNode : ISyntaxNode<object?>
	{
		public Name Name { get; set; }
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

	public interface ITokenNode : ISyntaxNode
	{
		public TokenType TokenType { get; set; }
		public Func<Token, object?>? Builder { get; set; }

		public void CoverBy(params ITokenNode[] nodes);
	}
	public class TokenNode<T> : ITokenNode, ISyntaxNode<T>
	{
		public Name Name { get; set; }
		public override string? ToString() => $"{Name}";
		public TokenType TokenType { get; set; }
		public Func<Token, object?>? Builder { get; set; }
		public TokenNode(string? name, TokenType tokenType, Func<Token, object?>? builder = null)
		{
			Name = new(GetType(), name);
			TokenType = tokenType;
			Builder = builder;
		}
		public TokenNode(TokenType tokenType, Func<Token, object?>? builder = null) : this(null, tokenType, builder) { }

		public void CoverBy(params ITokenNode[] nodes) => TokenType.CoverBy(nodes.Select(node => node.TokenType));

		public IEnumerable<object?> Parse(InputStream stream)
		{
			var token = stream.NextToken(TokenType);
			if (token is null)
			{
				Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields nothing");
				yield break;
			}
			var result = Builder?.Invoke(token);
			Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{result}`");
			yield return result;
		}
	}

	public interface ISequenceNode : ISyntaxNode
	{
		public class Results
		{
			readonly ISequenceNode seqNode;
			readonly object?[] objects;
			public Results(ISequenceNode seqNode, object?[] objects)
			{
				this.seqNode = seqNode;
				this.objects = objects;
			}

			public object? this[int index] => objects[index];
			public TNode? At<TNode>(ISyntaxNode<TNode> node, int order = 1) =>
				objects[seqNode.Children.Select((n, i) => new { n, i }).Where(x => x.n == node).ElementAt(order - 1).i].As<TNode>();
		}

		public List<ISyntaxNode> Children { get; set; }
		public Func<Results, object?>? Builder { get; set; }

		public void SetChildren(params ISyntaxNode[] children);
	}
	public class SequenceNode<T> : ISequenceNode, ISyntaxNode<T>
	{

		public Name Name { get; set; }
		public override string? ToString() => $"{Name}({string.Join(", ", Children.Select(ch => ch.Name))})";
		public List<ISyntaxNode> Children { get; set; } = new();
		public Func<ISequenceNode.Results, object?>? Builder { get; set; }
		public SequenceNode(string? name = null) => Name = new(GetType(), name);
		public SequenceNode(string? name = null, params ISyntaxNode[] branches) : this(name) => SetChildren(branches);
		public SequenceNode() : this(null) { }

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
				yield return Builder?.Invoke(new ISequenceNode.Results(this, childrenResults.ToArray()));
			}
		}
	}
	
	public interface IMultipleNode : ISyntaxNode
	{
		public List<ISyntaxNode> Branches { get; set; }
		public Func<object?, object?>? Converter { get; set; }

		public TBranch AddBranch<TBranch>(TBranch branch) where TBranch : ISyntaxNode;
		public IMultipleNode NewMultiBranch(string? name = null, params ISyntaxNode[] branches);
		public IMultipleNode NewMultiBranch(params ISyntaxNode[] branches);
		public ISequenceNode NewSeqBranch(string? name = null, params ISyntaxNode[] children);
		public ISequenceNode NewSeqBranch(params ISyntaxNode[] children);
		public void SetBranches(params ISyntaxNode[] branches);
	}
	public class MultipleNode<T> : IMultipleNode, ISyntaxNode<T>
	{
		public Name Name { get; set; }
		public override string? ToString() => $"{Name}[{string.Join(" | ", Branches.Select(br => br.Name))}]";
		public List<ISyntaxNode> Branches { get; set; } = new();
		public Func<object?, object?>? Converter { get; set; } = null;
		public MultipleNode(string? name = null) => Name = new(GetType(), name);
		public MultipleNode(string? name = null, params ISyntaxNode[] branches) : this(name) => SetBranches(branches);
		public MultipleNode() : this(null) { }

		string? RenameBranch(ISyntaxNode branch, int index) =>
			branch.Name.Value ??= $"{Name}_{branch.Name.Value ?? index.ToString()}";
		public TBranch AddBranch<TBranch>(TBranch branch) where TBranch : ISyntaxNode
		{
			RenameBranch(branch, Branches.Count);
			Branches.Add(branch);
			return branch;
		}
		public IMultipleNode NewMultiBranch(string? name = null, params ISyntaxNode[] branches) =>
			AddBranch(new MultipleNode<object?>(name, branches));
		public IMultipleNode NewMultiBranch(params ISyntaxNode[] branches) => NewMultiBranch(null, branches);
		public ISequenceNode NewSeqBranch(string? name = null, params ISyntaxNode[] children) =>
			AddBranch(new SequenceNode<object?>(name, children));
		public ISequenceNode NewSeqBranch(params ISyntaxNode[] children) => NewSeqBranch(null, children);
		public void SetBranches(params ISyntaxNode[] branches)
		{
			Branches = branches.ToList();
			_ = Branches.Select(RenameBranch);
		}

		public IEnumerable<object?> Parse(InputStream stream)
		{
			var checkpoint = stream.Index;
			foreach (var br in Branches)
			{
				stream.Index = checkpoint;
				Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` calls `{br}.Parse`");
				foreach (var result in br.Parse(stream))
				{
					Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{result}`");
					yield return Converter is null ? result : Converter.Invoke(result);
				}
			}
		}
	}
}
