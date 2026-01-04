using DbMetaTool.Database;
using DbMetaTool.Scripts;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace DbMetaTool.Services
{
    public class DatabaseBuilder
    {
        private readonly IFirebirdConnection _firebirdConnection;
        private readonly IScriptParser _scriptParser;

        public DatabaseBuilder(IFirebirdConnection firebirdConnection, IScriptParser scriptParser)
        {
            _firebirdConnection = firebirdConnection ?? throw new ArgumentNullException(nameof(firebirdConnection));
            _scriptParser = scriptParser ?? throw new ArgumentNullException(nameof(scriptParser));
        }

        public void Build(string databaseDirectory, string scriptsDirectory)
        {
            if (string.IsNullOrWhiteSpace(databaseDirectory))
            {
                throw new ArgumentException("Katalog bazy danych nie może być pusty.", nameof(databaseDirectory));
            }

            if (string.IsNullOrWhiteSpace(scriptsDirectory))
            {
                throw new ArgumentException("Katalog skryptów nie może być pusty.", nameof(scriptsDirectory));
            }

            if (!Directory.Exists(scriptsDirectory))
            {
                throw new DirectoryNotFoundException($"Nie znaleziono katalogu skryptów: {scriptsDirectory}");
            }

            Console.WriteLine("=== ROZPOCZĘTO BUDOWANIE BAZY DANYCH ===");
            Console.WriteLine($"Katalog bazy danych: {databaseDirectory}");
            Console.WriteLine($"Katalog skryptów: {scriptsDirectory}");
            Console.WriteLine();

            var databasePath = Path.Combine(databaseDirectory, "database.fdb");

            Console.WriteLine("Krok 1: Tworzenie pustej bazy danych...");
            _firebirdConnection.CreateDatabase(databasePath);
            Console.WriteLine();

            Console.WriteLine("Krok 2: Parsowanie skryptów...");
            var parsedScripts = _scriptParser.ParseScriptsFromDirectory(scriptsDirectory);
            Console.WriteLine();

            if (parsedScripts.TotalCount == 0)
            {
                Console.WriteLine("Ostrzeżenie: Nie znaleziono skryptów do wykonania.");
                return;
            }

            var connectionString = BuildConnectionString(databasePath);

            Console.WriteLine("Krok 3: Wykonywanie skryptów w kolejności...");
            ExecuteScripts(connectionString, parsedScripts);
            Console.WriteLine();

            Console.WriteLine("=== ZAKOŃCZONO BUDOWANIE BAZY DANYCH POMYŚLNIE ===");
        }

        private void ExecuteScripts(string connectionString, ParsedScripts scripts)
        {
            int totalExecuted = 0;

            if (scripts.DomainScripts.Count > 0)
            {
                Console.WriteLine($"Wykonywanie {scripts.DomainScripts.Count} skryptów domen...");
                int executed = ExecuteScriptBatch(connectionString, scripts.DomainScripts, ScriptType.Domain);
                totalExecuted += executed;
                Console.WriteLine($"  ✓ Utworzono {executed} domen(y)");
            }

            if (scripts.TableScripts.Count > 0)
            {
                Console.WriteLine($"Wykonywanie {scripts.TableScripts.Count} skryptów tabel...");
                int executed = ExecuteScriptBatch(connectionString, scripts.TableScripts, ScriptType.Table);
                totalExecuted += executed;
                Console.WriteLine($"  ✓ Utworzono {executed} tabel(i)");
            }

            if (scripts.ProcedureScripts.Count > 0)
            {
                Console.WriteLine($"Wykonywanie {scripts.ProcedureScripts.Count} skryptów procedur...");
                int executed = ExecuteScriptBatch(connectionString, scripts.ProcedureScripts, ScriptType.Procedure);
                totalExecuted += executed;
                Console.WriteLine($"  ✓ Utworzono {executed} procedur(y)");
            }

            Console.WriteLine($"Łącznie wykonano skryptów: {totalExecuted}");
        }

        private int ExecuteScriptBatch(string connectionString, List<string> scripts, ScriptType scriptType)
        {
            int successCount = 0;
            int failureCount = 0;

            foreach (var script in scripts)
            {
                try
                {
                    _firebirdConnection.ExecuteNonQuery(connectionString, script);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failureCount++;
                    Console.WriteLine($"  ✗ Błąd podczas wykonywania skryptu {scriptType}:");
                    Console.WriteLine($"    {ex.Message}");
                    Console.WriteLine($"    Podgląd skryptu: {GetScriptPreview(script)}");

                    throw new InvalidOperationException(
                        $"Nie udało się wykonać skryptu {scriptType}. {successCount} z {scripts.Count} skryptów wykonano pomyślnie przed wystąpieniem błędu.",
                        ex);
                }
            }

            return successCount;
        }

        private string GetScriptPreview(string script)
        {
            if (string.IsNullOrWhiteSpace(script))
            {
                return "[empty script]";
            }

            var firstLine = script.Split('\n')[0].Trim();
            return firstLine.Length > 100 ? firstLine.Substring(0, 100) + "..." : firstLine;
        }

        private string BuildConnectionString(string databasePath)
        {
            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var dataSource = configuration["DatabaseConnection:DataSource"];
            if (string.IsNullOrWhiteSpace(dataSource))
            {
                throw new InvalidOperationException("DatabaseConnection:DataSource nie jest skonfigurowane w appsettings.json");
            }

            var userID = configuration["DatabaseConnection:UserID"];
            if (string.IsNullOrWhiteSpace(userID))
            {
                throw new InvalidOperationException("DatabaseConnection:UserID nie jest skonfigurowane w appsettings.json");
            }

            var password = configuration["DatabaseConnection:Password"];
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new InvalidOperationException("DatabaseConnection:Password nie jest skonfigurowane w appsettings.json");
            }

            var charset = configuration["DatabaseConnection:Charset"];
            if (string.IsNullOrWhiteSpace(charset))
            {
                throw new InvalidOperationException("DatabaseConnection:Charset nie jest skonfigurowane w appsettings.json");
            }

            var csb = new FbConnectionStringBuilder
            {
                DataSource = dataSource,
                Database = databasePath,
                UserID = userID,
                Password = password,
                ServerType = FbServerType.Default,
                Charset = charset,
                Pooling = false
            };

            return csb.ToString();
        }
    }
}
