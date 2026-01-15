using DbMetaTool.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DbMetaTool.Scripts
{
    public class SqlDefinitionParser
    {
        private static readonly HashSet<string> KnownDataTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "INTEGER", "INT", "BIGINT", "SMALLINT", "INT128",
            "FLOAT", "DOUBLE", "REAL", "DECIMAL", "NUMERIC", "DECFLOAT",
            "VARCHAR", "CHAR", "CHARACTER", "BINARY", "VARBINARY",
            "DATE", "TIME", "TIMESTAMP",
            "BLOB", "BOOLEAN"
        };

        public Domain ParseDomainScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                throw new ArgumentException("Skrypt domeny nie może być pusty.", nameof(script));
            }

            var normalizedScript = RemoveComments(script);

            var pattern = @"CREATE\s+DOMAIN\s+(\w+)\s+AS\s+(\w+)(?:\s*\(([^)]+)\))?(.*)";
            var match = Regex.Match(normalizedScript, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!match.Success)
            {
                throw new InvalidOperationException($"Nie można sparsować skryptu domeny: {script}");
            }

            var domain = new Domain
            {
                Name = match.Groups[1].Value.Trim().ToUpperInvariant()
            };

            var baseType = match.Groups[2].Value.Trim().ToUpperInvariant();
            var typeParams = match.Groups[3].Value.Trim();
            var remainder = match.Groups[4].Value;

            domain.DataType = BuildDataType(baseType, typeParams);
            ParseTypeParameters(typeParams, domain);

            domain.IsNullable = !Regex.IsMatch(remainder, @"\bNOT\s+NULL\b", RegexOptions.IgnoreCase);

            var defaultMatch = Regex.Match(remainder, @"\bDEFAULT\s+(.+?)(?:NOT\s+NULL|;|$)", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (defaultMatch.Success)
            {
                domain.DefaultValue = defaultMatch.Groups[1].Value.Trim().TrimEnd(';', ' ');
            }

            return domain;
        }

        public Table ParseTableScript(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                throw new ArgumentException("Skrypt tabeli nie może być pusty.", nameof(script));
            }

            var normalizedScript = RemoveComments(script);

            var tableNameMatch = Regex.Match(normalizedScript, @"CREATE\s+TABLE\s+(\w+)", RegexOptions.IgnoreCase);
            if (!tableNameMatch.Success)
            {
                throw new InvalidOperationException($"Nie można sparsować nazwy tabeli: {script}");
            }

            var table = new Table
            {
                Name = tableNameMatch.Groups[1].Value.Trim().ToUpperInvariant(),
                Columns = new List<Column>()
            };

            var columnsMatch = Regex.Match(normalizedScript, @"CREATE\s+TABLE\s+\w+\s*\(\s*(.*)\s*\)", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            if (!columnsMatch.Success)
            {
                throw new InvalidOperationException($"Nie można sparsować kolumn tabeli: {script}");
            }

            var columnsText = columnsMatch.Groups[1].Value;
            var columnDefinitions = SplitColumnDefinitions(columnsText);

            int position = 0;
            foreach (var colDef in columnDefinitions)
            {
                var trimmed = colDef.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                var column = ParseColumnDefinition(trimmed, position);
                if (column != null)
                {
                    table.Columns.Add(column);
                    position++;
                }
            }

            return table;
        }

        private Column? ParseColumnDefinition(string definition, int position)
        {
            if (Regex.IsMatch(definition, @"^\s*(PRIMARY\s+KEY|FOREIGN\s+KEY|CONSTRAINT|CHECK|UNIQUE)\b", RegexOptions.IgnoreCase))
            {
                return null;
            }

            var pattern = @"^(\w+)\s+(\w+)(?:\s*\(([^)]+)\))?(.*)$";
            var match = Regex.Match(definition, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            if (!match.Success)
            {
                return null;

            }

            var column = new Column
            {
                Name = match.Groups[1].Value.Trim().ToUpperInvariant(),
                Position = position
            };

            var typeOrDomain = match.Groups[2].Value.Trim().ToUpperInvariant();
            var typeParams = match.Groups[3].Value.Trim();
            var remainder = match.Groups[4].Value;

            if (IsKnownDataType(typeOrDomain))
            {
                column.DataType = BuildDataType(typeOrDomain, typeParams);
                column.DomainName = null;
            }
            else
            {
                column.DomainName = typeOrDomain;
                column.DataType = string.Empty;
            }

            column.IsNullable = !Regex.IsMatch(remainder, @"\bNOT\s+NULL\b", RegexOptions.IgnoreCase);

            var defaultMatch = Regex.Match(remainder, @"\bDEFAULT\s+(\S+)", RegexOptions.IgnoreCase);
            if (defaultMatch.Success)
            {
                column.DefaultValue = defaultMatch.Groups[1].Value.Trim().TrimEnd(',', ';');
            }

            return column;
        }

        private List<string> SplitColumnDefinitions(string columnsText)
        {
            var result = new List<string>();
            var current = new StringBuilder();
            int parenDepth = 0;

            foreach (char c in columnsText)
            {
                if (c == '(')
                {
                    parenDepth++;
                    current.Append(c);
                }
                else if (c == ')')
                {
                    parenDepth--;
                    current.Append(c);
                }
                else if (c == ',' && parenDepth == 0)
                {
                    result.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                result.Add(current.ToString());
            }

            return result;
        }

        private bool IsKnownDataType(string typeName)
        {
            return KnownDataTypes.Contains(typeName);
        }

        private string BuildDataType(string baseType, string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                return baseType;
            }

            return $"{baseType}({parameters})";
        }

        private void ParseTypeParameters(string parameters, Domain domain)
        {
            if (string.IsNullOrWhiteSpace(parameters))
            {
                return;
            }

            var parts = parameters.Split(',');
            if (parts.Length >= 1 && int.TryParse(parts[0].Trim(), out int firstParam))
            {
                domain.Length = firstParam;
                domain.Precision = firstParam;
            }
            if (parts.Length >= 2 && int.TryParse(parts[1].Trim(), out int secondParam))
            {
                domain.Scale = secondParam;
            }
        }

        private string RemoveComments(string sql)
        {
            sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return sql.Trim();
        }
    }
}
