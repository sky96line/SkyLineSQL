using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyLineSQL.Visitors
{
    public class ColumnCollector : TSqlFragmentVisitor
    {
        private readonly List<TableRef> _tables;
        private readonly Dictionary<string, TableRef> _aliasMap;
        private readonly Dictionary<(string, string, string), HashSet<string>> _metadata;

        public readonly Dictionary<string, HashSet<string>> Usage =
            new(StringComparer.OrdinalIgnoreCase);

        public ColumnCollector(
            List<TableRef> tables,
            Dictionary<string, TableRef> aliasMap,
            Dictionary<(string, string, string), HashSet<string>> metadata)
        {
            _tables = tables;
            _aliasMap = aliasMap;
            _metadata = metadata;

            foreach (var t in tables)
                Usage[t.FullName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        public override void ExplicitVisit(ColumnReferenceExpression node)
        {
            var ids = node.MultiPartIdentifier?.Identifiers;
            if (ids == null || ids.Count == 0)
                return;

            // Qualified: alias.column
            if (ids.Count >= 2)
            {
                var qualifier = ids[^2].Value;
                var column = ids[^1].Value;

                if (_aliasMap.TryGetValue(qualifier, out var table))
                    Usage[table.FullName].Add(column);
            }
            // Unqualified: semantic resolution
            else
            {
                ResolveUnqualified(ids[0].Value);
            }

            base.ExplicitVisit(node);
        }

        public override void ExplicitVisit(SelectSetVariable node)
        {
            ExtractColumns(node.Expression);
            base.ExplicitVisit(node);
        }

        void ExtractColumns(ScalarExpression expr)
        {
            if (expr == null)
                return;

            switch (expr)
            {
                case ColumnReferenceExpression col:
                    HandleColumn(col);
                    break;

                case BinaryExpression bin:
                    ExtractColumns(bin.FirstExpression);
                    ExtractColumns(bin.SecondExpression);
                    break;

                case FunctionCall func:
                    foreach (var p in func.Parameters)
                        ExtractColumns(p);
                    break;

                case CastCall cast:
                    ExtractColumns(cast.Parameter);
                    break;

                case SearchedCaseExpression searched:
                    foreach (var w in searched.WhenClauses)
                    {
                        ExtractColumns(w.WhenExpression);
                        ExtractColumns(w.ThenExpression);
                    }
                    ExtractColumns(searched.ElseExpression);
                    break;

                case SimpleCaseExpression simple:
                    ExtractColumns(simple.InputExpression);
                    foreach (var w in simple.WhenClauses)
                    {
                        ExtractColumns(w.WhenExpression);
                        ExtractColumns(w.ThenExpression);
                    }
                    ExtractColumns(simple.ElseExpression);
                    break;
            }
        }

        void ExtractColumns(BooleanExpression expr)
        {
            if (expr == null)
                return;

            switch (expr)
            {
                case BooleanComparisonExpression cmp:
                    ExtractColumns(cmp.FirstExpression);
                    ExtractColumns(cmp.SecondExpression);
                    break;

                case BooleanBinaryExpression bin:
                    ExtractColumns(bin.FirstExpression);
                    ExtractColumns(bin.SecondExpression);
                    break;

                case BooleanNotExpression not:
                    ExtractColumns(not.Expression);
                    break;

                case ExistsPredicate exists:
                    // Subquery case (advanced, optional)
                    break;

                case InPredicate inp:
                    ExtractColumns(inp.Expression);
                    foreach (var v in inp.Values)
                        ExtractColumns(v);
                    break;

                case LikePredicate like:
                    ExtractColumns(like.FirstExpression);
                    ExtractColumns(like.SecondExpression);
                    break;
            }
        }

        void HandleColumn(ColumnReferenceExpression node)
        {
            var ids = node.MultiPartIdentifier?.Identifiers;
            if (ids == null || ids.Count == 0)
                return;

            if (ids.Count >= 2)
            {
                var qualifier = ids[^2].Value;
                var column = ids[^1].Value;

                if (_aliasMap.TryGetValue(qualifier, out var table))
                    Usage[table.FullName].Add(column);
            }
            else
            {
                ResolveUnqualified(ids[0].Value);
            }
        }



        void ResolveUnqualified(string column)
        {
            var matches = _tables
                .Where(t => _metadata.TryGetValue((t.Database, t.Schema, t.Table), out var cols)
                            && cols.Contains(column))
                .ToList();

            if (matches.Count == 1)
                Usage[matches[0].FullName].Add(column);
        }
    }
}
