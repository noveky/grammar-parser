using SyntaxParser.Shared;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SyntaxParser
{
	public class Parser
	{
		public static bool LogDebug { get; set; } = false;

		readonly List<TokenType> tokenTypes = new();

		public ISyntaxNode? RootSyntaxNode { get; set; }
		public bool IgnoreCase { get; set; } = false;
		public string? SkipPattern { get; set; }

		public RegexOptions RegexOptions => IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

		public TokenNode NewToken(string? name, string regex, Func<Token, object?>? builder = null)
		{
			TokenType tokenType = new(name, regex);
			tokenTypes.Add(tokenType);
			return new TokenNode(name, tokenType, builder);
		}

		IEnumerable<IEnumerable<Token>> TokenizeRecursive(string str)
		{
			if (str.Length == 0)
			{
				yield return Enumerable.Empty<Token>();
				yield break;
			}
			foreach (var type in tokenTypes)
			{
				if (SkipPattern is not null)
				{
					var skipLen = Regex.Match(str, $"^{SkipPattern}", RegexOptions).Length;
					str = str[skipLen..];
				}
				var match = Regex.Match(str, $"^{type.RegexPattern}", RegexOptions);
				if (!match.Success)
				{
					continue;
				}
				var matchLen = match.Length;
				var token = new Token(type, match.Value);
				foreach (var restTokens in TokenizeRecursive(str[matchLen..]))
				{
					yield return token.PrependTo(restTokens);
				}
			}
		}

		public IEnumerable<object?> Parse(string str)
		{
			if (RootSyntaxNode is null) throw new InvalidOperationException();
			var tokenss = TokenizeRecursive(str);
			var streams = tokenss.Select(tokens => new TokenStream(this, tokens));
			Debug.WriteLineIf(LogDebug, "Start to parse input string", "Parser");
			foreach (var stream in streams)
			{
				foreach (var result in RootSyntaxNode.Parse(stream))
				{
					if (!stream.AtEnd)
					{
						Debug.WriteLineIf(LogDebug, $"Discard result `{result}` for {stream.Rest.Count()} unparsed tokens", "Parser");
						continue;
					}
					Debug.WriteLineIf(LogDebug, $"Accept result `{result}`", "Parser");
					yield return result;
				}
			}
		}
	}

	public class TokenStream
	{
		public Token[] Tokens { get; }
		public Parser Parser { get; }
		public Token this[int index] => Tokens[index];
		public IEnumerable<Token> this[Range range] => Tokens[range];
		public int Index { get; set; } = -1;
		public TokenStream(Parser parser, IEnumerable<Token> tokens)
		{
			Parser = parser;
			Tokens = tokens.ToArray();
		}
		public bool AtBegin => Index < 0 &&
			(Index == -1 ? true : throw new IndexOutOfRangeException());
		public bool AtEnd => Index >= Tokens.Length - 1 &&
			(Index == Tokens.Length - 1 ? true : throw new IndexOutOfRangeException());
		public Token? Current => AtBegin ? null : Tokens[Index];
		public IEnumerable<Token> Rest => AtEnd ? Enumerable.Empty<Token>() : this[Index..];
		public Token? NextToken(TokenType? type)
		{
			var token = AtEnd ? null : Tokens[++Index];
			return token?.Type == type ? token : null;
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
		public List<TokenType> CoverTypes { get; } = new();
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}(\"{RegexPattern}\")";
		public string RegexPattern { get; }
		public TokenType(string? name, string regex)
		{
			Name = new(GetType(), name);
			RegexPattern = regex;
		}
		public void CoverBy(params TokenNode[]? nodes)
		{
			nodes?.Where(node => node.TokenType is not null).ToList().ForEach(node => CoverTypes.Add(node.TokenType!));
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

	public static class Syntax
	{
		public static EmptyNode Empty(string? name = null) => new(name);
		public static SequenceNode Seq(string? name = null, params ISyntaxNode[] children) =>
			new SequenceNode(name).WithChildren(children);
		public static SequenceNode Seq(params ISyntaxNode[] children) => Seq(null, children);
		public static MultipleNode Multi(string? name = null, params ISyntaxNode[] branches) =>
			new MultipleNode(name).WithBranches(branches);
		public static MultipleNode Multi(params ISyntaxNode[] branches) => Multi(null, branches);
	}

	public interface ISyntaxNode : INameable
	{
		public IEnumerable<object?> Parse(TokenStream stream);
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
		public IEnumerable<object?> Parse(TokenStream stream)
		{
			Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.Parse` yields `{null}`");
			yield return null;
		}
	}

	public class TokenNode : ISyntaxNode
	{
		public InstanceName Name { get; set; }
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
		public void CoverBy(params TokenNode[]? nodes) => TokenType.CoverBy(nodes);
		public IEnumerable<object?> Parse(TokenStream stream)
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

	public class SequenceNode : ISyntaxNode
	{
		public InstanceName Name { get; set; }
		public override string? ToString() => $"{Name}({string.Join(", ", Children.Select(ch => ch.Name))})";
		public List<ISyntaxNode> Children { get; set; } = new();
		public Func<object?[], object?>? Builder { get; set; }
		public SequenceNode(string? name = null) => Name = new(GetType(), name);
		public SequenceNode() : this(null) { }
		public void SetChildren(params ISyntaxNode[] children) => Children = children.ToList();
		public SequenceNode WithChildren(params ISyntaxNode[] children) { SetChildren(children); return this; }
		IEnumerable<IEnumerable<object?>> ParseRecursive(TokenStream stream, int childIndex)
		{
			if (childIndex >= Children.Count) yield break;

			var ch = Children[childIndex];
			Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.ParseRecursive` calls `{ch}.Parse` at child {childIndex} `{Children[childIndex]}`");
			foreach (var chResult in ch.Parse(stream))
			{
				int checkpoint = stream.Index;
				if (childIndex == Children.Count - 1)
				{
					Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.ParseRecursive` yields `{chResult}` at child {childIndex} `{Children[childIndex]}`");
					yield return chResult.Array();
				}
				else
				{
					stream.Index = checkpoint;
					foreach (var restResults in ParseRecursive(stream, childIndex + 1))
					{
						Debug.WriteLineIf(Parser.LogDebug, $"{$"@\"{stream.Current}\"",-8}`{this}.ParseRecursive` yields `{chResult}` at child {childIndex} `{Children[childIndex]}`");
						yield return chResult.PrependTo(restResults);
					}
				}
			}
		}
		public IEnumerable<object?> Parse(TokenStream stream)
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
		public override string? ToString() => $"{Name}[{string.Join(" | ", Branches.Select(br => br.Name))}]";
		public List<ISyntaxNode> Branches { get; set; } = new();
		public Func<object?, object?>? Converter { get; set; } = null;
		public MultipleNode(string? name = null) => Name = new(GetType(), name);
		public MultipleNode() : this(null) { }
		public MultipleNode(string? name = null, params ISyntaxNode[] branches) : this(name) => SetBranches(branches);
		string? RenameBranch(ISyntaxNode branch, int index) =>
			branch.Name.Value ??= $"{Name}_{branch.Name.Value ?? index.ToString()}";
		public TBranch AddBranch<TBranch>(TBranch branch) where TBranch : ISyntaxNode
		{
			RenameBranch(branch, Branches.Count);
			Branches.Add(branch);
			return branch;
		}
		public TBranch NewBranch<TBranch>(string? name = null) where TBranch : ISyntaxNode, new()
		{
			TBranch branch = new() { Name = new(typeof(TBranch), name) };
			return AddBranch(branch);
		}
		public SequenceNode NewBranch(string? name = null, params ISyntaxNode[] sequence)
		{
			var branch = NewBranch<SequenceNode>(name);
			branch.SetChildren(sequence);
			return branch;
		}
		public SequenceNode NewBranch(params ISyntaxNode[] sequence) => NewBranch(null, sequence);
		public void SetBranches(params ISyntaxNode[] branches)
		{
			Branches = branches.ToList();
			_ = Branches.Select(RenameBranch);
		}
		public MultipleNode WithBranches(params ISyntaxNode[] branches) { SetBranches(branches); return this; }
		public IEnumerable<object?> Parse(TokenStream stream)
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
