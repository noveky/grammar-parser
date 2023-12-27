﻿using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace SyntaxParser
{
	public class Parser
	{
		public static bool LogDebug { get; set; } = false;

		public ISyntaxNode? RootSyntaxNode { get; set; }
		public bool IgnoreCase { get; set; } = false;
		public string? SkipPattern { get; set; }
		public bool DynamicRegexMatch { get; set; } = false;

		public RegexOptions RegexOptions => IgnoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

		public static TokenNode NewToken(string? name, string regex, Func<Token, object?>? builder = null)
		{
			TokenType tokenType = new(name, regex);
			return new TokenNode(name, tokenType, builder);
		}

		public IEnumerable<object?> Parse(string str)
		{
			if (RootSyntaxNode is null) throw new InvalidOperationException();
			var inputStream = new InputStream(this, str);
			IParserStream stream = DynamicRegexMatch ? inputStream : new TokenStream(inputStream);
			Debug.WriteLineIf(LogDebug, "Start to parse input string", "Parser");
			foreach (var result in RootSyntaxNode.Parse(stream))
			{
				if (!stream.AtEnd)
				{
					Debug.WriteLineIf(LogDebug, $"Discard result `{result}` for unparsed portion", "Parser");
					continue;
				}
				Debug.WriteLineIf(LogDebug, $"Accept result `{result}`", "Parser");
				yield return result;
			}
		}
	}

	public interface IParserStream
	{
		public Parser Parser { get; }
		public int Index { get; set; }
		public bool AtBegin { get; }
		public bool AtEnd { get; }
		public object? Current { get; }
		public Token? NextToken(TokenType? type);
	}

	public class TokenStream : IParserStream
	{
		readonly Token[] tokens;
		public Parser Parser { get; }
		public Token this[int index] => tokens[index];
		public IEnumerable<Token> this[Range range] => tokens[range];
		public int Index { get; set; } = -1;
		public TokenStream(Parser parser, IEnumerable<Token> tokens)
		{
			Parser = parser;
			this.tokens = tokens.ToArray();
		}
		public TokenStream(InputStream inputStream)
		{
			throw new NotImplementedException();
		}
		public bool AtBegin => Index < 0 &&
			(Index == -1 ? true : throw new IndexOutOfRangeException());
		public bool AtEnd => Index >= tokens.Length - 1 &&
			(Index == tokens.Length - 1 ? true : throw new IndexOutOfRangeException());
		public object? Current => AtBegin ? null : tokens[Index];
		public Token? NextToken(TokenType? type)
		{
			var token = AtEnd ? null : tokens[++Index];
			return token?.Type == type ? token : null;
		}
	}

	public class InputStream : IParserStream
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
		public object? Current => AtEnd ? null : str[Index];
		public string? Rest => AtEnd ? null : this[Index..];
		public Token? NextToken(TokenType? type)
		{
			if (type is null || AtEnd || Rest is null) return null;

			if (Parser.SkipPattern is not null)
			{
				Index += Regex.Match(Rest, $"^{Parser.SkipPattern}", Parser.RegexOptions).Length;
			}

			var match = Regex.Match(Rest, $"^{type.RegexPattern}", Parser.RegexOptions);
			if (!match.Success)
			{
				return null;
			}
			Index += match.Length;

			return new Token(type, match.Value);
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
		public IEnumerable<object?> Parse(IParserStream stream);
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
		public IEnumerable<object?> Parse(IParserStream stream)
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
		public IEnumerable<object?> Parse(IParserStream stream)
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
		public void SetChildren(params ISyntaxNode[] children) => Children = children.ToList();
		public SequenceNode WithChildren(params ISyntaxNode[] children) { SetChildren(children); return this; }
		IEnumerable<IEnumerable<object?>> ParseRecursive(IParserStream stream, int childIndex)
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
		public IEnumerable<object?> Parse(IParserStream stream)
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
		public IEnumerable<object?> Parse(IParserStream stream)
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
