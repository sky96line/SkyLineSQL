using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyLineSQL.Visitors
{
    public class ExternalDbUsageVisitor : TSqlFragmentVisitor
    {
        private readonly string _databaseName;

        // alias -> full table name
        private readonly Dictionary<string, string> _aliasMap =
            new(StringComparer.OrdinalIgnoreCase);

        // table -> columns
        public readonly Dictionary<string, HashSet<string>> Usage =
            new(StringComparer.OrdinalIgnoreCase);

        public ExternalDbUsageVisitor(string databaseName)
        {
            _databaseName = databaseName;
        }

        static string GetFullName(SchemaObjectName schema)
        {
            return string.Join(".",
                schema.Identifiers.Select(i => i.Value));
        }

        public override void Visit(NamedTableReference node)
        {
            var identifiers = node.SchemaObject.Identifiers;

            // Must be at least: db.schema.table
            if (identifiers.Count >= 3 &&
                identifiers[0].Value.Equals(_databaseName, StringComparison.OrdinalIgnoreCase))
            {
                var fullTableName = GetFullName(node.SchemaObject);

                // Register table
                if (!Usage.ContainsKey(fullTableName))
                    Usage[fullTableName] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Register alias
                if (node.Alias != null)
                {
                    _aliasMap[node.Alias.Value] = fullTableName;
                }
                else
                {
                    // No alias → table name acts as its own qualifier
                    _aliasMap[identifiers[^1].Value] = fullTableName;
                }
            }

            base.Visit(node);
        }

        public override void Visit(ColumnReferenceExpression node)
        {
            if (node.MultiPartIdentifier == null)
                return;

            var ids = node.MultiPartIdentifier.Identifiers;

            // alias.column
            if (ids.Count >= 2)
            {
                var qualifier = ids[^2].Value;
                var column = ids[^1].Value;

                if (_aliasMap.TryGetValue(qualifier, out var table))
                {
                    Usage[table].Add(column);
                }
            }

            base.Visit(node);
        }
    }
}
