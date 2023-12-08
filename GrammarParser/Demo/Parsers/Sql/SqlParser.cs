namespace GrammarParser.Demo.Parsers.Sql
{
	public class SqlParser
	{
		readonly GrammarParser sqlParser = new();

		public SqlParser()
		{
			// Tokens (order by precedence desc)

			var blankToken = sqlParser.NewToken("blank", @"\s+", null, ignore: true);
			var selectToken = sqlParser.NewToken("select", @"SELECT");
			var fromToken = sqlParser.NewToken("from", @"FROM");
			var whereToken = sqlParser.NewToken("where", @"WHERE");
			var asToken = sqlParser.NewToken("as", @"AS");
			var idToken = sqlParser.NewToken("id", @"[A-Za-z_]+[A-Za-z0-9_]*", t => t.Value);
			var commaToken = sqlParser.NewToken("comma", @",");
			var starToken = sqlParser.NewToken("star", @"\*", t => t.Value);
			var dotToken = sqlParser.NewToken("dot", @"\.");
			var unsignedIntToken = sqlParser.NewToken("unsignedInt", @"\d+", t => int.Parse(t.Value));
			var unsignedFloatToken = sqlParser.NewToken("unsignedFloat", @"\d+\.\d+", t => int.Parse(t.Value));
			var eqToken = sqlParser.NewToken("eq", @"=");
			var plusToken = sqlParser.NewToken("plus", @"\+");
			var minusToken = sqlParser.NewToken("minus", @"-");

			// Grammar nodes

			var empty = new EmptyNode("ε");

			var value = new MultipleNode("value");
			{
				var __ = value.NewChild(new SequenceNode());
				__.SetChildren(unsignedIntToken);
				__.Builder = a => new Value(a[0].AsInt());
			}
			{
				var __ = value.NewChild(new SequenceNode());
				__.SetChildren(minusToken, unsignedIntToken);
				__.Builder = a => new Value(-a[1].AsInt());
			}
			{
				var __ = value.NewChild(new SequenceNode());
				__.SetChildren(unsignedFloatToken);
				__.Builder = a => new Value(a[0].AsFloat());
			}
			{
				var __ = value.NewChild(new SequenceNode());
				__.SetChildren(minusToken, unsignedFloatToken);
				__.Builder = a => new Value(-a[1].AsFloat());
			}

			var asAlias = new MultipleNode("asAlias");
			{
				var __ = asAlias.NewChild(empty);
			}
			{
				var __ = asAlias.NewChild(idToken);
			}
			{
				var __ = asAlias.NewChild(new SequenceNode());
				__.SetChildren(asToken, idToken);
				__.Builder = a => a[1].AsString();
			}

			var fieldName = new MultipleNode("fieldName");
			{
				var __ = fieldName.NewChild(starToken);
			}
			{
				var __ = fieldName.NewChild(idToken);
			}

			var attr = new MultipleNode("attr");
			{
				var __ = attr.NewChild(new SequenceNode());
				__.SetChildren(fieldName, asAlias);
				__.Builder = a => new Attribute()
				{
					FieldName = a[0].AsString(),
					Alias = a[1].AsString(),
				};
			}
			{
				var __ = attr.NewChild(new SequenceNode());
				__.SetChildren(idToken, dotToken, fieldName, asAlias);
				__.Builder = a => new Attribute()
				{
					RelationName = a[0].AsString(),
					FieldName = a[2].AsString(),
					Alias = a[3].AsString(),
				};
			}

			var arithOper = new MultipleNode("arithOper");
			{
				var __ = arithOper.NewChild(new SequenceNode());
				__.SetChildren(plusToken);
				__.Builder = a => Operator.Arith.Add;
			}
			{
				var __ = arithOper.NewChild(new SequenceNode());
				__.SetChildren(minusToken);
				__.Builder = a => Operator.Arith.Subtract;
			}

			var compOper = new MultipleNode("compOper");
			{
				var __ = compOper.NewChild(new SequenceNode());
				__.SetChildren(eqToken);
				__.Builder = a => Operator.Comp.Eq;
			}

			var expr3 = new MultipleNode("expr3");
			{
				var __ = expr3.NewChild(new SequenceNode("valueExpr"));
				__.SetChildren(value);
				__.Builder = a => new ValueExpr(a[0].As<Value?>());
			}
			{
				var __ = expr3.NewChild(new SequenceNode("attrExpr"));
				__.SetChildren(attr);
				__.Builder = a => new AttrExpr(a[0].As<Attribute?>());
			}

			var expr2 = new MultipleNode("expr2");
			{
				var __ = expr2.NewChild(expr3);
			}
			{
				var __ = expr2.NewChild(new SequenceNode("arithExpr"));
				__.SetChildren(expr3, arithOper, expr2);
				__.Builder = a =>
				{
					var _expr3 = a[0].As<Expression?>();
					var _arithOper = a[1].As<Operator.Arith?>();
					var _expr2 = a[2].As<Expression?>();
					return ArithExpr.Join(_arithOper, _expr3, _expr2);
				};
			}

			var expr1 = new MultipleNode("expr1");
			{
				var __ = expr1.NewChild(expr2);
			}
			{
				var __ = expr1.NewChild(new SequenceNode("compExpr"));
				__.SetChildren(expr2, compOper, expr1);
				__.Builder = a => new CompExpr
				(
					oper: a[1].As<Operator.Comp?>(),
					left: a[0].As<Expression?>(),
					right: a[2].As<Expression?>()
				);
			}

			var expression = new MultipleNode("expression");
			{
				var __ = expression.NewChild(expr1);
			}

			var restExpressions = new MultipleNode("restExpressions");
			{
				var __ = restExpressions.NewChild(empty);
			}
			{
				var __ = restExpressions.NewChild(new SequenceNode());
				__.SetChildren(commaToken, expression, restExpressions);
				__.Builder = a => a[1].JoinBefore<Expression?>(a[2]);
			}

			var where = new MultipleNode("where");
			{
				var __ = where.NewChild(empty);
			}
			{
				var __ = where.NewChild(new SequenceNode());
				__.SetChildren(whereToken, expression);
				__.Builder = a => a[1].As<Expression?>();
			}

			var relation = new SequenceNode("relation");
			relation.SetChildren(idToken, asAlias);
			relation.Builder = a =>
			{
				return new Relation()
				{
					Name = a[0].AsString(),
					Alias = a[1].AsString(),
				};
			};

			var restRelations = new MultipleNode("restRelations");
			{
				var __ = restRelations.NewChild(empty);
			}
			{
				var __ = restRelations.NewChild(new SequenceNode());
				__.SetChildren(commaToken, relation, restRelations);
				__.Builder = a => a[1].JoinBefore<Relation?>(restRelations);
			}

			var from = new MultipleNode("from");
			{
				var __ = from.NewChild(empty);
			}
			{
				var __ = from.NewChild(new SequenceNode());
				__.SetChildren(fromToken, relation, restRelations);
				__.Builder = a => a[1].JoinBefore<Relation?>(a[2]);
			}

			var selectStmt = new SequenceNode("selectStmt");
			selectStmt.SetChildren(selectToken, expression, restExpressions, from, where);
			selectStmt.Builder = a =>
			{
				var _from = a[3].AsEnumerable<Relation?>();
				var _where = a[4].As<Expression?>();
				var selectList = a[1].JoinBefore<Expression?>(a[2]);
				return new
				{
					selectList = $"[{string.Join(", ", selectList)}]",
					fromList = _from is null ? null : $"[{string.Join(", ", _from)}]",
					whereCondition = _where,
				};
			};

			var commandWrapper = new MultipleNode("commandWrapper");
			commandWrapper.SetChildren(selectStmt);

			sqlParser.RootGrammarRule = commandWrapper;
			sqlParser.IgnoreCase = true;
		}

		public IEnumerable<object?> Parse(string str) => sqlParser.Parse(str);
	}
}
