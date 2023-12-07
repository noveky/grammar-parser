using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GrammarParser.Demo.Parsers.Sql
{
	public enum CompOperator { Eq };
	public class RelationSqlNode
	{
		public string? Name { get; set; }
		public string? Alias { get; set; }
		public override string? ToString() => $"{Name}{(Alias is null ? null : $" (as {Alias})")}";
	}
	public class AttributeSqlNode
	{
		public string? RelationName { get; set; }
		public string? FieldName { get; set; }
		public string? Alias { get; set; }
		public override string? ToString() =>
			$"{(RelationName is null ? null : $"{RelationName}.")}{FieldName}{(Alias is null ? null : $" (as {Alias})")}";
	}
	public class ValueSqlNode
	{
		public Type? Type { get; set; }
		public object? Value { get; set; }
		public ValueSqlNode(object? value)
		{
			Type = value?.GetType();
			Value = value;
		}
		public override string? ToString() => $"{Value}";
	}
	public class ExpressionSqlNode
	{
		public string? Name { get; set; }
		public override string? ToString() => $"{Name}";
	}
	public class ValueExprSqlNode : ExpressionSqlNode
	{
		public ValueSqlNode? Value { get; set; }
		public override string? ToString() => $"{Value}";
	}
	public class AttrExprSqlNode : ExpressionSqlNode
	{
		public AttributeSqlNode? Attribute { get; set; }
		public override string? ToString() => $"{Attribute}";
	}
	public class CompExprSqlNode : ExpressionSqlNode
	{
		public CompOperator? Operator { get; set; }
		public ExpressionSqlNode? Left { get; set; }
		public ExpressionSqlNode? Right { get; set; }
		public override string? ToString() => $"{Left} {Operator} {Right}";
	}
}
