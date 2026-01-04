using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace DbMetaTool.Scripts
{
    public class SqlScriptParser : IScriptParser
    {
        public ParsedScripts ParseScriptsFromDirectory(string scriptsDirectory)
        {
            if (string.IsNullOrWhiteSpace(scriptsDirectory))
            {
                throw new ArgumentException("Katalog skryptów nie może być pusty.", nameof(scriptsDirectory));
            }

            if (!Directory.Exists(scriptsDirectory))
            {
                throw new DirectoryNotFoundException($"Nie znaleziono katalogu skryptów: {scriptsDirectory}");
            }

            var result = new ParsedScripts();

            var domainsDir = Path.Combine(scriptsDirectory, "domains");
            var tablesDir = Path.Combine(scriptsDirectory, "tables");
            var proceduresDir = Path.Combine(scriptsDirectory, "procedures");

            if (Directory.Exists(domainsDir))
            {
                result.DomainScripts = LoadScriptsFromDirectory(domainsDir);
                Console.WriteLine($"Załadowano {result.DomainScripts.Count} skryptów domen z {domainsDir}");
            }

            if (Directory.Exists(tablesDir))
            {
                result.TableScripts = LoadScriptsFromDirectory(tablesDir);
                Console.WriteLine($"Załadowano {result.TableScripts.Count} skryptów tabel z {tablesDir}");
            }

            if (Directory.Exists(proceduresDir))
            {
                result.ProcedureScripts = LoadScriptsFromDirectory(proceduresDir);
                Console.WriteLine($"Załadowano {result.ProcedureScripts.Count} skryptów procedur z {proceduresDir}");
            }

            if (result.TotalCount == 0)
            {
                Console.WriteLine("Nie znaleziono podkatalogów. Skanowanie głównego katalogu i wykrywanie typów skryptów...");
                result = ParseFlatDirectory(scriptsDirectory);
            }

            Console.WriteLine($"Łącznie załadowano skryptów: {result.TotalCount}");
            return result;
        }

        private ParsedScripts ParseFlatDirectory(string directory)
        {
            var result = new ParsedScripts();
            var sqlFiles = Directory.GetFiles(directory, "*.sql", SearchOption.TopDirectoryOnly);

            foreach (var filePath in sqlFiles)
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    var scriptType = DetectScriptType(content);

                    switch (scriptType)
                    {
                        case ScriptType.Domain:
                            result.DomainScripts.Add(content);
                            break;
                        case ScriptType.Table:
                            result.TableScripts.Add(content);
                            break;
                        case ScriptType.Procedure:
                            result.ProcedureScripts.Add(content);
                            break;
                        default:
                            Console.WriteLine($"Ostrzeżenie: Nie można wykryć typu skryptu: {Path.GetFileName(filePath)}");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas odczytywania pliku {filePath}: {ex.Message}");
                }
            }

            return result;
        }

        private List<string> LoadScriptsFromDirectory(string directory)
        {
            var scripts = new List<string>();
            var sqlFiles = Directory.GetFiles(directory, "*.sql", SearchOption.TopDirectoryOnly);

            foreach (var filePath in sqlFiles)
            {
                try
                {
                    var content = File.ReadAllText(filePath);
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        scripts.Add(content);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Błąd podczas odczytywania pliku {filePath}: {ex.Message}");
                }
            }

            return scripts;
        }

        public ScriptType DetectScriptType(string scriptContent)
        {
            if (string.IsNullOrWhiteSpace(scriptContent))
            {
                return ScriptType.Unknown;
            }

            var normalizedContent = RemoveComments(scriptContent);

            if (Regex.IsMatch(normalizedContent, @"\bCREATE\s+DOMAIN\b", RegexOptions.IgnoreCase))
            {
                return ScriptType.Domain;
            }

            if (Regex.IsMatch(normalizedContent, @"\bCREATE\s+TABLE\b", RegexOptions.IgnoreCase))
            {
                return ScriptType.Table;
            }

            if (Regex.IsMatch(normalizedContent, @"\bCREATE\s+PROCEDURE\b", RegexOptions.IgnoreCase))
            {
                return ScriptType.Procedure;
            }

            return ScriptType.Unknown;
        }

        private string RemoveComments(string sql)
        {
            sql = Regex.Replace(sql, @"--.*$", "", RegexOptions.Multiline);
            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
            return sql;
        }
    }
}
