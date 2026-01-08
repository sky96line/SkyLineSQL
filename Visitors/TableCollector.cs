using Microsoft.SqlServer.TransactSql.ScriptDom;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkyLineSQL.Visitors
{
    public class TableCollector : TSqlFragmentVisitor
    {
        public readonly List<TableRef> Tables = new();
        public readonly Dictionary<string, TableRef> AliasMap =
            new(StringComparer.OrdinalIgnoreCase);

        public override void ExplicitVisit(NamedTableReference node)
        {
            var ids = node.SchemaObject.Identifiers;
            if (ids.Count >= 3)
            {
                var table = new TableRef(
                    ids[^3].Value,
                    ids[^2].Value,
                    ids[^1].Value,
                    string.Join(".", ids.Select(i => i.Value))
                );

                Tables.Add(table);

                if (node.Alias != null)
                    AliasMap[node.Alias.Value] = table;
                else
                    AliasMap[table.Table] = table;
            }

            base.ExplicitVisit(node);
        }
    }
}
