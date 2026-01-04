using DbMetaTool.Database;
using DbMetaTool.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DbMetaTool.Scripts
{
    public class SqlScriptGenerator : IScriptGenerator
    {
        public string GenerateDomainScript(Domain domain)
        {
            ArgumentNullException.ThrowIfNull(domain);

            var sb = new StringBuilder();

            sb.Append($"CREATE DOMAIN {domain.Name} AS {domain.DataType}");

            if (!domain.IsNullable)
            {
                sb.Append(" NOT NULL");
            }

            if (!string.IsNullOrWhiteSpace(domain.DefaultValue))
            {
                sb.Append($" {domain.DefaultValue}");
            }

            sb.Append(';');

            return sb.ToString();
        }

        public string GenerateTableScript(Table table)
        {
            ArgumentNullException.ThrowIfNull(table);

            if (table.Columns == null || table.Columns.Count == 0)
            {
                throw new InvalidOperationException($"Tabela {table.Name} nie ma kolumn.");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"CREATE TABLE {table.Name} (");

            var columnDefinitions = new List<string>();

            foreach (var column in table.Columns.OrderBy(c => c.Position))
            {
                var columnDef = new StringBuilder();
                columnDef.Append($"    {column.Name} ");

                if (!string.IsNullOrWhiteSpace(column.DomainName))
                {
                    columnDef.Append(column.DomainName);
                }
                else
                {
                    columnDef.Append(column.DataType);
                }

                if (!column.IsNullable)
                {
                    columnDef.Append(" NOT NULL");
                }

                if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                {
                    columnDef.Append($" {column.DefaultValue}");
                }

                columnDefinitions.Add(columnDef.ToString());
            }

            sb.AppendLine(string.Join($",{Environment.NewLine}", columnDefinitions));
            sb.Append(");");

            return sb.ToString();
        }

        public string GenerateProcedureScript(Procedure procedure)
        {
            ArgumentNullException.ThrowIfNull(procedure);

            if (string.IsNullOrWhiteSpace(procedure.SourceCode))
            {
                throw new InvalidOperationException($"Procedure {procedure.Name} has no source code.");
            }

            var sb = new StringBuilder();
            sb.AppendLine($"CREATE PROCEDURE {procedure.Name}");
            sb.Append(procedure.SourceCode);

            if (!procedure.SourceCode.TrimEnd().EndsWith(";"))
            {
                sb.AppendLine();
                sb.Append(';');
            }

            return sb.ToString();
        }

        public void SaveToFiles(string outputDirectory, List<Domain> domains, List<Table> tables, List<Procedure> procedures)
        {
            if (string.IsNullOrWhiteSpace(outputDirectory))
            {
                throw new ArgumentException("Katalog wyjściowy nie może być pusty.", nameof(outputDirectory));
            }

            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            int totalFiles = 0;

            if (domains != null && domains.Count > 0)
            {
                var domainsDir = Path.Combine(outputDirectory, "domains");
                Directory.CreateDirectory(domainsDir);

                foreach (var domain in domains)
                {
                    var fileName = $"{domain.Name}.sql";
                    var filePath = Path.Combine(domainsDir, fileName);
                    var script = GenerateDomainScript(domain);

                    File.WriteAllText(filePath, script, Encoding.UTF8);
                    totalFiles++;
                }

                Console.WriteLine($"Zapisano {domains.Count} skryptów domen do {domainsDir}");
            }

            if (tables != null && tables.Count > 0)
            {
                var tablesDir = Path.Combine(outputDirectory, "tables");
                Directory.CreateDirectory(tablesDir);

                foreach (var table in tables)
                {
                    var fileName = $"{table.Name}.sql";
                    var filePath = Path.Combine(tablesDir, fileName);
                    var script = GenerateTableScript(table);

                    File.WriteAllText(filePath, script, Encoding.UTF8);
                    totalFiles++;
                }

                Console.WriteLine($"Zapisano {tables.Count} skryptów tabel do {tablesDir}");
            }

            if (procedures != null && procedures.Count > 0)
            {
                var proceduresDir = Path.Combine(outputDirectory, "procedures");
                Directory.CreateDirectory(proceduresDir);

                foreach (var procedure in procedures)
                {
                    var fileName = $"{procedure.Name}.sql";
                    var filePath = Path.Combine(proceduresDir, fileName);
                    var script = GenerateProcedureScript(procedure);

                    File.WriteAllText(filePath, script, Encoding.UTF8);
                    totalFiles++;
                }

                Console.WriteLine($"Zapisano {procedures.Count} skryptów procedur do {proceduresDir}");
            }

            Console.WriteLine($"Łącznie zapisano plików: {totalFiles}");
        }
    }
}
