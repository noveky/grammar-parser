using GrammarParser.Shared;

namespace GrammarParser.Demo.Parsers.Sql
{
	public class SelectSqlNode
	{
		public IEnumerable<Expression?>? Expressions { get; set; }
		public IEnumerable<Relation?>? Relations { get; set; }
		public Expression? Condition { get; set; }

		public override string? ToString() => $"{GetType().Name} {{\n\texpressions: [\n\t\t{string.Join(",\n\t\t", Expressions ?? Enumerable.Empty<Expression?>())}\n\t],\n\trelations: [\n\t\t{string.Join(",\n\t\t", Relations ?? Enumerable.Empty<Relation?>())}\n\t],\n\tcondition: {Condition?.ToString() ?? "null"}\n}}";
	}

	public class Relation
	{
		public string? Name { get; set; }
		public string? Alias { get; set; }

		public override string? ToString() => $"{Name}{(Alias is null ? null : $" (alias: {Alias})")}";
	}

	public class Attribute
	{
		public string? RelationName { get; set; }
		public string? FieldName { get; set; }
		public string? Alias { get; set; }

		public override string? ToString() =>
			$"{(RelationName is null ? null : $"{RelationName}.")}{FieldName}{(Alias is null ? null : $" (alias: {Alias})")}";
	}

	public class Value
	{
		public object? ValueObj { get; set; }
		public Value(object? value) => ValueObj = value;

		public Type? Type => ValueObj?.GetType();
		public bool IsNumeric => Type.IsIn(null, typeof(byte), typeof(sbyte), typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal));
		public double? GetNumeric() => IsNumeric ? Convert.ToDouble(ValueObj) : null;

		public override string? ToString() => $"{ValueObj}";
	}

	public static class Operator
	{
		public enum Assoc { None, Left, Right }
		public enum Comp { Eq, Ne, Lt, Le, Gt, Ge }
		public enum Arith { Add, Subtract, Multiply, Divide, Negative }
		public enum Logical { And, Or, Not }
	}

	public abstract class Expression
	{
		public string? Alias { get; set; }

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
				if (this is not (ValueExpr or AttrExpr)
					|| (this is ValueExpr valueExpr && valueExpr.Value?.GetNumeric() is < 0))
				{
					result = $"({result})";
				}
			}
			return result;
		}
		public string? ToString(string? str) => $"{str}{(Alias is null ? null : $" (alias: {Alias})")}";
		public override string? ToString() => $"{Alias}";
	}

	public class ValueExpr : Expression
	{
		public Value? Value { get; set; }
		public ValueExpr(Value? value) => Value = value;

		public override string? ToString() => ToString($"{Value}");
	}

	public class AttrExpr : Expression
	{
		public Attribute? Attribute { get; set; }
		public AttrExpr(Attribute? attribute) => Attribute = attribute;

		public override string? ToString() => ToString($"{Attribute}");
	}

	public class ParensExpr : Expression
	{
		public Expression? Child { get; set; }
		public ParensExpr(Expression? child) => Child = child;

		public override string? ToString() => ToString($"{Child}");
	}

	public class CompExpr : Expression
	{
		public Operator.Comp? Oper { get; set; }
		public Expression? Left { get; set; }
		public Expression? Right { get; set; }
		public CompExpr(Expression? left, Operator.Comp? oper, Expression? right)
		{
			Oper = oper;
			Left = left;
			Right = right;
		}

		public override string? ToString() =>
			ToString($"{Left?.ToString(ToStringOptions.WithBrackets)}{Oper switch
			{
				Operator.Comp.Eq => " = ",
				Operator.Comp.Ne => " <> ",
				Operator.Comp.Lt => " < ",
				Operator.Comp.Le => " <= ",
				Operator.Comp.Gt => " > ",
				Operator.Comp.Ge => " >= ",
				_ => default,
			}}{Right?.ToString(ToStringOptions.WithBrackets)}");
	}

	public class ArithExpr : Expression
	{
		public class Unit
		{
			public Operator.Arith? Oper { get; set; }
			public Expression? Expression { get; set; }
			public Unit(Operator.Arith? oper, Expression? expression)
			{
				Oper = oper;
				Expression = expression;
			}
		}

		public IEnumerable<Unit?>? Children { get; set; }
		public ArithExpr(IEnumerable<Unit?>? children) => Children = children;
		public static ArithExpr Binary(Expression? left, Operator.Arith? oper, Expression? right)
			=> new(new Unit[] { new(null, left), new(oper, right) });
		public static ArithExpr Unary(Operator.Arith? oper, Expression? right)
			=> new(new Unit(oper, right).Array());
		public static ArithExpr JoinRest(Expression? expression, Operator.Arith? oper, ArithExpr? other)
		{
			var unit = new Unit(null, expression);
			var nextUnit = other?.Children?.Any() is true
				? new Unit(oper, other?.Children?.FirstOrDefault()?.Expression)
				: null;
			var restChildren = other?.Children?.Skip(1);
			return new(new[] { unit, nextUnit }.ConcatBefore(restChildren));
		}

		public bool IsUnary => Children?.FirstOrDefault()?.Oper is not null;

		public override string? ToString()
		{
			var childStrs = Children?
				.Select(ch => $"{ch?.Oper switch
				{
					Operator.Arith.Add => " + ",
					Operator.Arith.Subtract => " - ",
					Operator.Arith.Multiply => " * ",
					Operator.Arith.Divide => " / ",
					Operator.Arith.Negative => "-",
					_ => default,
				}}{ch?.Expression?.ToString(ToStringOptions.WithBrackets)}")
				?? Enumerable.Empty<string?>();
			return ToString(string.Concat(childStrs));
		}
	}

	public class LogicalExpr : Expression
	{
		public class Unit
		{
			public Operator.Logical? Oper { get; set; }
			public Expression? Expression { get; set; }
			public Unit(Operator.Logical? oper, Expression? expression)
			{
				Oper = oper;
				Expression = expression;
			}
		}

		public IEnumerable<Unit?>? Children { get; set; }
		public LogicalExpr(IEnumerable<Unit?>? children) => Children = children;
		public static LogicalExpr Binary(Expression? left, Operator.Logical? oper, Expression? right)
			=> new(new Unit[] { new(null, left), new(oper, right) });
		public static LogicalExpr Unary(Operator.Logical? oper, Expression? right)
			=> new(new Unit(oper, right).Array());
		public static LogicalExpr JoinRest(Expression? expression, Operator.Logical? oper, LogicalExpr? other)
		{
			var unit = new Unit(null, expression);
			var nextUnit = other?.Children?.Any() is true
				? new Unit(oper, other?.Children?.FirstOrDefault()?.Expression)
				: null;
			var restChildren = other?.Children?.Skip(1);
			return new(new[] { unit, nextUnit }.ConcatBefore(restChildren));
		}

		public bool IsUnary => Children?.FirstOrDefault()?.Oper is not null;

		public override string? ToString()
		{
			var childStrs = Children?
				.Select(ch => $"{ch?.Oper switch
				{
					Operator.Logical.And => " AND ",
					Operator.Logical.Or => " OR ",
					Operator.Logical.Not => "NOT ",
					_ => default,
				}}{ch?.Expression?.ToString(ToStringOptions.WithBrackets)}")
				?? Enumerable.Empty<string?>();
			return ToString(string.Concat(childStrs));
		}
	}
}
