using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace DbMetaTool.Database
{
    public class FirebirdConnection : IFirebirdConnection
    {
        public void CreateDatabase(string databasePath)
        {
            if (string.IsNullOrWhiteSpace(databasePath))
            {
                throw new ArgumentException("Ścieżka bazy danych nie może być pusta.", nameof(databasePath));
            }

            var directory = Path.GetDirectoryName(databasePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .Build();

            var dataSource = configuration["DatabaseConnection:DataSource"];
            if (string.IsNullOrWhiteSpace(dataSource))
                throw new InvalidOperationException("DatabaseConnection:DataSource nie jest skonfigurowane w appsettings.json");

            var userID = configuration["DatabaseConnection:UserID"];
            if (string.IsNullOrWhiteSpace(userID))
                throw new InvalidOperationException("DatabaseConnection:UserID nie jest skonfigurowane w appsettings.json");

            var password = configuration["DatabaseConnection:Password"];
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("DatabaseConnection:Password nie jest skonfigurowane w appsettings.json");

            var charset = configuration["DatabaseConnection:Charset"];
            if (string.IsNullOrWhiteSpace(charset))
                throw new InvalidOperationException("DatabaseConnection:Charset nie jest skonfigurowane w appsettings.json");

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

            try
            {
                FbConnection.CreateDatabase(csb.ToString(), overwrite: false);
                Console.WriteLine($"Utworzono bazę danych: {databasePath}");
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas tworzenia bazy danych: {ex.Message}", ex);
            }
        }

        public int ExecuteNonQuery(string connectionString, string sql)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string nie może być pusty.", nameof(connectionString));
            }

            if (string.IsNullOrWhiteSpace(sql))
            {
                throw new ArgumentException("Zapytanie SQL nie może być puste.", nameof(sql));
            }

            try
            {
                using var connection = new FbConnection(connectionString);
                connection.Open();

                var sqlWithoutComments = RemoveSqlComments(sql);
                var (cleanedSql, separator) = ParseTerminator(sqlWithoutComments);

                var commands = cleanedSql.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);

                int affectedRows = 0;
                foreach (var query in commands.Where(x => !string.IsNullOrWhiteSpace(x)))
                {
                    var trimmedQuery = query.Trim();
                    if (string.IsNullOrWhiteSpace(trimmedQuery))
                        continue;

                    using var transaction = connection.BeginTransaction();
                    try
                    {
                        using var command = new FbCommand(trimmedQuery, connection, transaction);
                        command.CommandTimeout = 120;
                        affectedRows += command.ExecuteNonQuery();
                        transaction.Commit();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd podczas wykonywania zapytania: {ex.Message}\nSQL: {trimmedQuery}");
                        transaction.Rollback();
                        throw;
                    }
                }

                return affectedRows;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas wykonywania zapytania SQL: {ex.Message}\nSQL: {sql}", ex);
            }
        }

        private string RemoveSqlComments(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return sql;
            }

            sql = Regex.Replace(sql, @"/\*.*?\*/", "", RegexOptions.Singleline);
            sql = Regex.Replace(sql, @"--.*?$", "", RegexOptions.Multiline);

            return sql;
        }

        private (string cleanedSql, string separator) ParseTerminator(string sql)
        {
            if (string.IsNullOrWhiteSpace(sql))
            {
                return (sql, ";");
            }

            var setTermPattern = @"SET\s+TERM\s+(\S+)\s*;?";
            var match = Regex.Match(sql, setTermPattern, RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                return (sql, ";");
            }

            var newSeparator = match.Groups[1].Value.Trim();

            var cleanedSql = Regex.Replace(sql, setTermPattern, "", RegexOptions.IgnoreCase | RegexOptions.Multiline);

            return (cleanedSql, newSeparator);
        }

        public FbConnection GetConnection(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new ArgumentException("Connection string nie może być pusty.", nameof(connectionString));
            }

            try
            {
                var connection = new FbConnection(connectionString);
                connection.Open();
                return connection;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas nawiązywania połączenia z bazą danych: {ex.Message}", ex);
            }
        }
    }
}
