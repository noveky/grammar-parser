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

			var udecimal = Parser.NewToken<decimal>("udecimal", @"\b\d+\.\d+\b", t => decimal.Parse(t.Value));
			var @uint = Parser.NewToken<int>("uint", @"\b\d+\b", t => int.Parse(t.Value));
			var @true = Parser.NewToken<bool>("true", @"\bTRUE\b", t => true);
			var @false = Parser.NewToken<bool>("false", @"\bFALSE\b", t => false);
			var @select = Parser.NewToken("select", @"\bSELECT\b");
			var @from = Parser.NewToken("from", @"\bFROM\b");
			var @where = Parser.NewToken("where", @"\bWHERE\b");
			var @as = Parser.NewToken("as", @"\bAS\b");
			var and = Parser.NewToken("and", @"\bAND\b");
			var or = Parser.NewToken("or", @"\bOR\b");
			var not = Parser.NewToken("not", @"\bNOT\b");
			var id = Parser.NewToken<string>("id", @"\b[A-Za-z_]+[A-Za-z0-9_]*\b", t => t.Value);
			var lParen = Parser.NewToken("lParen", @"\(");
			var rParen = Parser.NewToken("rParen", @"\)");
			var comma = Parser.NewToken("comma", @",");
			var dot = Parser.NewToken("dot", @"\.");
			var star = Parser.NewToken("star", @"\*");
			var eq = Parser.NewToken("eq", @"=");
			var ne = Parser.NewToken("ne", @"<>");
			var lt = Parser.NewToken("lt", @"<");
			var le = Parser.NewToken("le", @"<=");
			var gt = Parser.NewToken("gt", @">");
			var ge = Parser.NewToken("ge", @">=");
			var plus = Parser.NewToken("plus", @"\+");
			var minus = Parser.NewToken("minus", @"-");
			var multiply = Parser.NewToken("multiply", @"\*");
			var divide = Parser.NewToken("divide", @"/");

			id.CoverBy(@true, @false);

			// Syntax nodes

			var empty = Syntax.Empty();

			var value = Syntax.Multi<Value>("value");
			{
				var __ = value.NewSeqBranch(@uint);
				__.Builder = a => new Value(a.At(@uint));
			}
			{
				var __ = value.NewSeqBranch(udecimal);
				__.Builder = a => new Value(a.At(@udecimal));
			}
			{
				var __ = value.NewMultiBranch(@true, @false);
				__.Converter = o => new Value(o);
			}

			var asAlias = Syntax.Multi<string>("asAlias");
			{
				_ = asAlias.AddBranch(empty);
			}
			{
				_ = asAlias.AddBranch(id);
			}
			{
				var __ = asAlias.NewSeqBranch(@as, id);
				__.Builder = a => a.At(id);
			}

			var fieldName = Syntax.Multi<string>("fieldName");
			{
				var __ = fieldName.NewSeqBranch(star);
				__.Builder = a => "*";
			}
			{
				_ = fieldName.AddBranch(id);
			}

			var attr = Syntax.Multi<Attr>("attr");
			{
				var __ = attr.NewSeqBranch(fieldName);
				__.Builder = a => new Attr()
				{
					FieldName = a.At(fieldName),
				};
			}
			{
				var __ = attr.NewSeqBranch(id, dot, fieldName);
				__.Builder = a => new Attr()
				{
					RelationName = a.At(id),
					FieldName = a.At(fieldName),
				};
			}

			var compOper = Syntax.Multi<Operator.Comp>("compOper");
			{
				var __ = compOper.NewSeqBranch(eq);
				__.Builder = a => Operator.Comp.Eq;
			}
			{
				var __ = compOper.NewSeqBranch(ne);
				__.Builder = a => Operator.Comp.Ne;
			}
			{
				var __ = compOper.NewSeqBranch(lt);
				__.Builder = a => Operator.Comp.Lt;
			}
			{
				var __ = compOper.NewSeqBranch(le);
				__.Builder = a => Operator.Comp.Le;
			}
			{
				var __ = compOper.NewSeqBranch(gt);
				__.Builder = a => Operator.Comp.Gt;
			}
			{
				var __ = compOper.NewSeqBranch(ge);
				__.Builder = a => Operator.Comp.Ge;
			}

			var binaryArithOper = Syntax.Multi<Operator.Arith>("binaryArithOper");
			{
				var __ = binaryArithOper.NewSeqBranch(plus);
				__.Builder = a => Operator.Arith.Add;
			}
			{
				var __ = binaryArithOper.NewSeqBranch(minus);
				__.Builder = a => Operator.Arith.Subtract;
			}
			{
				var __ = binaryArithOper.NewSeqBranch(multiply);
				__.Builder = a => Operator.Arith.Multiply;
			}
			{
				var __ = binaryArithOper.NewSeqBranch(divide);
				__.Builder = a => Operator.Arith.Divide;
			}

			var unaryArithOper = Syntax.Multi<Operator.Arith>("unaryArithOper");
			{
				var __ = unaryArithOper.NewSeqBranch("negative", minus);
				__.Builder = a => Operator.Arith.Negative;
			}

			var binaryLogicalOper = Syntax.Multi<Operator.Logical>("binaryLogicalOper");
			{
				var __ = binaryLogicalOper.NewSeqBranch(and);
				__.Builder = a => Operator.Logical.And;
			}
			{
				var __ = binaryLogicalOper.NewSeqBranch(or);
				__.Builder = a => Operator.Logical.Or;
			}

			var unaryLogicalOper = Syntax.Multi<Operator.Logical>("unaryLogicalOper");
			{
				var __ = unaryLogicalOper.NewSeqBranch(not);
				__.Builder = a => Operator.Logical.Not;
			}

			var expr = Syntax.Multi<Expr>("expr");

			var expr4 = Syntax.Multi<Expr>("expr4");
			{
				var __ = expr4.NewSeqBranch("valueExpr", value);
				__.Builder = a => new ValueExpr(a.At(value));
			}
			{
				var __ = expr4.NewSeqBranch("attrExpr", attr);
				__.Builder = a => new AttrExpr(a.At(attr));
			}
			{
				var __ = expr4.NewSeqBranch("parensExpr", lParen, expr, rParen);
				__.Builder = a => new ParensExpr(a.At(expr));
			}

			var expr3 = Syntax.Multi<Expr>("expr3");
			{
				_ = expr3.AddBranch(expr4);
			}
			{
				var __ = expr3.NewSeqBranch("unaryArithExpr", unaryArithOper, expr4);
				__.Builder = a =>
				{
					return OperatorExpr<Operator.Arith>.Unary(a.At(unaryArithOper), a.At(expr4));
				};
			}

			var expr2 = Syntax.Multi<Expr>("expr2");
			{
				_ = expr2.AddBranch(expr3);
			}
			{
				var __ = expr2.NewSeqBranch("binaryArithExpr", expr3, binaryArithOper, expr2);
				__.Builder = a => a.At(expr2) is OperatorExpr<Operator.Arith> other && !other.IsUnary
					? OperatorExpr<Operator.Arith>.JoinRest(a.At(expr3), a.At(binaryArithOper), other)
					: OperatorExpr<Operator.Arith>.Binary(a.At(expr3), a.At(binaryArithOper), a.At(expr2));
			}

			var expr1 = Syntax.Multi<Expr>("expr1");
			{
				_ = expr1.AddBranch(expr2);
			}
			{
				var __ = expr1.NewSeqBranch("compExpr", expr2, compOper, expr1);
				__.Builder = a => OperatorExpr<Operator.Comp>.Binary(
					left: a.At(expr2),
					oper: a.At(compOper),
					right: a.At(expr1)
				);
			}

			var expr0 = Syntax.Multi<Expr>("expr0");
			{
				_ = expr0.AddBranch(expr1);
			}
			{
				var __ = expr0.NewSeqBranch("unaryLogicalExpr", unaryLogicalOper, expr1);
				__.Builder = a => OperatorExpr<Operator.Logical>.Unary(a.At(unaryLogicalOper), a.At(expr1));
			}

			_ = expr;
			{
				_ = expr.AddBranch(expr0);
			}
			{
				var __ = expr.NewSeqBranch("binaryLogicalExpr", expr0, binaryLogicalOper, expr);
				__.Builder = a => a.At(expr) is OperatorExpr<Operator.Logical> other && !other.IsUnary
					? OperatorExpr<Operator.Logical>.JoinRest(a.At(expr0), a.At(binaryLogicalOper), other)
					: OperatorExpr<Operator.Logical>.Binary(a.At(expr0), a.At(binaryLogicalOper), a.At(expr));
			}

			var selectExpr = Syntax.Multi<Expr>("selectExpr");
			{
				var __ = selectExpr.NewSeqBranch(expr, asAlias);
				__.Builder = a =>
				{
					var _expr = a.At(expr);
					if (_expr is not null) _expr.Alias = a.At(asAlias);
					return _expr;
				};
			}

			var selectExprs = Syntax.Sugar.List("selectExpr", selectExpr, comma);

			var whereClause = Syntax.Multi<Expr>("whereClause");
			{
				_ = whereClause.AddBranch(empty);
			}
			{
				var __ = whereClause.NewSeqBranch(where, expr);
				__.Builder = a => a.At(expr);
			}

			var relation = Syntax.Multi<Relation>("relation");
			{
				var __ = relation.NewSeqBranch(id, asAlias);
				__.Builder = a => new Relation()
				{
					Name = a.At(id),
					Alias = a.At(asAlias),
				};
			}

			var relations = Syntax.Sugar.List("relations", relation, comma);

			var fromClause = Syntax.Multi<IEnumerable<Relation>>("fromClause");
			{
				_ = fromClause.AddBranch(empty);
			}
			{
				var __ = fromClause.NewSeqBranch(from, relations);
				__.Builder = a => a.At(relations);
			}

			var selectStmt = Syntax.Multi<object?>("selectStmt");
			{
				var __ = selectStmt.NewSeqBranch(select, selectExprs, fromClause, whereClause);
				__.Builder = a => new SelectSqlNode()
				{
					Columns = a.At(selectExprs),
					Tables = a.At(fromClause),
					Condition = a.At(whereClause),
				};
			}

			var stmt = Syntax.Multi<object?>("stmt", selectStmt);

			parser.RootSyntaxNode = stmt;
		}

		public IEnumerable<object?> Parse(string str) => parser.Parse(str);
	}
}
