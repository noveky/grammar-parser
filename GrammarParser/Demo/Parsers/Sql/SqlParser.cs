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
			var minusToken = sqlParser.NewToken("minus", @"-");
			var eqToken = sqlParser.NewToken("eq", @"=");

			// Functions

			// TODO ConcatList

			// Grammar nodes

			var value = new MultipleNode("value");
			{
				var __0 = value.NewChild(new SequenceNode());
				{
					__0.SetChildren(unsignedIntToken);
					__0.Builder = a => new ValueSqlNode(a[0].AsInt());
				}
				var __1 = value.NewChild(new SequenceNode());
				{
					__1.SetChildren(minusToken, unsignedIntToken);
					__1.Builder = a => new ValueSqlNode(-a[1].AsInt());
				}
				var __2 = value.NewChild(new SequenceNode());
				{
					__2.SetChildren(unsignedFloatToken);
					__2.Builder = a => new ValueSqlNode(a[0].AsFloat());
				}
				var __3 = value.NewChild(new SequenceNode());
				{
					__3.SetChildren(minusToken, unsignedFloatToken);
					__3.Builder = a => new ValueSqlNode(-a[1].AsFloat());
				}
			}

			var asAlias = new MultipleNode("asAlias");
			{
				var __0 = asAlias.NewChild(new EmptyNode());
				var __1 = asAlias.NewChild(idToken);
				var __2 = asAlias.NewChild(new SequenceNode());
				{
					__2.SetChildren(asToken, idToken);
					__2.Builder = a => a[1].As<string>();
				}
			}

			var fieldName = new MultipleNode("fieldName");
			{
				var __0 = fieldName.NewChild(starToken);
				var __1 = fieldName.NewChild(idToken);
			}

			var attr = new MultipleNode("attr");
			{
				var __0 = attr.NewChild(new SequenceNode());
				{
					__0.SetChildren(fieldName, asAlias);
					__0.Builder = a => new AttributeSqlNode()
					{
						FieldName = a[0].As<string>(),
						Alias = a[1].As<string>(),
					};
				}
				var __1 = attr.NewChild(new SequenceNode());
				{
					__1.SetChildren(idToken, dotToken, fieldName, asAlias);
					__1.Builder = a => new AttributeSqlNode()
					{
						RelationName = a[0].AsString(),
						FieldName = a[2].AsString(),
						Alias = a[3].AsString(),
					};
				}
			}

			var attrList = new MultipleNode("attrList");
			{
				var __0 = attrList.NewChild(new EmptyNode());
				var __1 = attrList.NewChild(new SequenceNode());
				{
					__1.SetChildren(commaToken, attr, attrList);
					__1.Builder = a =>
					{
						IEnumerable<AttributeSqlNode?> __;
						var _attr = a[1].As<AttributeSqlNode>();
						var _attrList = a[2].As<IEnumerable<AttributeSqlNode?>>();
						__ = new[] { _attr };
						if (_attrList is not null) __ = __.Concat(_attrList);
						return __;
					};
				}
			}

			var compOperator = new MultipleNode("compOperator");
			{
				var __0 = compOperator.NewChild(new SequenceNode());
				{
					__0.SetChildren(eqToken);
					__0.Builder = a => CompOperator.Eq;
				}
			}

			var compOperand = new MultipleNode("compOperand");
			{
				var __0 = compOperand.NewChild(value);
				var __1 = compOperand.NewChild(attr);
			}

			var expression = new MultipleNode("expression");
			{
				var __0 = expression.NewChild(new SequenceNode());
				{
					__0.SetChildren(value);
					__0.Builder = a => new ValueExprSqlNode
					{
						Value = a[0].As<ValueSqlNode>(),
					};
				}
				var __1 = expression.NewChild(new SequenceNode());
				{
					__1.SetChildren(attr);
					__1.Builder = a => new AttrExprSqlNode
					{
						Attribute = a[0].As<AttributeSqlNode>(),
					};
				}
				var __2 = expression.NewChild(new SequenceNode());
				{
					__2.SetChildren(expression, compOperator, expression);
					__2.Builder = a => new CompExprSqlNode
					{
						Operator = a[1].AsValueType<CompOperator>(),
						Left = a[0].As<ExpressionSqlNode>(),
						Right = a[2].As<ExpressionSqlNode>(),
					};
				}
			}

			var where = new MultipleNode("where");
			{
				var __0 = where.NewChild(new EmptyNode());
				var __1 = where.NewChild(new SequenceNode());
				{
					__1.SetChildren(whereToken, expression);
					__1.Builder = a => a[1].As<ExpressionSqlNode>();
				}
			}

			var relation = new SequenceNode("relation");
			{
				relation.SetChildren(idToken, asAlias);
				relation.Builder = a =>
				{
					return new RelationSqlNode()
					{
						Name = a[0].AsString(),
						Alias = a[1].AsString(),
					};
				};
			}

			var relList = new MultipleNode("relList");
			{
				var __0 = relList.NewChild(new EmptyNode());
				var __1 = relList.NewChild(new SequenceNode());
				{
					__1.SetChildren(commaToken, relation, relList);
					__1.Builder = a =>
					{
						IEnumerable<RelationSqlNode?> __;
						var _relation = a[1].As<RelationSqlNode>();
						var _relList = a[2].As<IEnumerable<RelationSqlNode?>>();
						__ = new[] { _relation };
						if (_relList is not null) __ = __.Concat(_relList);
						return __;
					};
				}
			}

			var from = new MultipleNode("from");
			{
				var __0 = from.NewChild(new EmptyNode());
				var __1 = from.NewChild(new SequenceNode());
				{
					__1.SetChildren(fromToken, relation, relList);
					__1.Builder = a =>
					{
						IEnumerable<RelationSqlNode?> __;
						var _relation = a[1].As<RelationSqlNode>();
						var _relList = a[2].As<IEnumerable<RelationSqlNode?>>();
						__ = new[] { _relation };
						if (_relList is not null) __ = __.Concat(_relList);
						return __;
					};
				};
			}

			var selectStmt = new SequenceNode("selectStmt");
			selectStmt.SetChildren(selectToken, attr, attrList, from, where);
			selectStmt.Builder = a =>
			{
				IEnumerable<AttributeSqlNode?> _selectList;
				var _attr = a[1].As<AttributeSqlNode>();
				var _attrList = a[2].As<IEnumerable<AttributeSqlNode?>>();
				var _from = a[3].As<IEnumerable<RelationSqlNode>>();
				var _where = a[4].As<ExpressionSqlNode>();
				_selectList = new[] { _attr };
				if (_attrList is not null) _selectList = _selectList.Concat(_attrList);
				return new
				{
					selectList = $"[{string.Join(", ", _selectList)}]",
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
