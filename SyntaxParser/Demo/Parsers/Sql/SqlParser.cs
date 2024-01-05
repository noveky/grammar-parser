﻿namespace SyntaxParser.Demo.Parsers.Sql
{
	public class SqlParser
	{
		readonly Parser parser = new();

		public SqlParser() => parser
			.SetIgnoreCase() // Ignore case
			.SetSkipPattern(@"\s+") // Ignore whitespace
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
		public TokenNode<string> id = new();
		public TokenNode<string> @select = new();
		public TokenNode<string> @from = new();
		public TokenNode<string> @where = new();
		public TokenNode<string> @as = new();
		public TokenNode<string> and = new();
		public TokenNode<string> or = new();
		public TokenNode<string> not = new();
		public TokenNode<string> lParen = new();
		public TokenNode<string> rParen = new();
		public TokenNode<string> comma = new();
		public TokenNode<string> dot = new();
		public TokenNode<string> star = new();
		public TokenNode<string> eq = new();
		public TokenNode<string> ne = new();
		public TokenNode<string> lt = new();
		public TokenNode<string> le = new();
		public TokenNode<string> gt = new();
		public TokenNode<string> ge = new();
		public TokenNode<string> @in = new();
		public TokenNode<string> notIn = new();
		public TokenNode<string> exists = new();
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
		public MultiNode<SelectSqlNode> selectStmt = new();
		public MultiNode<object> root = new();

		public SqlSyntaxDef()
		{
			#region Tokens

			udecimal.SetRegex(@"\b\d+\.\d+\b").SetBuilder(t => decimal.Parse(t.Value));
			@uint.SetRegex(@"\b\d+\b").SetBuilder(t => uint.Parse(t.Value));
			@true.SetRegex(@"\bTRUE\b").SetBuilder(t => true);
			@false.SetRegex(@"\bFALSE\b").SetBuilder(t => false);
			@null.SetRegex(@"\bNULL\b").SetBuilder(t => null);
			id.SetRegex(@"\b[A-Za-z_]+[A-Za-z0-9_]*\b").SetBuilder(t => t.Value).SetCoverTypes(@true, @false, @null);
			@select.SetRegex(@"\bSELECT\b");
			@from.SetRegex(@"\bFROM\b");
			@where.SetRegex(@"\bWHERE\b");
			@as.SetRegex(@"\bAS\b");
			and.SetRegex(@"\bAND\b");
			or.SetRegex(@"\bOR\b");
			not.SetRegex(@"\bNOT\b");
			lParen.SetRegex(@"\(");
			rParen.SetRegex(@"\)");
			comma.SetRegex(@",");
			dot.SetRegex(@"\.");
			star.SetRegex(@"\*");
			eq.SetRegex(@"=");
			ne.SetRegex(@"<>");
			lt.SetRegex(@"<");
			le.SetRegex(@"<=");
			gt.SetRegex(@">");
			ge.SetRegex(@">=");
			exists.SetRegex(@"\bEXISTS\b");
			@in.SetRegex(@"\bIN\b");
			notIn.SetRegex(@"\bNOT IN\b");
			plus.SetRegex(@"\+");
			minus.SetRegex(@"-");
			multiply.SetRegex(@"\*");
			divide.SetRegex(@"/");

			#endregion

			#region Syntax tree

			// Root

			root.NewSeqBranch(Syntax.Converter<object>(selectStmt));

			// Statements

			selectStmt.NewSeqBranch(@select, selectColumns, fromClause, whereClause)
				.SetBuilder(s => new SelectSqlNode()
				{
					Columns = s.At(selectColumns),
					Tables = s.At(fromClause),
					Condition = s.At(whereClause),
				});

			// Components

			fromClause.NewSeqBranch(empty);
			fromClause.NewSeqBranch(@from, relations).SetBuilder(s => s.At(relations));

			whereClause.NewSeqBranch(empty);
			whereClause.NewSeqBranch(@where, expr).SetBuilder(s => s.At(expr));

			selectColumns.NewSeqBranch(Syntax.Sugar.List(selectColumn, comma));

			selectColumn.NewSeqBranch(expr, asAlias)
				.SetBuilder(s =>
				{
					var _expr = s.At(expr);
					_expr.Alias = s.At(asAlias);
					return _expr;
				});

			relations.NewSeqBranch(Syntax.Sugar.List(relation, comma));

			relation.NewSeqBranch(id, asAlias)
				.SetBuilder(s => new Relation()
				{
					Name = s.At(id),
					Alias = s.At(asAlias),
				});

			asAlias.NewSeqBranch(empty);
			asAlias.NewSeqBranch(id!);
			asAlias.NewSeqBranch(@as, id).SetBuilder(s => s.At(id));

			value.NewSeqBranch(Syntax.Converter<Value>(udecimal, @uint, @true, @false, @null)
				.SetBuilder(o => new Value(o)));

			fieldName.NewSeqBranch(star);
			fieldName.NewSeqBranch(id);

			attr.NewSeqBranch(fieldName)
				.SetBuilder(s => new Attr()
				{
					FieldName = s.At(fieldName),
				});
			attr.NewSeqBranch(id, dot, fieldName)
				.SetBuilder(s => new Attr()
				{
					RelationName = s.At(id),
					FieldName = s.At(fieldName),
				});

			// Expressions

			expr.NewSeqBranch(expr0);
			expr.NewSeqBranch(expr0, binaryLogicalOper, expr)
				.SetBuilder(s => s.At(expr) is OperatorExpr<Operator.Logical> other && !other.IsUnary
				? OperatorExpr<Operator.Logical>.JoinRest(s.At(expr0), s.At(binaryLogicalOper), other)
				: OperatorExpr<Operator.Logical>.Binary(s.At(expr0), s.At(binaryLogicalOper), s.At(expr)));

			expr0.NewSeqBranch(expr1);
			expr0.NewSeqBranch(unaryLogicalOper, expr1)
				.SetBuilder(s => OperatorExpr<Operator.Logical>.Unary(s.At(unaryLogicalOper), s.At(expr1)));

			expr1.NewSeqBranch(expr2);
			expr1.NewSeqBranch(unaryCompOper, expr2)
				.SetBuilder(s => OperatorExpr<Operator.Comp>.Unary(s.At(unaryCompOper), s.At(expr2)));
			expr1.NewSeqBranch(expr2, binaryCompOper, expr1)
				.SetBuilder(s => OperatorExpr<Operator.Comp>.Binary(
					left: s.At(expr2),
					oper: s.At(binaryCompOper),
					right: s.At(expr1)
				));

			expr2.NewSeqBranch(expr3);
			expr2.NewSeqBranch(expr3, binaryArithOper, expr2)
				.SetBuilder(s => s.At(expr2) is OperatorExpr<Operator.Arith> other && !other.IsUnary
				? OperatorExpr<Operator.Arith>.JoinRest(s.At(expr3), s.At(binaryArithOper), other)
				: OperatorExpr<Operator.Arith>.Binary(s.At(expr3), s.At(binaryArithOper), s.At(expr2)));

			expr3.NewSeqBranch(expr4);
			expr3.NewSeqBranch(unaryArithOper, expr4)
				.SetBuilder(s => OperatorExpr<Operator.Arith>.Unary(s.At(unaryArithOper), s.At(expr4)));

			expr4.NewSeqBranch(value).SetBuilder(s => new ValueExpr(s.At(value)));
			expr4.NewSeqBranch(attr).SetBuilder(s => new AttrExpr(s.At(attr)));
			expr4.NewSeqBranch(lParen, expr, rParen).SetBuilder(s => new ParensExpr(s.At(expr)));
			expr4.NewSeqBranch(lParen, selectStmt, rParen).SetBuilder(s => new SubqueryExpr(s.At(selectStmt)));

			// Operators

			binaryCompOper.NewSeqBranch(eq).SetBuilder(s => Operator.Comp.Eq);
			binaryCompOper.NewSeqBranch(ne).SetBuilder(s => Operator.Comp.Ne);
			binaryCompOper.NewSeqBranch(lt).SetBuilder(s => Operator.Comp.Lt);
			binaryCompOper.NewSeqBranch(le).SetBuilder(s => Operator.Comp.Le);
			binaryCompOper.NewSeqBranch(gt).SetBuilder(s => Operator.Comp.Gt);
			binaryCompOper.NewSeqBranch(ge).SetBuilder(s => Operator.Comp.Ge);
			binaryCompOper.NewSeqBranch(@in).SetBuilder(s => Operator.Comp.In);
			binaryCompOper.NewSeqBranch(@notIn).SetBuilder(s => Operator.Comp.NotIn);
			
			unaryCompOper.NewSeqBranch(@exists).SetBuilder(s => Operator.Comp.Exists);

			binaryArithOper.NewSeqBranch(plus).SetBuilder(s => Operator.Arith.Add);
			binaryArithOper.NewSeqBranch(minus).SetBuilder(s => Operator.Arith.Subtract);
			binaryArithOper.NewSeqBranch(multiply).SetBuilder(s => Operator.Arith.Multiply);
			binaryArithOper.NewSeqBranch(divide).SetBuilder(s => Operator.Arith.Divide);

			unaryArithOper.NewSeqBranch(minus).SetBuilder(s => Operator.Arith.Negative);

			binaryLogicalOper.NewSeqBranch(and).SetBuilder(s => Operator.Logical.And);
			binaryLogicalOper.NewSeqBranch(or).SetBuilder(s => Operator.Logical.Or);

			unaryLogicalOper.NewSeqBranch(not).SetBuilder(s => Operator.Logical.Not);

			#endregion
		}
	}
}
