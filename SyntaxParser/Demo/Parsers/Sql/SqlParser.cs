namespace SyntaxParser.Demo.Parsers.Sql
{
	public class SqlParser
	{
		readonly Parser parser = new();

		public SqlParser() => parser
			.SetIgnoreCase(true) // Ignore case
			.SetSkipPattern(@"\s+") // Ignore whitespace
			.SetTokenizationType(TokenizationType.Static) // Static tokenization
			.SetSyntaxDef(new SqlSyntaxDef());

		public IEnumerable<object?> Parse(string str) => parser.Parse(str);
	}

	public class SqlSyntaxDef : ISyntaxDef
	{
		public INode RootNode => root;

		// Tokens
		public TokenNode<decimal> udecimal = new();
		public TokenNode<uint> @uint = new();
		public TokenNode<bool> @true = new();
		public TokenNode<bool> @false = new();
		public TokenNode<object?> @null = new();
		public TokenNode<string> @select = new();
		public TokenNode<string> @from = new();
		public TokenNode<string> @where = new();
		public TokenNode<string> @as = new();
		public TokenNode<string> and = new();
		public TokenNode<string> or = new();
		public TokenNode<string> not = new();
		public TokenNode<string> @in = new();
		public TokenNode<string> notIn = new();
		public TokenNode<string> exists = new();
		public TokenNode<string> id = new();
		public TokenNode<string> lParen = new();
		public TokenNode<string> rParen = new();
		public TokenNode<string> comma = new();
		public TokenNode<string> dot = new();
		public TokenNode<string> star = new();
		public TokenNode<string> eq = new();
		public TokenNode<string> ne = new();
		public TokenNode<string> le = new();
		public TokenNode<string> lt = new();
		public TokenNode<string> ge = new();
		public TokenNode<string> gt = new();
		public TokenNode<string> plus = new();
		public TokenNode<string> minus = new();
		public TokenNode<string> multiply = new();
		public TokenNode<string> divide = new();

		// Syntax tree
		public EmptyNode empty = new();
		public MultiNode<Value> value = new();
		public MultiNode<string?> asAlias = new();
		public MultiNode<string> fieldName = new();
		public MultiNode<Attr> attr = new();
		public MultiNode<Operator.Comp> binaryCompOper = new();
		public MultiNode<Operator.Comp> unaryCompOper = new();
		public MultiNode<Operator.Arith> binaryArithOper = new();
		public MultiNode<Operator.Arith> unaryArithOper = new();
		public MultiNode<Operator.Logical> binaryLogicalOper = new();
		public MultiNode<Operator.Logical> unaryLogicalOper = new();
		public MultiNode<Expr> expr = new();
		public MultiNode<Expr> expr0 = new();
		public MultiNode<Expr> expr1 = new();
		public MultiNode<Expr> expr2 = new();
		public MultiNode<Expr> expr3 = new();
		public MultiNode<Expr> expr4 = new();
		public MultiNode<Expr> selectColumn = new();
		public MultiNode<IEnumerable<Expr>> selectColumns = new();
		public MultiNode<Expr?> whereClause = new();
		public MultiNode<Relation> relation = new();
		public MultiNode<IEnumerable<Relation>> relations = new();
		public MultiNode<IEnumerable<Relation>?> fromClause = new();
		public MultiNode<SelectStmt> selectStmt = new();
		public MultiNode<object> root = new();

		public SqlSyntaxDef()
		{
			#region Tokens

			udecimal.SetRegex(@"\b\d+\.\d+\b").SetBuilder(t => decimal.Parse(t.Value));
			@uint.SetRegex(@"\b\d+\b").SetBuilder(t => uint.Parse(t.Value));
			@true.SetRegex(@"\bTRUE\b").SetBuilder(t => true);
			@false.SetRegex(@"\bFALSE\b").SetBuilder(t => false);
			@null.SetRegex(@"\bNULL\b").SetBuilder(t => null);
			@select.SetRegex(@"\bSELECT\b");
			@from.SetRegex(@"\bFROM\b");
			@where.SetRegex(@"\bWHERE\b");
			@as.SetRegex(@"\bAS\b");
			and.SetRegex(@"\bAND\b");
			or.SetRegex(@"\bOR\b");
			not.SetRegex(@"\bNOT\b");
			exists.SetRegex(@"\bEXISTS\b");
			@in.SetRegex(@"\bIN\b");
			notIn.SetRegex(@"\bNOT IN\b");
			id.SetRegex(@"\b[A-Za-z_]+[A-Za-z0-9_]*\b").SetBuilder(t => t.Value).SetCoverTypes(@true, @false, @null);
			lParen.SetRegex(@"\(");
			rParen.SetRegex(@"\)");
			comma.SetRegex(@",");
			dot.SetRegex(@"\.");
			star.SetRegex(@"\*");
			eq.SetRegex(@"=");
			ne.SetRegex(@"<>");
			le.SetRegex(@"<=");
			lt.SetRegex(@"<");
			ge.SetRegex(@">=");
			gt.SetRegex(@">");
			plus.SetRegex(@"\+");
			minus.SetRegex(@"-");
			multiply.SetTokenType(star.TokenType);
			divide.SetRegex(@"/");

			#endregion

			#region Syntax tree

			// Root

			root.AddBranch(Syntax.Converter<object>(selectStmt));

			// Statements

			selectStmt.NewSeqBranch(@select, selectColumns, fromClause, whereClause)
				.SetBuilder(m => new SelectStmt()
				{
					Columns = m.At(selectColumns),
					Tables = m.At(fromClause),
					Condition = m.At(whereClause),
				});

			// Components

			fromClause.NewSeqBranch(empty).SetBuilder(m => null);
			fromClause.NewSeqBranch(@from, relations).SetBuilder(m => m.At(relations));

			whereClause.NewSeqBranch(empty).SetBuilder(m => null);
			whereClause.NewSeqBranch(@where, expr).SetBuilder(m => m.At(expr));

			selectColumns.AddBranch(Syntax.Sugar.List(selectColumn, comma));

			selectColumn.NewSeqBranch(expr, asAlias)
				.SetBuilder(m =>
				{
					var _expr = m.At(expr);
					_expr.Alias = m.At(asAlias);
					return _expr;
				});

			relations.AddBranch(Syntax.Sugar.List(relation, comma));

			relation.NewSeqBranch(id, asAlias)
				.SetBuilder(m => new Relation()
				{
					Name = m.At(id),
					Alias = m.At(asAlias),
				});

			asAlias.NewSeqBranch(empty).SetBuilder(m => null);
			asAlias.AddBranch(id!);
			asAlias.NewSeqBranch(@as, id).SetBuilder(m => m.At(id));

			value.AddBranch(
				Syntax.Converter<Value>(udecimal, @uint, @true, @false, @null)
					.SetBuilder(o => new Value(o)));

			fieldName.AddBranch(star);
			fieldName.AddBranch(id);

			attr.NewSeqBranch(fieldName)
				.SetBuilder(m => new Attr()
				{
					FieldName = m.At(fieldName),
				});
			attr.NewSeqBranch(id, dot, fieldName)
				.SetBuilder(m => new Attr()
				{
					RelationName = m.At(id),
					FieldName = m.At(fieldName),
				});

			// Expressions

			expr.AddBranch(expr0);
			expr.NewSeqBranch(expr0, binaryLogicalOper, expr)
				.SetBuilder(m => m.At(expr) is OperatorExpr<Operator.Logical> other && !other.IsUnary
				? OperatorExpr<Operator.Logical>.JoinRest(m.At(expr0), m.At(binaryLogicalOper), other)
				: OperatorExpr<Operator.Logical>.Binary(m.At(expr0), m.At(binaryLogicalOper), m.At(expr)));

			expr0.AddBranch(expr1);
			expr0.NewSeqBranch(unaryLogicalOper, expr1)
				.SetBuilder(m => OperatorExpr<Operator.Logical>.Unary(m.At(unaryLogicalOper), m.At(expr1)));

			expr1.AddBranch(expr2);
			expr1.NewSeqBranch(unaryCompOper, expr2)
				.SetBuilder(m => OperatorExpr<Operator.Comp>.Unary(m.At(unaryCompOper), m.At(expr2)));
			expr1.NewSeqBranch(expr2, binaryCompOper, expr1)
				.SetBuilder(m => OperatorExpr<Operator.Comp>.Binary(m.At(expr2), m.At(binaryCompOper), m.At(expr1)));

			expr2.AddBranch(expr3);
			expr2.NewSeqBranch(expr3, binaryArithOper, expr2)
				.SetBuilder(m => m.At(expr2) is OperatorExpr<Operator.Arith> other && !other.IsUnary
				? OperatorExpr<Operator.Arith>.JoinRest(m.At(expr3), m.At(binaryArithOper), other)
				: OperatorExpr<Operator.Arith>.Binary(m.At(expr3), m.At(binaryArithOper), m.At(expr2)));

			expr3.AddBranch(expr4);
			expr3.NewSeqBranch(unaryArithOper, expr4)
				.SetBuilder(m => OperatorExpr<Operator.Arith>.Unary(m.At(unaryArithOper), m.At(expr4)));

			expr4.NewSeqBranch(value).SetBuilder(m => new ValueExpr(m.At(value)));
			expr4.NewSeqBranch(attr).SetBuilder(m => new AttrExpr(m.At(attr)));
			expr4.NewSeqBranch(lParen, expr, rParen).SetBuilder(m => new ParensExpr(m.At(expr)));
			expr4.NewSeqBranch(lParen, selectStmt, rParen).SetBuilder(m => new SubqueryExpr(m.At(selectStmt)));

			// Operators

			binaryCompOper.NewSeqBranch(eq).SetBuilder(m => Operator.Comp.Eq);
			binaryCompOper.NewSeqBranch(ne).SetBuilder(m => Operator.Comp.Ne);
			binaryCompOper.NewSeqBranch(lt).SetBuilder(m => Operator.Comp.Lt);
			binaryCompOper.NewSeqBranch(le).SetBuilder(m => Operator.Comp.Le);
			binaryCompOper.NewSeqBranch(gt).SetBuilder(m => Operator.Comp.Gt);
			binaryCompOper.NewSeqBranch(ge).SetBuilder(m => Operator.Comp.Ge);
			binaryCompOper.NewSeqBranch(@in).SetBuilder(m => Operator.Comp.In);
			binaryCompOper.NewSeqBranch(@notIn).SetBuilder(m => Operator.Comp.NotIn);

			unaryCompOper.NewSeqBranch(@exists).SetBuilder(m => Operator.Comp.Exists);

			binaryArithOper.NewSeqBranch(plus).SetBuilder(m => Operator.Arith.Add);
			binaryArithOper.NewSeqBranch(minus).SetBuilder(m => Operator.Arith.Subtract);
			binaryArithOper.NewSeqBranch(multiply).SetBuilder(m => Operator.Arith.Multiply);
			binaryArithOper.NewSeqBranch(divide).SetBuilder(m => Operator.Arith.Divide);

			unaryArithOper.NewSeqBranch(minus).SetBuilder(m => Operator.Arith.Negative);

			binaryLogicalOper.NewSeqBranch(and).SetBuilder(m => Operator.Logical.And);
			binaryLogicalOper.NewSeqBranch(or).SetBuilder(m => Operator.Logical.Or);

			unaryLogicalOper.NewSeqBranch(not).SetBuilder(m => Operator.Logical.Not);

			#endregion
		}
	}
}
