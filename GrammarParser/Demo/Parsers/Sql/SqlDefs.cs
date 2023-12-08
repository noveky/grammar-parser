using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarParser.Demo.Parsers.Sql
{
	public class Relation
	{
		public string? Name { get; set; }
		public string? Alias { get; set; }

		public override string? ToString() => $"{Name}{(Alias is null ? null : $" (as {Alias})")}";
	}

	public class Attribute
	{
		public string? RelationName { get; set; }
		public string? FieldName { get; set; }
		public string? Alias { get; set; }

		public override string? ToString() =>
			$"{(RelationName is null ? null : $"{RelationName}.")}{FieldName}{(Alias is null ? null : $" (as {Alias})")}";
	}

	public class Value
	{
		public Type? Type { get; set; }
		public object? ValueObj { get; set; }

		public Value(object? value)
		{
			Type = value?.GetType();
			ValueObj = value;
		}
		public override string? ToString() => $"{ValueObj}";
	}

	public static class Operator
	{
		public enum Assoc { None, Left, Right }
		public enum Arith { Add, Subtract, Negative }
		public enum Comp { Eq }

		public static bool IsUnary(Arith? @operator) =>
			@operator is Arith.Negative;
		public static Assoc GetAssoc(Arith? oper1, Arith? oper2) =>
			(oper1, oper2) switch
			{
				(Arith.Add, Arith.Add) => Assoc.Left,
				(Arith.Add, Arith.Subtract) => Assoc.Left,
				(Arith.Subtract, Arith.Add) => Assoc.Left,
				_ => Assoc.None,
			};
	}

	public abstract class Expression
	{
		public string? Name { get; set; }

		public enum ToStringOptions
		{
			None = 0,
			WithBrackets = 1 << 0,
		}
		public string? ToString(ToStringOptions options)
		{
			string? result = ToString();
			if ((options & ToStringOptions.WithBrackets) != 0)
			{
				if (this is not (ValueExpr or AttrExpr)) result = $"({result})";
			}
			return result;
		}
		public override string? ToString() => $"{Name}";
	}

	public class ValueExpr : Expression
	{
		public Value? Value { get; set; }
		public ValueExpr(Value? value) => Value = value;

		public override string? ToString() => $"{Value}";
	}

	public class AttrExpr : Expression
	{
		public Attribute? Attribute { get; set; }
		public AttrExpr(Attribute? attribute) => Attribute = attribute;

		public override string? ToString() => $"{Attribute}";
	}

	public class ArithExpr : Expression
	{
		public Operator.Arith? Oper { get; set; }
		public IEnumerable<Expression?>? Children { get; set; }
		public ArithExpr(Operator.Arith? oper, IEnumerable<Expression?>? children = null)
		{
			Oper = oper;
			Children = children;
		}
		public static ArithExpr Join(Operator.Arith? oper, Expression? left, Expression? right)
		{
			IEnumerable<Expression?>? children;
			if (left is ArithExpr l && right is ArithExpr r && Operator.GetAssoc(l.Oper, r.Oper) is Operator.Assoc lrAssoc && Operator.GetAssoc(l.Oper, oper) is Operator.Assoc lAssoc && Operator.GetAssoc(oper, r.Oper) is Operator.Assoc rAssoc && lAssoc == lrAssoc && rAssoc == lrAssoc && lrAssoc is not Operator.Assoc.None)
			{
				children = l.Children.JoinEnumerable(r.Children);
			}
			else if (left is ArithExpr l_ && Operator.GetAssoc(l_.Oper, oper) is not Operator.Assoc.None)
			{
				children = right.JoinAfter(l_.Children);
			}
			else if (right is ArithExpr r_ && Operator.GetAssoc(oper, r_.Oper) is not Operator.Assoc.None)
			{
				children = left.JoinBefore(r_.Children);
			}
			else
			{
				children = new[] { left, right };
			}
			return new(oper, children);
		}

		public override string? ToString()
		{
			var childStrs = Children?.Select(ch => ch?.ToString(ToStringOptions.WithBrackets)) ?? Enumerable.Empty<string?>();
			return Operator.IsUnary(Oper)
				? $"{Oper}{childStrs.SingleOrDefault()}"
				: string.Join(
					Oper switch
					{
						Operator.Arith.Add => " + ",
						Operator.Arith.Subtract => " - ",
						_ => null,
					},
					childStrs);
		}
	}

	public class CompExpr : Expression
	{
		public Operator.Comp? Oper { get; set; }
		public Expression? Left { get; set; }
		public Expression? Right { get; set; }
		public CompExpr(Operator.Comp? oper, Expression? left, Expression? right)
		{
			Oper = oper;
			Left = left;
			Right = right;
		}

		public override string? ToString() => $"{Left?.ToString(ToStringOptions.WithBrackets)} {Oper switch
		{
			Operator.Comp.Eq => "=",
			_ => null,
		}} {Right?.ToString(ToStringOptions.WithBrackets)}";
	}
}
