using DbMetaTool.Database;
using DbMetaTool.Models;
using DbMetaTool.Models.Changes;
using DbMetaTool.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DbMetaTool.Services
{
    public class DatabaseUpdater
    {
        private readonly IMetadataExtractor _metadataExtractor;
        private readonly IScriptParser _scriptParser;
        private readonly IFirebirdConnection _firebirdConnection;
        private readonly SqlDefinitionParser _definitionParser;
        private readonly SchemaComparer _schemaComparer;

        public DatabaseUpdater(
            IMetadataExtractor metadataExtractor,
            IScriptParser scriptParser,
            IFirebirdConnection firebirdConnection)
        {
            _metadataExtractor = metadataExtractor ?? throw new ArgumentNullException(nameof(metadataExtractor));
            _scriptParser = scriptParser ?? throw new ArgumentNullException(nameof(scriptParser));
            _firebirdConnection = firebirdConnection ?? throw new ArgumentNullException(nameof(firebirdConnection));
            _definitionParser = new SqlDefinitionParser();
            _schemaComparer = new SchemaComparer();
        }

        public void Update(string connectionString, string scriptsDirectory)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string nie może być pusty.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(scriptsDirectory))
            {
                throw new ArgumentException("Katalog skryptów nie może być pusty.", nameof(scriptsDirectory));
            }

            if (!Directory.Exists(scriptsDirectory))
            {
                throw new DirectoryNotFoundException($"Nie znaleziono katalogu skryptów: {scriptsDirectory}");
            }

            Console.WriteLine("=== ROZPOCZĘTO AKTUALIZACJĘ BAZY DANYCH ===");
            Console.WriteLine($"Katalog skryptów: {scriptsDirectory}");
            Console.WriteLine();

            Console.WriteLine("Krok 1: Wydobywanie istniejących metadanych z bazy danych...");
            var existingDomains = _metadataExtractor.ExtractDomains(connectionString);
            var existingTables = _metadataExtractor.ExtractTables(connectionString);
            var existingProcedures = _metadataExtractor.ExtractProcedures(connectionString);
            Console.WriteLine($"Znaleziono {existingDomains.Count} domen(y), {existingTables.Count} tabel(i), {existingProcedures.Count} procedur(y)");
            Console.WriteLine();

            Console.WriteLine("Krok 2: Parsowanie skryptów z katalogu...");
            var parsedScripts = _scriptParser.ParseScriptsFromDirectory(scriptsDirectory);
            ParseScriptsToObjects(parsedScripts);
            Console.WriteLine();

            if (parsedScripts.TotalCount == 0)
            {
                Console.WriteLine("Ostrzeżenie: Nie znaleziono skryptów do wykonania.");
                return;
            }

            Console.WriteLine("Krok 3: Analiza zmian...");
            var changes = AnalyzeChanges(existingDomains, existingTables, existingProcedures, parsedScripts);
            PrintChangesSummary(changes);
            Console.WriteLine();

            if (changes.TotalChanges == 0)
            {
                Console.WriteLine("Nie wykryto zmian. Baza danych jest aktualna.");
                return;
            }

            Console.WriteLine("Krok 4: Stosowanie zmian do bazy danych...");
            ApplyChanges(connectionString, changes);
            Console.WriteLine();

            Console.WriteLine("=== ZAKOŃCZONO AKTUALIZACJĘ BAZY DANYCH POMYŚLNIE ===");
        }

        private void ParseScriptsToObjects(ParsedScripts parsedScripts)
        {
            foreach (var script in parsedScripts.DomainScripts)
            {
                try
                {
                    var domain = _definitionParser.ParseDomainScript(script);
                    parsedScripts.ParsedDomains.Add(domain);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Ostrzeżenie: Nie można sparsować domeny: {ex.Message}");
                }
            }

            foreach (var script in parsedScripts.TableScripts)
            {
                try
                {
                    var table = _definitionParser.ParseTableScript(script);
                    parsedScripts.ParsedTables.Add(table);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Ostrzeżenie: Nie można sparsować tabeli: {ex.Message}");
                }
            }

            Console.WriteLine($"  Sparsowano {parsedScripts.ParsedDomains.Count} domen, {parsedScripts.ParsedTables.Count} tabel, {parsedScripts.ProcedureScripts.Count} procedur");
        }

        private DatabaseChanges AnalyzeChanges(
            List<Domain> existingDomains,
            List<Table> existingTables,
            List<Procedure> existingProcedures,
            ParsedScripts parsedScripts)
        {
            var changes = new DatabaseChanges();

            var domainChanges = _schemaComparer.CompareDomains(existingDomains, parsedScripts.ParsedDomains);

            var domainScriptsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var domain in parsedScripts.ParsedDomains)
            {
                var originalScript = parsedScripts.DomainScripts
                    .FirstOrDefault(s => ExtractObjectName(s, "DOMAIN")
                        .Equals(domain.Name, StringComparison.OrdinalIgnoreCase));
                if (originalScript != null)
                {
                    domainScriptsMap[domain.Name] = originalScript;
                }
            }

            foreach (var domain in domainChanges.DomainsToCreate)
            {
                if (domainScriptsMap.TryGetValue(domain.Name, out var originalScript))
                {
                    changes.DomainCreateScripts.Add(originalScript);
                }
            }

            changes.DomainAlterStatements.AddRange(domainChanges.DomainsToAlter.SelectMany(d => d.AlterStatements));

            var tableChanges = _schemaComparer.CompareTables(existingTables, parsedScripts.ParsedTables);

            var tableScriptsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var table in parsedScripts.ParsedTables)
            {
                var originalScript = parsedScripts.TableScripts
                    .FirstOrDefault(s => ExtractObjectName(s, "TABLE")
                        .Equals(table.Name, StringComparison.OrdinalIgnoreCase));
                if (originalScript != null)
                {
                    tableScriptsMap[table.Name] = originalScript;
                }
            }

            foreach (var table in tableChanges.TablesToCreate)
            {
                if (tableScriptsMap.TryGetValue(table.Name, out var originalScript))
                {
                    changes.TableCreateScripts.Add(originalScript);
                }
            }

            foreach (var (tableName, columnChanges) in tableChanges.TablesToAlter)
            {
                foreach (var column in columnChanges.ColumnsToAdd)
                {
                    var addStatement = GenerateAddColumnStatement(tableName, column);
                    changes.TableAlterStatements.Add(addStatement);
                }

                foreach (var (colName, alterStatements) in columnChanges.ColumnsToAlter)
                {
                    changes.TableAlterStatements.AddRange(alterStatements);
                }
            }

            foreach (var script in parsedScripts.ProcedureScripts)
            {
                var name = ExtractObjectName(script, "PROCEDURE");
                if (!string.IsNullOrEmpty(name))
                {
                    var createOrAlterScript = ConvertToCreateOrAlter(script);
                    changes.ProcedureScripts.Add(createOrAlterScript);
                }
            }

            return changes;
        }

        private string GenerateAddColumnStatement(string tableName, Column column)
        {
            var sb = new StringBuilder();
            sb.Append($"ALTER TABLE {tableName} ADD {column.Name} ");

            if (!string.IsNullOrWhiteSpace(column.DomainName))
                sb.Append(column.DomainName);
            else
                sb.Append(column.DataType);

            if (!column.IsNullable)
                sb.Append(" NOT NULL");

            if (!string.IsNullOrWhiteSpace(column.DefaultValue))
                sb.Append($" DEFAULT {column.DefaultValue}");

            return sb.ToString();
        }

        private string ExtractObjectName(string script, string objectType)
        {
            var pattern = $@"CREATE\s+(?:OR\s+ALTER\s+)?{objectType}\s+(\w+)";
            var match = Regex.Match(script, pattern, RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        private string ConvertToCreateOrAlter(string script)
        {
            return Regex.Replace(
                script,
                @"\bCREATE\s+PROCEDURE\b",
                "CREATE OR ALTER PROCEDURE",
                RegexOptions.IgnoreCase);
        }

        private void PrintChangesSummary(DatabaseChanges changes)
        {
            Console.WriteLine("Wykryte zmiany:");
            Console.WriteLine($"  Domeny:     {changes.DomainCreateScripts.Count} do utworzenia, {changes.DomainAlterStatements.Count} modyfikacji");
            Console.WriteLine($"  Tabele:     {changes.TableCreateScripts.Count} do utworzenia, {changes.TableAlterStatements.Count} modyfikacji");
            Console.WriteLine($"  Procedury:  {changes.ProcedureScripts.Count} do utworzenia/aktualizacji");
            Console.WriteLine($"  Łączna liczba zmian: {changes.TotalChanges}");
        }

        private void ApplyChanges(string connectionString, DatabaseChanges changes)
        {
            int totalExecuted = 0;

            if (changes.DomainAlterStatements.Count > 0)
            {
                Console.WriteLine($"Modyfikowanie domen ({changes.DomainAlterStatements.Count} zmian)...");
                foreach (var statement in changes.DomainAlterStatements)
                {
                    ExecuteSafely(connectionString, statement, statement);
                    totalExecuted++;
                }
                Console.WriteLine($"  Zmodyfikowano domeny");
            }

            if (changes.DomainCreateScripts.Count > 0)
            {
                Console.WriteLine($"Tworzenie {changes.DomainCreateScripts.Count} nowych domen...");
                foreach (var script in changes.DomainCreateScripts)
                {
                    ExecuteSafely(connectionString, script, "CREATE DOMAIN");
                    totalExecuted++;
                }
                Console.WriteLine($"  Utworzono {changes.DomainCreateScripts.Count} domen");
            }

            if (changes.TableCreateScripts.Count > 0)
            {
                Console.WriteLine($"Tworzenie {changes.TableCreateScripts.Count} nowych tabel...");
                foreach (var script in changes.TableCreateScripts)
                {
                    ExecuteSafely(connectionString, script, "CREATE TABLE");
                    totalExecuted++;
                }
                Console.WriteLine($"  Utworzono {changes.TableCreateScripts.Count} tabel");
            }

            if (changes.TableAlterStatements.Count > 0)
            {
                Console.WriteLine($"Modyfikowanie tabel ({changes.TableAlterStatements.Count} zmian)...");
                foreach (var statement in changes.TableAlterStatements)
                {
                    ExecuteSafely(connectionString, statement, statement);
                    totalExecuted++;
                }
                Console.WriteLine($"  Zmodyfikowano tabele");
            }

            if (changes.ProcedureScripts.Count > 0)
            {
                Console.WriteLine($"Tworzenie/aktualizowanie {changes.ProcedureScripts.Count} procedur(y)...");
                foreach (var script in changes.ProcedureScripts)
                {
                    ExecuteSafely(connectionString, script, "CREATE OR ALTER PROCEDURE");
                    totalExecuted++;
                }
                Console.WriteLine($"  Przetworzono {changes.ProcedureScripts.Count} procedur");
            }

            Console.WriteLine($"Łącznie wykonano operacji: {totalExecuted}");
        }

        private void ExecuteSafely(string connectionString, string sql, string description)
        {
            try
            {
                _firebirdConnection.ExecuteNonQuery(connectionString, sql);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Nie udało się wykonać: {description}\nBłąd: {ex.Message}", ex);
            }
        }

        private class DatabaseChanges
        {
            public List<string> DomainCreateScripts { get; } = new List<string>();
            public List<string> DomainAlterStatements { get; } = new List<string>();

            public List<string> TableCreateScripts { get; } = new List<string>();
            public List<string> TableAlterStatements { get; } = new List<string>();

            public List<string> ProcedureScripts { get; } = new List<string>();

            public int TotalChanges =>
                DomainCreateScripts.Count + DomainAlterStatements.Count +
                TableCreateScripts.Count + TableAlterStatements.Count +
                ProcedureScripts.Count;
        }
    }
}
