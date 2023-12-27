﻿using SyntaxParser.Shared;

namespace SyntaxParser.Demo.Parsers.Sql
{
	public class SqlParser
	{
		readonly Parser parser = new();

		public SqlParser()
		{
			parser.IgnoreCase = true; // Ignore case
			parser.SkipPattern = @"\s+"; // Skip whitespace

			// Tokens

			var udecimal = parser.NewToken("udecimal", @"\b\d+\.\d+\b", t => decimal.Parse(t.Value));
			var @uint = parser.NewToken("uint", @"\b\d+\b", t => int.Parse(t.Value));
			var @true = parser.NewToken("true", @"\bTRUE\b", t => true);
			var @false = parser.NewToken("false", @"\bFALSE\b", t => false);
			var @select = parser.NewToken("select", @"\bSELECT\b");
			var @from = parser.NewToken("from", @"\bFROM\b");
			var @where = parser.NewToken("where", @"\bWHERE\b");
			var @as = parser.NewToken("as", @"\bAS\b");
			var and = parser.NewToken("and", @"\bAND\b");
			var or = parser.NewToken("or", @"\bOR\b");
			var not = parser.NewToken("not", @"\bNOT\b");
			var id = parser.NewToken("id", @"\b[A-Za-z_]+[A-Za-z0-9_]*\b", t => t.Value);
			var lParen = parser.NewToken("lParen", @"\(");
			var rParen = parser.NewToken("rParen", @"\)");
			var comma = parser.NewToken("comma", @",");
			var dot = parser.NewToken("dot", @"\.");
			var star = parser.NewToken("star", @"\*");
			var eq = parser.NewToken("eq", @"=");
			var ne = parser.NewToken("ne", @"<>");
			var lt = parser.NewToken("lt", @"<");
			var le = parser.NewToken("le", @"<=");
			var gt = parser.NewToken("gt", @">");
			var ge = parser.NewToken("ge", @">=");
			var plus = parser.NewToken("plus", @"\+");
			var minus = parser.NewToken("minus", @"-");
			var multiply = parser.NewToken("multiply", @"\*");
			var divide = parser.NewToken("divide", @"/");

			id.CoverBy(@true, @false);

			// Syntax nodes

			var empty = Syntax.Empty("ε");

			var value = Syntax.Multi("value");
			{
				var __ = value.NewBranch();
				__.SetChildren(@uint);
				__.Builder = a => new Value(a[0].AsInt());
			}
			{
				var __ = value.NewBranch(udecimal);
				__.Builder = a => new Value(a[0].AsDecimal());
			}
			{
				var __ = value.NewBranch<MultipleNode>();
				__.SetBranches(@true, @false);
				__.Converter = o => new Value(o.AsBool());
			}

			var asAlias = Syntax.Multi("asAlias");
			{
				_ = asAlias.AddBranch(empty);
			}
			{
				_ = asAlias.AddBranch(id);
			}
			{
				var __ = asAlias.NewBranch(@as, id);
				__.Builder = a => a[1];
			}

			var fieldName = Syntax.Multi("fieldName");
			{
				var __ = fieldName.NewBranch(star);
				__.Builder = a => "*";
			}
			{
				_ = fieldName.AddBranch(id);
			}

			var attr = Syntax.Multi("attr");
			{
				var __ = attr.NewBranch(fieldName);
				__.Builder = a => new Attr()
				{
					FieldName = a[0].AsString(),
				};
			}
			{
				var __ = attr.NewBranch(id, dot, fieldName);
				__.Builder = a => new Attr()
				{
					RelationName = a[0].AsString(),
					FieldName = a[2].AsString(),
				};
			}

			var compOper = Syntax.Multi("compOper");
			{
				var __ = compOper.NewBranch(eq);
				__.Builder = a => Operator.Comp.Eq;
			}
			{
				var __ = compOper.NewBranch(ne);
				__.Builder = a => Operator.Comp.Ne;
			}
			{
				var __ = compOper.NewBranch(lt);
				__.Builder = a => Operator.Comp.Lt;
			}
			{
				var __ = compOper.NewBranch(le);
				__.Builder = a => Operator.Comp.Le;
			}
			{
				var __ = compOper.NewBranch(gt);
				__.Builder = a => Operator.Comp.Gt;
			}
			{
				var __ = compOper.NewBranch(ge);
				__.Builder = a => Operator.Comp.Ge;
			}

			var binaryArithOper = Syntax.Multi("binaryArithOper");
			{
				var __ = binaryArithOper.NewBranch(plus);
				__.Builder = a => Operator.Arith.Add;
			}
			{
				var __ = binaryArithOper.NewBranch(minus);
				__.Builder = a => Operator.Arith.Subtract;
			}
			{
				var __ = binaryArithOper.NewBranch(multiply);
				__.Builder = a => Operator.Arith.Multiply;
			}
			{
				var __ = binaryArithOper.NewBranch(divide);
				__.Builder = a => Operator.Arith.Divide;
			}

			var unaryArithOper = Syntax.Multi("unaryArithOper");
			{
				var __ = unaryArithOper.NewBranch("negative", minus);
				__.Builder = a => Operator.Arith.Negative;
			}

			var binaryLogicalOper = Syntax.Multi("binaryLogicalOper");
			{
				var __ = binaryLogicalOper.NewBranch(and);
				__.Builder = a => Operator.Logical.And;
			}
			{
				var __ = binaryLogicalOper.NewBranch(or);
				__.Builder = a => Operator.Logical.Or;
			}

			var unaryLogicalOper = Syntax.Multi("unaryLogicalOper");
			{
				var __ = unaryLogicalOper.NewBranch(not);
				__.Builder = a => Operator.Logical.Not;
			}

			var expr = Syntax.Multi("expr");

			var expr4 = Syntax.Multi("expr4");
			{
				var __ = expr4.NewBranch("valueExpr", value);
				__.Builder = a => new ValueExpr(a[0].As<Value>());
			}
			{
				var __ = expr4.NewBranch("attrExpr", attr);
				__.Builder = a => new AttrExpr(a[0].As<Attr>());
			}
			{
				var __ = expr4.NewBranch("parensExpr", lParen, expr, rParen);
				__.Builder = a => new ParensExpr(a[1].As<Expr>());
			}

			var expr3 = Syntax.Multi("expr3");
			{
				_ = expr3.AddBranch(expr4);
			}
			{
				var __ = expr3.NewBranch("unaryArithExpr", unaryArithOper, expr4);
				__.Builder = a =>
				{
					var _unaryArithOper = a[0].As<Operator.Arith>();
					var _expr4 = a[1].As<Expr>();
					return OperatorExpr<Operator.Arith>.Unary(_unaryArithOper, _expr4);
				};
			}

			var expr2 = Syntax.Multi("expr2");
			{
				_ = expr2.AddBranch(expr3);
			}
			{
				var __ = expr2.NewBranch("binaryArithExpr", expr3, binaryArithOper, expr2);
				__.Builder = a =>
				{
					var _expr3 = a[0].As<Expr>();
					var _binaryArithOper = a[1].As<Operator.Arith>();
					var _expr2 = a[2].As<Expr>();
					return _expr2 is OperatorExpr<Operator.Arith> other && !other.IsUnary
						? OperatorExpr<Operator.Arith>.JoinRest(_expr3, _binaryArithOper, other)
						: OperatorExpr<Operator.Arith>.Binary(_expr3, _binaryArithOper, _expr2);
				};
			}

			var expr1 = Syntax.Multi("expr1");
			{
				_ = expr1.AddBranch(expr2);
			}
			{
				var __ = expr1.NewBranch("compExpr", expr2, compOper, expr1);
				__.Builder = a => OperatorExpr<Operator.Comp>.Binary
				(
					left: a[0].As<Expr>(),
					oper: a[1].As<Operator.Comp>(),
					right: a[2].As<Expr>()
				);
			}

			var expr0 = Syntax.Multi("expr0");
			{
				_ = expr0.AddBranch(expr1);
			}
			{
				var __ = expr0.NewBranch("unaryLogicalExpr", unaryLogicalOper, expr1);
				__.Builder = a =>
				{
					var _unaryLogicalOper = a[0].As<Operator.Logical>();
					var _expr1 = a[1].As<Expr>();
					return OperatorExpr<Operator.Logical>.Unary(_unaryLogicalOper, _expr1);
				};
			}

			_ = expr;
			{
				_ = expr.AddBranch(expr0);
			}
			{
				var __ = expr.NewBranch("binaryLogicalExpr", expr0, binaryLogicalOper, expr);
				__.Builder = a =>
				{
					var _expr0 = a[0].As<Expr>();
					var _binaryLogicalOper = a[1].As<Operator.Logical>();
					var _expr = a[2].As<Expr>();
					return _expr is OperatorExpr<Operator.Logical> other && !other.IsUnary
						? OperatorExpr<Operator.Logical>.JoinRest(_expr0, _binaryLogicalOper, other)
						: OperatorExpr<Operator.Logical>.Binary(_expr0, _binaryLogicalOper, _expr);
				};
			}

			var selectExpr = Syntax.Seq("selectExpr");
			{
				var __ = selectExpr;
				__.SetChildren(expr, asAlias);
				__.Builder = a =>
				{
					var _expr = a[0].As<Expr>();
					var _asAlias = a[1].AsString();
					if (_expr is not null) _expr.Alias = _asAlias;
					return _expr;
				};
			}

			var restSelectExprs = Syntax.Multi("restSelectExprs");
			{
				_ = restSelectExprs.AddBranch(empty);
			}
			{
				var __ = restSelectExprs.NewBranch(comma, selectExpr, restSelectExprs);
				__.Builder = a => a[1].PrependTo<Expr>(a[2]);
			}

			var whereClause = Syntax.Multi("whereClause");
			{
				_ = whereClause.AddBranch(empty);
			}
			{
				var __ = whereClause.NewBranch(where, expr);
				__.Builder = a => a[1];
			}

			var relation = Syntax.Seq("relation");
			{
				var __ = relation.WithChildren(id, asAlias);
				__.Builder = a => new Relation()
				{
					Name = a[0].AsString(),
					Alias = a[1].AsString(),
				};
			}

			var restRelations = Syntax.Multi("restRelations");
			{
				_ = restRelations.AddBranch(empty);
			}
			{
				var __ = restRelations.NewBranch(comma, relation, restRelations);
				__.Builder = a => a[1].PrependTo<Relation>(a[2]);
			}

			var fromClause = Syntax.Multi("fromClause");
			{
				_ = fromClause.AddBranch(empty);
			}
			{
				var __ = fromClause.NewBranch(from, relation, restRelations);
				__.Builder = a => a[1].PrependTo<Relation>(a[2]);
			}

			var selectStmt = Syntax.Seq("selectStmt");
			{
				var __ = selectStmt.WithChildren(select, selectExpr, restSelectExprs, fromClause, whereClause);
				__.Builder = a =>
				{
					var _from = a[3].AsEnumerable<Relation>();
					var _where = a[4].As<Expr>();
					var selectExprs = a[1].PrependTo<Expr>(a[2]);
					return new SelectSqlNode
					{
						Exprs = selectExprs,
						Relations = _from,
						Condition = _where,
					};
				};
			}

			var stmt = Syntax.Multi("stmt", selectStmt);

			parser.RootSyntaxNode = stmt;
		}

		public IEnumerable<object?> Parse(string str) => parser.Parse(str);
	}
}
