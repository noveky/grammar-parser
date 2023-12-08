using SyntaxParser.Shared;
using System.Diagnostics;
using System.Dynamic;

namespace SyntaxParser.Demo.Parsers.Sql
{
	public class SqlParser
	{
		readonly Parser parser = new();

		public SqlParser()
		{
			parser.IgnoreCase = true;
			parser.IgnorePattern = @"\s+";

			// Tokens

			var uDecimalToken = Parser.NewToken("uDecimal", @"\b\d+\.\d+\b", t => decimal.Parse(t.Value));
			var uIntToken = Parser.NewToken("uInt", @"\b\d+\b", t => int.Parse(t.Value));
			var trueToken = Parser.NewToken("true", @"\bTRUE\b", t => true);
			var falseToken = Parser.NewToken("false", @"\bFALSE\b", t => false);
			var selectToken = Parser.NewToken("select", @"\bSELECT\b");
			var fromToken = Parser.NewToken("from", @"\bFROM\b");
			var whereToken = Parser.NewToken("where", @"\bWHERE\b");
			var asToken = Parser.NewToken("as", @"\bAS\b");
			var andToken = Parser.NewToken("and", @"\bAND\b");
			var orToken = Parser.NewToken("or", @"\bOR\b");
			var notToken = Parser.NewToken("not", @"\bNOT\b");
			var idToken = Parser.NewToken("id", @"\b[A-Za-z_]+[A-Za-z0-9_]*\b", t => t.Value);
			var lParenToken = Parser.NewToken("lParen", @"\(");
			var rParenToken = Parser.NewToken("rParen", @"\)");
			var commaToken = Parser.NewToken("comma", @",");
			var dotToken = Parser.NewToken("dot", @"\.");
			var starToken = Parser.NewToken("star", @"\*");
			var eqToken = Parser.NewToken("eq", @"=");
			var neToken = Parser.NewToken("ne", @"<>");
			var ltToken = Parser.NewToken("lt", @"<");
			var leToken = Parser.NewToken("le", @"<=");
			var gtToken = Parser.NewToken("gt", @">");
			var geToken = Parser.NewToken("ge", @">=");
			var plusToken = Parser.NewToken("plus", @"\+");
			var minusToken = Parser.NewToken("minus", @"-");
			var multiplyToken = Parser.NewToken("multiply", @"\*");
			var divideToken = Parser.NewToken("divide", @"/");

			idToken.CoverBy(trueToken, falseToken);

			// Syntax nodes

			var empty = new EmptyNode("ε");

			var value = new MultipleNode("value");
			{
				var __ = value.NewChild<SequenceNode>();
				__.SetChildren(uIntToken);
				__.Builder = a => new Value(a[0].AsInt());
			}
			{
				var __ = value.NewChild<SequenceNode>();
				__.SetChildren(uDecimalToken);
				__.Builder = a => new Value(a[0].AsDecimal());
			}
			{
				var __ = value.NewChild<MultipleNode>();
				__.SetChildren(trueToken, falseToken);
				__.Converter = o => new Value(o.AsBool());
			}

			var asAlias = new MultipleNode("asAlias");
			{
				var __ = asAlias.NewChild(empty);
			}
			{
				var __ = asAlias.NewChild(idToken);
			}
			{
				var __ = asAlias.NewChild<SequenceNode>();
				__.SetChildren(asToken, idToken);
				__.Builder = a => a[1];
			}

			var fieldName = new MultipleNode("fieldName");
			{
				var __ = fieldName.NewChild<SequenceNode>();
				__.SetChildren(starToken);
				__.Builder = a => "*";
			}
			{
				var __ = fieldName.NewChild(idToken);
			}

			var attr = new MultipleNode("attr");
			{
				var __ = attr.NewChild<SequenceNode>();
				__.SetChildren(fieldName);
				__.Builder = a => new Attr()
				{
					FieldName = a[0].AsString(),
				};
			}
			{
				var __ = attr.NewChild<SequenceNode>();
				__.SetChildren(idToken, dotToken, fieldName);
				__.Builder = a => new Attr()
				{
					RelationName = a[0].AsString(),
					FieldName = a[2].AsString(),
				};
			}

			var compOper = new MultipleNode("compOper");
			{
				var __ = compOper.NewChild<SequenceNode>("eq");
				__.SetChildren(eqToken);
				__.Builder = a => Operator.Comp.Eq;
			}
			{
				var __ = compOper.NewChild<SequenceNode>("ne");
				__.SetChildren(neToken);
				__.Builder = a => Operator.Comp.Ne;
			}
			{
				var __ = compOper.NewChild<SequenceNode>("lt");
				__.SetChildren(ltToken);
				__.Builder = a => Operator.Comp.Lt;
			}
			{
				var __ = compOper.NewChild<SequenceNode>("le");
				__.SetChildren(leToken);
				__.Builder = a => Operator.Comp.Le;
			}
			{
				var __ = compOper.NewChild<SequenceNode>("gt");
				__.SetChildren(gtToken);
				__.Builder = a => Operator.Comp.Gt;
			}
			{
				var __ = compOper.NewChild<SequenceNode>("ge");
				__.SetChildren(geToken);
				__.Builder = a => Operator.Comp.Ge;
			}

			var binaryArithOper = new MultipleNode("binaryArithOper");
			{
				var __ = binaryArithOper.NewChild<SequenceNode>("add");
				__.SetChildren(plusToken);
				__.Builder = a => Operator.Arith.Add;
			}
			{
				var __ = binaryArithOper.NewChild<SequenceNode>("subtract");
				__.SetChildren(minusToken);
				__.Builder = a => Operator.Arith.Subtract;
			}
			{
				var __ = binaryArithOper.NewChild<SequenceNode>("multiply");
				__.SetChildren(multiplyToken);
				__.Builder = a => Operator.Arith.Multiply;
			}
			{
				var __ = binaryArithOper.NewChild<SequenceNode>("divide");
				__.SetChildren(divideToken);
				__.Builder = a => Operator.Arith.Divide;
			}

			var unaryArithOper = new MultipleNode("unaryArithOper");
			{
				var __ = unaryArithOper.NewChild<SequenceNode>("negative");
				__.SetChildren(minusToken);
				__.Builder = a => Operator.Arith.Negative;
			}

			var binaryLogicalOper = new MultipleNode("binaryLogicalOper");
			{
				var __ = binaryLogicalOper.NewChild<SequenceNode>("and");
				__.SetChildren(andToken);
				__.Builder = a => Operator.Logical.And;
			}
			{
				var __ = binaryLogicalOper.NewChild<SequenceNode>("or");
				__.SetChildren(orToken);
				__.Builder = a => Operator.Logical.Or;
			}

			var unaryLogicalOper = new MultipleNode("unaryLogicalOper");
			{
				var __ = unaryLogicalOper.NewChild<SequenceNode>("not");
				__.SetChildren(notToken);
				__.Builder = a => Operator.Logical.Not;
			}

			var expression = new MultipleNode("expression");

			var expr4 = new MultipleNode("expr4");
			{
				var __ = expr4.NewChild<SequenceNode>("valueExpr");
				__.SetChildren(value);
				__.Builder = a => new ValueExpr(a[0].As<Value>());
			}
			{
				var __ = expr4.NewChild<SequenceNode>("attrExpr");
				__.SetChildren(attr);
				__.Builder = a => new AttrExpr(a[0].As<Attr>());
			}
			{
				var __ = expr4.NewChild<SequenceNode>("parensExpr");
				__.SetChildren(lParenToken, expression, rParenToken);
				__.Builder = a => new ParensExpr(a[1].As<Expression>());
			}

			var expr3 = new MultipleNode("expr3");
			{
				var __ = expr3.NewChild(expr4);
			}
			{
				var __ = expr3.NewChild<SequenceNode>("unaryArithExpr");
				__.SetChildren(unaryArithOper, expr4);
				__.Builder = a =>
				{
					var _unaryArithOper = a[0].As<Operator.Arith>();
					var _expr4 = a[1].As<Expression>();
					return OperatorExpr<Operator.Arith>.Unary(_unaryArithOper, _expr4);
				};
			}

			var expr2 = new MultipleNode("expr2");
			{
				var __ = expr2.NewChild(expr3);
			}
			{
				var __ = expr2.NewChild<SequenceNode>("binaryArithExpr");
				__.SetChildren(expr3, binaryArithOper, expr2);
				__.Builder = a =>
				{
					var _expr3 = a[0].As<Expression>();
					var _binaryArithOper = a[1].As<Operator.Arith>();
					var _expr2 = a[2].As<Expression>();
					return _expr2 is OperatorExpr<Operator.Arith> other && !other.IsUnary
						? OperatorExpr<Operator.Arith>.JoinRest(_expr3, _binaryArithOper, other)
						: OperatorExpr<Operator.Arith>.Binary(_expr3, _binaryArithOper, _expr2);
				};
			}

			var expr1 = new MultipleNode("expr1");
			{
				var __ = expr1.NewChild(expr2);
			}
			{
				var __ = expr1.NewChild<SequenceNode>("compExpr");
				__.SetChildren(expr2, compOper, expr1);
				__.Builder = a => OperatorExpr<Operator.Comp>.Binary
				(
					left: a[0].As<Expression>(),
					oper: a[1].As<Operator.Comp>(),
					right: a[2].As<Expression>()
				);
			}

			var expr0 = new MultipleNode("expr0");
			{
				var __ = expr0.NewChild(expr1);
			}
			{
				var __ = expr0.NewChild<SequenceNode>("unaryLogicalExpr");
				__.SetChildren(unaryLogicalOper, expr1);
				__.Builder = a =>
				{
					var _unaryLogicalOper = a[0].As<Operator.Logical>();
					var _expr1 = a[1].As<Expression>();
					return OperatorExpr<Operator.Logical>.Unary(_unaryLogicalOper, _expr1);
				};
			}

			_ = expression;
			{
				var __ = expression.NewChild(expr0);
			}
			{
				var __ = expression.NewChild<SequenceNode>("binaryLogicalExpr");
				__.SetChildren(expr0, binaryLogicalOper, expression);
				__.Builder = a =>
				{
					var _expr0 = a[0].As<Expression>();
					var _binaryLogicalOper = a[1].As<Operator.Logical>();
					var _expression = a[2].As<Expression>();
					return _expression is OperatorExpr<Operator.Logical> other && !other.IsUnary
						? OperatorExpr<Operator.Logical>.JoinRest(_expr0, _binaryLogicalOper, other)
						: OperatorExpr<Operator.Logical>.Binary(_expr0, _binaryLogicalOper, _expression);
				};
			}

			var selectExpr = new SequenceNode("selectExpr");
			{
				var __ = selectExpr;
				__.SetChildren(expression, asAlias);
				__.Builder = a =>
				{
					var _expression = a[0].As<Expression>();
					var _asAlias = a[1].AsString();
					if (_expression is not null) _expression.Alias = _asAlias;
					return _expression;
				};
			}

			var restSelectExprs = new MultipleNode("restExpressions");
			{
				var __ = restSelectExprs.NewChild(empty);
			}
			{
				var __ = restSelectExprs.NewChild<SequenceNode>();
				__.SetChildren(commaToken, selectExpr, restSelectExprs);
				__.Builder = a => a[1].PrependTo<Expression>(a[2]);
			}

			var where = new MultipleNode("where");
			{
				var __ = where.NewChild(empty);
			}
			{
				var __ = where.NewChild<SequenceNode>();
				__.SetChildren(whereToken, expression);
				__.Builder = a => a[1];
			}

			var relation = new SequenceNode("relation");
			{
				var __ = relation;
				__.SetChildren(idToken, asAlias);
				__.Builder = a =>
				{
					return new Relation()
					{
						Name = a[0].AsString(),
						Alias = a[1].AsString(),
					};
				};
			}

			var restRelations = new MultipleNode("restRelations");
			{
				var __ = restRelations.NewChild(empty);
			}
			{
				var __ = restRelations.NewChild<SequenceNode>();
				__.SetChildren(commaToken, relation, restRelations);
				__.Builder = a => a[1].PrependTo<Relation>(a[2]);
			}

			var from = new MultipleNode("from");
			{
				var __ = from.NewChild(empty);
			}
			{
				var __ = from.NewChild<SequenceNode>();
				__.SetChildren(fromToken, relation, restRelations);
				__.Builder = a => a[1].PrependTo<Relation>(a[2]);
			}

			var selectStmt = new SequenceNode("selectStmt");
			{
				var __ = selectStmt;
				__.SetChildren(selectToken, selectExpr, restSelectExprs, from, where);
				__.Builder = a =>
				{
					var _from = a[3].AsEnumerable<Relation>();
					var _where = a[4].As<Expression>();
					var selectExprs = a[1].PrependTo<Expression>(a[2]);
					return new SelectSqlNode
					{
						Expressions = selectExprs,
						Relations = _from,
						Condition = _where,
					};
				};
			}

			var stmt = new MultipleNode("stmt");
			{
				var __ = stmt;
				__.SetChildren(selectStmt);
			}

			parser.RootSyntaxNode = stmt;
		}

		public IEnumerable<object?> Parse(string str) => parser.Parse(str);
	}
}
