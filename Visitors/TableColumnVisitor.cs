using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyLineSQL.Visitors
{
    public class TableColumnVisitorV2 : TSqlFragmentVisitor
    {
        private readonly string _targetTable;
        private readonly HashSet<string> _aliases = new(StringComparer.OrdinalIgnoreCase);
        public readonly HashSet<string> Columns = new(StringComparer.OrdinalIgnoreCase);

        public TableColumnVisitorV2(string targetTable)
        {
            _targetTable = targetTable;
        }

        static string GetFullName(SchemaObjectName schema)
        {
            return string.Join(".",
                schema.Identifiers.Select(i => i.Value));
        }

        public override void Visit(NamedTableReference node)
        {
            var fullName = GetFullName(node.SchemaObject);

            if (fullName.Equals(_targetTable, StringComparison.OrdinalIgnoreCase)
                || node.SchemaObject.BaseIdentifier.Value
                       .Equals(_targetTable, StringComparison.OrdinalIgnoreCase))
            {
                // Table itself
                _aliases.Add(node.SchemaObject.BaseIdentifier.Value);

                // Alias if present
                if (node.Alias != null)
                    _aliases.Add(node.Alias.Value);
            }

            base.Visit(node);
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.MultiPartIdentifier == null)
                return;

            var ids = node.MultiPartIdentifier.Identifiers;

            if (ids.Count >= 2)
            {
                var qualifier = ids[^2].Value; // table or alias
                var column = ids[^1].Value;

                if (_aliases.Contains(qualifier))
                    Columns.Add(column);
            }

            base.Visit(node);
        }
    }

}
