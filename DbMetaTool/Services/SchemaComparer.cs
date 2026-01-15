using DbMetaTool.Models;
using DbMetaTool.Models.Changes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbMetaTool.Services
{
    public class SchemaComparer
    {
        public DomainChanges CompareDomains(List<Domain> existing, List<Domain> desired)
        {
            var changes = new DomainChanges();
            var existingByName = existing.ToDictionary(d => d.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var desiredDomain in desired)
            {
                if (!existingByName.TryGetValue(desiredDomain.Name, out var existingDomain))
                {
                    changes.DomainsToCreate.Add(desiredDomain);
                }
                else
                {
                    var alterStatements = GenerateDomainAlterStatements(existingDomain, desiredDomain);
                    if (alterStatements.Count > 0)
                    {
                        changes.DomainsToAlter.Add((desiredDomain.Name, alterStatements));
                    }
                }
            }

            return changes;
        }

        public TableChanges CompareTables(List<Table> existing, List<Table> desired)
        {
            var changes = new TableChanges();
            var existingByName = existing.ToDictionary(t => t.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var desiredTable in desired)
            {
                if (!existingByName.TryGetValue(desiredTable.Name, out var existingTable))
                {
                    changes.TablesToCreate.Add(desiredTable);
                }
                else
                {
                    var columnChanges = CompareColumns(existingTable, desiredTable);
                    if (columnChanges.HasChanges)
                    {
                        changes.TablesToAlter.Add((desiredTable.Name, columnChanges));
                    }
                }
            }

            return changes;
        }

        private List<string> GenerateDomainAlterStatements(Domain existing, Domain desired)
        {
            var statements = new List<string>();
            var domainName = existing.Name;

            if (!string.Equals(existing.DataType, desired.DataType, StringComparison.OrdinalIgnoreCase))
            {
                statements.Add($"ALTER DOMAIN {domainName} TYPE {desired.DataType}");
            }

            if (existing.IsNullable != desired.IsNullable)
            {
                if (desired.IsNullable)
                {

                    statements.Add($"ALTER DOMAIN {domainName} DROP NOT NULL");
                }
                else
                {
                    statements.Add($"ALTER DOMAIN {domainName} SET NOT NULL");
                }
            }

            var existingDefault = NormalizeDefault(existing.DefaultValue);
            var desiredDefault = NormalizeDefault(desired.DefaultValue);

            if (!string.Equals(existingDefault, desiredDefault, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(desiredDefault))
                {
                    statements.Add($"ALTER DOMAIN {domainName} DROP DEFAULT");
                }
                else
                {
                    statements.Add($"ALTER DOMAIN {domainName} SET DEFAULT {desired.DefaultValue}");
                }
            }

            return statements;
        }

        private ColumnChanges CompareColumns(Table existing, Table desired)
        {
            var changes = new ColumnChanges();
            var existingByName = existing.Columns.ToDictionary(c => c.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var desiredColumn in desired.Columns)
            {
                if (!existingByName.TryGetValue(desiredColumn.Name, out var existingColumn))
                {
                    changes.ColumnsToAdd.Add(desiredColumn);
                }
                else
                {
                    var alterStatements = GenerateColumnAlterStatements(existing.Name, existingColumn, desiredColumn);
                    if (alterStatements.Count > 0)
                    {
                        changes.ColumnsToAlter.Add((desiredColumn.Name, alterStatements));
                    }
                }
            }

            return changes;
        }

        private List<string> GenerateColumnAlterStatements(string tableName, Column existing, Column desired)
        {
            var statements = new List<string>();

            if (string.IsNullOrEmpty(desired.DomainName) &&
                string.IsNullOrEmpty(existing.DomainName))
            {
                if (!string.Equals(existing.DataType, desired.DataType, StringComparison.OrdinalIgnoreCase))
                {
                    statements.Add($"ALTER TABLE {tableName} ALTER COLUMN {desired.Name} TYPE {desired.DataType}");
                }
            }

            if (existing.IsNullable != desired.IsNullable)
            {
                if (desired.IsNullable)
                {
                    statements.Add($"ALTER TABLE {tableName} ALTER COLUMN {desired.Name} DROP NOT NULL");
                }
                else
                {
                    statements.Add($"ALTER TABLE {tableName} ALTER COLUMN {desired.Name} SET NOT NULL");
                }
            }

            var existingDefault = NormalizeDefault(existing.DefaultValue);
            var desiredDefault = NormalizeDefault(desired.DefaultValue);

            if (!string.Equals(existingDefault, desiredDefault, StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(desiredDefault))
                {
                    statements.Add($"ALTER TABLE {tableName} ALTER COLUMN {desired.Name} DROP DEFAULT");
                }
                else
                {
                    statements.Add($"ALTER TABLE {tableName} ALTER COLUMN {desired.Name} SET DEFAULT {desired.DefaultValue}");
                }
            }

            return statements;
        }

        private string NormalizeDefault(string? defaultValue)
        {
            if (string.IsNullOrWhiteSpace(defaultValue))
            {
                return string.Empty;
            }

            return defaultValue.Trim().ToUpperInvariant();
        }
    }
}
