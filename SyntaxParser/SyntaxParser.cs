using SyntaxParser.Shared;
using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SyntaxParser
{
	public class Parser
	{
		public static bool DebugMode { get; set; } = false;

		public static void LogDebug(string? message, string? category = null) => Debug.WriteLineIf(DebugMode, message, category);
		public static void LogDebug(InputStream stream, string? message, string? category = null) => LogDebug($"{$"@\"{stream.Current}\"",-8}{message}", category);

		ISyntaxDef SyntaxDef
		{
			set
			{
				foreach (var kvp in value.ToExpando())
				{
					var name = kvp.Key;
					if (kvp.Value is not INode node) continue;
					node.Name.Value = name;
					if (node is ITokenNode tokenNode)
					{
						tokenNode.TokenType.Regex = tokenNode.Regex;
					}
					if (node is IMultiNode multiNode)
					{
						_ = multiNode.Branches.Select(multiNode.RenameBranch).ToArray();
					}
				}
				RootNode = value.RootNode;
			}
		}
		public INode? RootNode { get; private set; }
		public bool IgnoreCase { get; set; } = false;
		public string? SkipPattern { get; set; }
		public RegexOptions RegexOptions => IgnoreCase? RegexOptions.IgnoreCase : RegexOptions.None;

		public Parser SetIgnoreCase(bool ignoreCase = true) { IgnoreCase = ignoreCase; return this; }
		public Parser SetSkipPattern(string? skipPattern) { SkipPattern = skipPattern; return this; }
		public Parser SetSyntaxDef(ISyntaxDef syntaxDef) { SyntaxDef = syntaxDef; return this; }
		
		public IEnumerable<object?> Parse(string str)
		{
			if (RootNode is null) throw new InvalidOperationException();

			var stream = new InputStream(this, str);
			LogDebug("Start to parse input string", "Parser");
			foreach (var result in RootNode.Parse(stream))
			{
				if (!stream.AtEnd)
				{
					LogDebug($"Discard result `{result}` for unparsed portion: \"{stream.Rest}\"", "Parser");
					continue;
				}
				LogDebug($"Accept result `{result}`", "Parser");
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

			var match = Regex.Match(Rest, $"^{type.Regex ?? throw new InvalidOperationException()}", Parser.RegexOptions);
			if (!match.Success)
			{
				return null;
			}
			foreach (var coverType in type.CoverTypes)
			{
				if (Regex.IsMatch(match.Value, $"{coverType.Regex ?? throw new InvalidOperationException()}", Parser.RegexOptions))
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
		public Name(Type type, string? value = null)
		{
			this.type = type;
			Value = value;
		}

		public override string? ToString() => Value ?? type?.Name;
	}

	public interface INameable
	{
		public Name Name { get; }
	}

	public class TokenType : INameable
	{
		public List<TokenType> CoverTypes { get; set; } = new();
		public Name Name { get; }
		public string? Regex { get; set; }
		public TokenType() => Name = new(GetType());

		public override string? ToString() => $"{Name}(\"{Regex}\")";
	}

	public class Token
	{
		public TokenType Type { get; set; }
		public string Value { get; set; }
		public Token(TokenType type, string value)
		{
			Type = type;
			Value = value;
		}

		public override string? ToString() => $"{Type.Name}(\"{Value}\")";
	}

	public interface ISyntaxDef
	{
		public INode RootNode { get; }
	}

	public static class Syntax
	{
		public static EmptyNode Empty() => new();
		public static SeqNode<T> Sequence<T>(params INode[] children) => new SeqNode<T>().SetChildren(children);
		public static MultiNode<T> Multiple<T>(params INode<T>[] branches) => new MultiNode<T>().AddBranches(branches);
		public static ConvertNode<T> Converter<T>(params INode[] branches) => new ConvertNode<T>().AddBranches(branches);

		public static class Sugar
		{
			public static INode<IEnumerable<T>> List<T>(INode<T> element, INode? separator = null)
			{
				var restElements = Multiple<IEnumerable<T>?>();
				restElements.NewSeqBranch(Empty()).SetBuilder(a => null);
				restElements.NewSeqBranch(separator is null
					? new INode[] { element, restElements }
					: new INode[] { separator, element, restElements })
					.SetBuilder(a => a.At(element).PrependTo(a.At(restElements)));

				var elements = Sequence<IEnumerable<T>>(element, restElements)
					.SetBuilder(a => a.At(element).PrependTo(a.At(restElements)));

				return elements;
			}
		}
	}

	public interface INode : INameable
	{
		public IEnumerable<object?> Parse(InputStream stream);
	}
	public interface INode<T> : INode { }

	public interface ITokenNode : INode
	{
		public string? Regex { get; set; }
		public TokenType TokenType { get; set; }
	}

	public interface ISeqNode : INode
	{
		public class Results
		{
			readonly ISeqNode seqNode;
			readonly object?[] objects;
			public Results(ISeqNode seqNode, object?[] objects)
			{
				this.seqNode = seqNode;
				this.objects = objects;
			}

			public object? this[int index] => objects[index];
			public TNode At<TNode>(INode<TNode> node, int order = 0) =>
				objects[seqNode.Children.Select((n, i) => new { n, i }).Where(x => x.n == node).ElementAt(order).i].As<TNode>();
		}

		public List<INode> Children { get; set; }
	}

	public interface IMultiNode : INode
	{
		public List<INode> Branches { get; set; }

		public string? RenameBranch(INode branch, int index) =>
			branch.Name.Value ??= $"{Name}_{index}";
	}

	public class EmptyNode : INode<object?>
	{
		public Name Name { get; }
		public EmptyNode() => Name = new(GetType(), "ε");

		public EmptyNode SetName(string? name) { Name.Value = name; return this; }

		public IEnumerable<object?> Parse(InputStream stream)
		{
			Parser.LogDebug(stream, $"`{this}.Parse` yields `{null}`");
			yield return null;
		}

		public override string? ToString() => $"{Name}";
	}

	public class TokenNode<T> : ITokenNode, INode<T>
	{
		public delegate T BuilderFunc(Token t);

		public Name Name { get; }
		public string? Regex { get; set; }
		public TokenType TokenType { get; set; } = new();
		public BuilderFunc? Builder { get; set; }
		public TokenNode() => Name = TokenType.Name;

		public TokenNode<T> SetName(string? name) { Name.Value = name; return this; }
		public TokenNode<T> SetRegex(string regex) { Regex = regex; return this; }
		public TokenNode<T> SetBuilder(BuilderFunc builder) { Builder = builder; return this; }
		public TokenNode<T> SetCoverTypes(params ITokenNode[] nodes) { TokenType.CoverTypes = nodes.Select(node => node.TokenType).ToList(); return this; }

		public IEnumerable<object?> Parse(InputStream stream)
		{
			var token = stream.NextToken(TokenType);
			if (token is null)
			{
				Parser.LogDebug(stream, $"`{this}.Parse` yields nothing");
				yield break;
			}
			object? result = Builder is null ? token.Value : Builder.Invoke(token);
			Parser.LogDebug(stream, $"`{this}.Parse` yields `{result}`");
			yield return result;
		}

		public override string? ToString() => $"{Name}";
	}

	public class SeqNode<T> : ISeqNode, INode<T>
	{
		public delegate T BuilderFunc(ISeqNode.Results a);

		public Name Name { get; }
		public List<INode> Children { get; set; } = new();
		public BuilderFunc? Builder { get; set; }
		public SeqNode() => Name = new(GetType());

		public SeqNode<T> SetName(string? name) { Name.Value = name; return this; }
		public SeqNode<T> SetChildren(params INode[] children) { Children = children.ToList(); return this; }
		public SeqNode<T> SetBuilder(BuilderFunc builder) { Builder = builder; return this; }

		IEnumerable <IEnumerable<object?>> ParseRecursive(InputStream stream, int childIndex)
		{
			if (childIndex >= Children.Count) yield break;

			var ch = Children[childIndex];
			Parser.LogDebug(stream, $"`{this}.ParseRecursive` calls `{ch}.Parse` at child {childIndex} `{Children[childIndex]}`");
			foreach (var chResult in ch.Parse(stream))
			{
				int checkpoint = stream.Index;
				var chResultAsArray = new[] { chResult };
				if (childIndex == Children.Count - 1)
				{
					Parser.LogDebug(stream, $"`{this}.ParseRecursive` yields `{chResult}` at child {childIndex} `{Children[childIndex]}`");
					yield return chResultAsArray;
				}
				else
				{
					stream.Index = checkpoint;
					foreach (var restResults in ParseRecursive(stream, childIndex + 1))
					{
						Parser.LogDebug(stream, $"`{this}.ParseRecursive` yields `{chResult}` at child {childIndex} `{Children[childIndex]}`");
						yield return chResultAsArray.Concat(restResults);
					}
				}
			}
		}
		public IEnumerable<object?> Parse(InputStream stream)
		{
			foreach (var childrenResults in ParseRecursive(stream, 0))
			{
				if (Builder is null) throw new InvalidOperationException();

				Parser.LogDebug(stream, $"`{this}.Parse` yields `{childrenResults}`");
				yield return Builder.Invoke(new ISeqNode.Results(this, childrenResults.ToArray()));
			}
		}

		public override string? ToString() => $"{Name}({string.Join(", ", Children.Select(ch => ch.Name))})";
	}

	public abstract class MultiNode : IMultiNode
	{
		public Name Name { get; }
		public List<INode> Branches { get; set; } = new();
		public MultiNode() => Name = new(GetType());

		public virtual IEnumerable<object?> Parse(InputStream stream)
		{
			var checkpoint = stream.Index;
			foreach (var br in Branches)
			{
				stream.Index = checkpoint;
				Parser.LogDebug(stream, $"`{this}.Parse` calls `{br}.Parse`");
				foreach (var result in br.Parse(stream))
				{
					Parser.LogDebug(stream, $"`{this}.Parse` yields `{result}`");
					yield return result;
				}
			}
		}

		public override string? ToString() => $"{Name}[{string.Join(" | ", Branches.Select(br => br.Name))}]";
	}

	public class MultiNode<T> : MultiNode, INode<T>
	{
		public SeqNode<T> NewSeqBranch(params INode[] children)
		{
			var branch = new SeqNode<T>().SetChildren(children);
			Branches.Add(branch);
			return branch;
		}

		public MultiNode<T> SetName(string? name) { Name.Value = name; return this; }
		public MultiNode<T> AddBranches(params INode<T>[] branches) { Branches.AddRange(branches); return this; }
		public MultiNode<T> AddBranch(INode<T> branch) => AddBranches(branch.Array());

		public override IEnumerable<object?> Parse(InputStream stream)
		{
			foreach (var baseResult in base.Parse(stream))
			{
				Parser.LogDebug(stream, $"`{this}.Parse` yields `{baseResult}`");
				yield return baseResult;
			}
		}
	}

	public class ConvertNode<T> : MultiNode, INode<T>
	{
		public delegate T ConverterFunc(object? o);

		public ConverterFunc? Converter { get; set; } = null;

		public ConvertNode<T> SetName(string? name) { Name.Value = name; return this; }
		public ConvertNode<T> AddBranches(params INode[] branches) { Branches.AddRange(branches); return this; }
		public ConvertNode<T> AddBranch(INode<T> branch) => AddBranches(branch.Array());
		public ConvertNode<T> SetConverter(ConverterFunc converter) { Converter = converter; return this; }

		public override IEnumerable<object?> Parse(InputStream stream)
		{
			foreach (var baseResult in base.Parse(stream))
			{
				if (Converter is null) throw new InvalidOperationException();

				var result = Converter.Invoke(baseResult).As<T>();
				Parser.LogDebug(stream, $"`{this}.Parse` yields `{result}`");
				yield return result;
			}
		}
	}
}
