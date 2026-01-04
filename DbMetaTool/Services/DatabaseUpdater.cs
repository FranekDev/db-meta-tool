using DbMetaTool.Database;
using DbMetaTool.Scripts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbMetaTool.Services
{
    public class DatabaseUpdater
    {
        private readonly IMetadataExtractor _metadataExtractor;
        private readonly IScriptParser _scriptParser;
        private readonly IFirebirdConnection _firebirdConnection;

        public DatabaseUpdater(
            IMetadataExtractor metadataExtractor,
            IScriptParser scriptParser,
            IFirebirdConnection firebirdConnection)
        {
            _metadataExtractor = metadataExtractor ?? throw new ArgumentNullException(nameof(metadataExtractor));
            _scriptParser = scriptParser ?? throw new ArgumentNullException(nameof(scriptParser));
            _firebirdConnection = firebirdConnection ?? throw new ArgumentNullException(nameof(firebirdConnection));
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

        private DatabaseChanges AnalyzeChanges(
            List<Models.Domain> existingDomains,
            List<Models.Table> existingTables,
            List<Models.Procedure> existingProcedures,
            ParsedScripts parsedScripts)
        {
            var changes = new DatabaseChanges();

            var existingDomainNames = new HashSet<string>(existingDomains.Select(d => d.Name), StringComparer.OrdinalIgnoreCase);
            var existingTableNames = new HashSet<string>(existingTables.Select(t => t.Name), StringComparer.OrdinalIgnoreCase);
            var existingProcedureNames = new HashSet<string>(existingProcedures.Select(p => p.Name), StringComparer.OrdinalIgnoreCase);

            foreach (var script in parsedScripts.ProcedureScripts)
            {
                var name = ExtractObjectName(script, "PROCEDURE");
                if (!string.IsNullOrEmpty(name))
                {
                    if (existingProcedureNames.Contains(name))
                    {
                        changes.ProceduresToDrop.Add(name);
                        changes.ProceduresToCreate.Add(script);
                    }
                    else
                    {
                        changes.ProceduresToCreate.Add(script);
                    }
                }
            }

            foreach (var script in parsedScripts.TableScripts)
            {
                var name = ExtractObjectName(script, "TABLE");
                if (!string.IsNullOrEmpty(name))
                {
                    if (existingTableNames.Contains(name))
                    {
                        changes.TablesToDrop.Add(name);
                        changes.TablesToCreate.Add(script);
                    }
                    else
                    {
                        changes.TablesToCreate.Add(script);
                    }
                }
            }

            foreach (var script in parsedScripts.DomainScripts)
            {
                var name = ExtractObjectName(script, "DOMAIN");
                if (!string.IsNullOrEmpty(name))
                {
                    if (existingDomainNames.Contains(name))
                    {
                        changes.DomainsToDrop.Add(name);
                        changes.DomainsToCreate.Add(script);
                    }
                    else
                    {
                        changes.DomainsToCreate.Add(script);
                    }
                }
            }

            return changes;
        }

        private string ExtractObjectName(string script, string objectType)
        {
            var pattern = $@"CREATE\s+{objectType}\s+(\w+)";
            var match = Regex.Match(script, pattern, RegexOptions.IgnoreCase);

            if (match.Success && match.Groups.Count > 1)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }

        private void PrintChangesSummary(DatabaseChanges changes)
        {
            Console.WriteLine("Wykryte zmiany:");
            Console.WriteLine($"  Domeny:     {changes.DomainsToCreate.Count} do utworzenia/aktualizacji, {changes.DomainsToDrop.Count} do usunięcia");
            Console.WriteLine($"  Tabele:     {changes.TablesToCreate.Count} do utworzenia/aktualizacji, {changes.TablesToDrop.Count} do usunięcia");
            Console.WriteLine($"  Procedury:  {changes.ProceduresToCreate.Count} do utworzenia/aktualizacji, {changes.ProceduresToDrop.Count} do usunięcia");
            Console.WriteLine($"  Łączna liczba zmian: {changes.TotalChanges}");
        }

        private void ApplyChanges(string connectionString, DatabaseChanges changes)
        {
            int totalExecuted = 0;

            if (changes.ProceduresToDrop.Count > 0)
            {
                Console.WriteLine($"Usuwanie {changes.ProceduresToDrop.Count} procedur(y)...");
                foreach (var name in changes.ProceduresToDrop)
                {
                    ExecuteSafely(connectionString, $"DROP PROCEDURE {name};", $"DROP PROCEDURE {name}");
                    totalExecuted++;
                }
                Console.WriteLine($"  ✓ Usunięto {changes.ProceduresToDrop.Count} procedur(y)");
            }

            if (changes.TablesToDrop.Count > 0)
            {
                Console.WriteLine($"Usuwanie {changes.TablesToDrop.Count} tabel(i)...");
                foreach (var name in changes.TablesToDrop)
                {
                    ExecuteSafely(connectionString, $"DROP TABLE {name};", $"DROP TABLE {name}");
                    totalExecuted++;
                }
                Console.WriteLine($"  ✓ Usunięto {changes.TablesToDrop.Count} tabel(i)");
            }

            if (changes.DomainsToDrop.Count > 0)
            {
                Console.WriteLine($"Usuwanie {changes.DomainsToDrop.Count} domen(y)...");
                foreach (var name in changes.DomainsToDrop)
                {
                    ExecuteSafely(connectionString, $"DROP DOMAIN {name};", $"DROP DOMAIN {name}");
                    totalExecuted++;
                }
                Console.WriteLine($"  ✓ Usunięto {changes.DomainsToDrop.Count} domen(y)");
            }

            if (changes.DomainsToCreate.Count > 0)
            {
                Console.WriteLine($"Tworzenie {changes.DomainsToCreate.Count} domen(y)...");
                foreach (var script in changes.DomainsToCreate)
                {
                    ExecuteSafely(connectionString, script, "CREATE DOMAIN");
                    totalExecuted++;
                }
                Console.WriteLine($"  ✓ Utworzono {changes.DomainsToCreate.Count} domen(y)");
            }

            if (changes.TablesToCreate.Count > 0)
            {
                Console.WriteLine($"Tworzenie {changes.TablesToCreate.Count} tabel(i)...");
                foreach (var script in changes.TablesToCreate)
                {
                    ExecuteSafely(connectionString, script, "CREATE TABLE");
                    totalExecuted++;
                }
                Console.WriteLine($"  ✓ Utworzono {changes.TablesToCreate.Count} tabel(i)");
            }

            if (changes.ProceduresToCreate.Count > 0)
            {
                Console.WriteLine($"Tworzenie {changes.ProceduresToCreate.Count} procedur(y)...");
                foreach (var script in changes.ProceduresToCreate)
                {
                    ExecuteSafely(connectionString, script, "CREATE PROCEDURE");
                    totalExecuted++;
                }
                Console.WriteLine($"  ✓ Utworzono {changes.ProceduresToCreate.Count} procedur(y)");
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
            public List<string> DomainsToDrop { get; set; } = new List<string>();
            public List<string> DomainsToCreate { get; set; } = new List<string>();

            public List<string> TablesToDrop { get; set; } = new List<string>();
            public List<string> TablesToCreate { get; set; } = new List<string>();

            public List<string> ProceduresToDrop { get; set; } = new List<string>();
            public List<string> ProceduresToCreate { get; set; } = new List<string>();

            public int TotalChanges =>
                DomainsToDrop.Count + DomainsToCreate.Count +
                TablesToDrop.Count + TablesToCreate.Count +
                ProceduresToDrop.Count + ProceduresToCreate.Count;
        }
    }
}
