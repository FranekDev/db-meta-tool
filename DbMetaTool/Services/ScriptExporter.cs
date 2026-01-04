using DbMetaTool.Database;
using DbMetaTool.Scripts;
using System;
using System.IO;

namespace DbMetaTool.Services
{
    public class ScriptExporter
    {
        private readonly IMetadataExtractor _metadataExtractor;
        private readonly IScriptGenerator _scriptGenerator;

        public ScriptExporter(IMetadataExtractor metadataExtractor, IScriptGenerator scriptGenerator)
        {
            _metadataExtractor = metadataExtractor ?? throw new ArgumentNullException(nameof(metadataExtractor));
            _scriptGenerator = scriptGenerator ?? throw new ArgumentNullException(nameof(scriptGenerator));
        }

        public void Export(string connectionString, string outputDirectory)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Connection string nie może być pusty.", nameof(connectionString));

            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new ArgumentException("Katalog wyjściowy nie może być pusty.", nameof(outputDirectory));

            Console.WriteLine("=== ROZPOCZĘTO EKSPORT SKRYPTÓW ===");
            Console.WriteLine($"Katalog wyjściowy: {outputDirectory}");
            Console.WriteLine();

            outputDirectory = Path.GetFullPath(outputDirectory);
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
                Console.WriteLine($"Utworzono katalog wyjściowy: {outputDirectory}");
            }

            Console.WriteLine("Krok 1: Wydobywanie metadanych z bazy danych...");
            var domains = _metadataExtractor.ExtractDomains(connectionString);
            var tables = _metadataExtractor.ExtractTables(connectionString);
            var procedures = _metadataExtractor.ExtractProcedures(connectionString);
            Console.WriteLine();

            int totalObjects = domains.Count + tables.Count + procedures.Count;

            if (totalObjects == 0)
            {
                Console.WriteLine("Ostrzeżenie: Nie znaleziono metadanych w bazie danych. Brak danych do eksportu.");
                return;
            }

            Console.WriteLine($"Znaleziono {totalObjects} obiektów bazy danych:");
            Console.WriteLine($"  - {domains.Count} domen(y)");
            Console.WriteLine($"  - {tables.Count} tabel(i)");
            Console.WriteLine($"  - {procedures.Count} procedur(y)");
            Console.WriteLine();

            Console.WriteLine("Krok 2: Generowanie skryptów SQL i zapisywanie do plików...");
            _scriptGenerator.SaveToFiles(outputDirectory, domains, tables, procedures);
            Console.WriteLine();

            Console.WriteLine("=== ZAKOŃCZONO EKSPORT SKRYPTÓW POMYŚLNIE ===");
        }
    }
}
