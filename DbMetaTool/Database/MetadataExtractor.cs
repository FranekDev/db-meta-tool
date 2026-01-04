using DbMetaTool.Models;
using FirebirdSql.Data.FirebirdClient;
using System;
using System.Collections.Generic;

namespace DbMetaTool.Database
{
    public class MetadataExtractor : IMetadataExtractor
    {
        private readonly IFirebirdConnection _firebirdConnection;

        public MetadataExtractor(IFirebirdConnection firebirdConnection)
        {
            _firebirdConnection = firebirdConnection ?? throw new ArgumentNullException(nameof(firebirdConnection));
        }

        public List<Domain> ExtractDomains(string connectionString)
        {
            var domains = new List<Domain>();

            const string query = @"
                SELECT
                    TRIM(f.RDB$FIELD_NAME) AS FIELD_NAME,
                    f.RDB$FIELD_TYPE,
                    f.RDB$FIELD_LENGTH,
                    f.RDB$FIELD_PRECISION,
                    f.RDB$FIELD_SCALE,
                    f.RDB$NULL_FLAG,
                    TRIM(f.RDB$DEFAULT_SOURCE) AS DEFAULT_SOURCE,
                    TRIM(f.RDB$VALIDATION_SOURCE) AS VALIDATION_SOURCE
                FROM RDB$FIELDS f
                WHERE f.RDB$FIELD_NAME NOT STARTING WITH 'RDB$'
                  AND f.RDB$SYSTEM_FLAG = 0
                ORDER BY f.RDB$FIELD_NAME";

            try
            {
                using (var connection = _firebirdConnection.GetConnection(connectionString))
                using (var command = new FbCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var fieldType = Convert.ToInt16(reader["RDB$FIELD_TYPE"]);
                        var length = reader["RDB$FIELD_LENGTH"] != DBNull.Value
                            ? Convert.ToInt32(reader["RDB$FIELD_LENGTH"])
                            : (int?)null;
                        var precision = reader["RDB$FIELD_PRECISION"] != DBNull.Value
                            ? Convert.ToInt32(reader["RDB$FIELD_PRECISION"])
                            : (int?)null;
                        var scale = reader["RDB$FIELD_SCALE"] != DBNull.Value
                            ? Convert.ToInt32(reader["RDB$FIELD_SCALE"])
                            : (int?)null;

                        var domain = new Domain
                        {
                            Name = reader["FIELD_NAME"].ToString() ?? string.Empty,
                            DataType = fieldType.ToSqlTypeString(length, precision, scale),
                            Length = length,
                            Precision = precision,
                            Scale = scale,
                            IsNullable = reader["RDB$NULL_FLAG"] == DBNull.Value,
                            DefaultValue = reader["DEFAULT_SOURCE"] != DBNull.Value
                                ? reader["DEFAULT_SOURCE"].ToString()
                                : null,
                        };

                        domains.Add(domain);
                    }
                }

                Console.WriteLine($"Wydobyto {domains.Count} domen");
                return domains;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas wydobywania domen: {ex.Message}", ex);
            }
        }

        public List<Table> ExtractTables(string connectionString)
        {
            var tables = new List<Table>();

            const string query = @"
                SELECT
                    TRIM(RDB$RELATION_NAME) AS TABLE_NAME,
                    TRIM(RDB$DESCRIPTION) AS DESCRIPTION
                FROM RDB$RELATIONS
                WHERE RDB$SYSTEM_FLAG = 0
                  AND RDB$VIEW_BLR IS NULL
                ORDER BY RDB$RELATION_NAME";

            try
            {
                using (var connection = _firebirdConnection.GetConnection(connectionString))
                using (var command = new FbCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var tableName = reader["TABLE_NAME"].ToString() ?? string.Empty;
                        var table = new Table
                        {
                            Name = tableName,
                            Description = reader["DESCRIPTION"] != DBNull.Value
                                ? reader["DESCRIPTION"].ToString()
                                : null
                        };

                        table.Columns = ExtractColumns(connectionString, tableName);
                        tables.Add(table);
                    }
                }

                Console.WriteLine($"Wydobyto {tables.Count} tabel");
                return tables;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas wydobywania tabel: {ex.Message}", ex);
            }
        }

        private List<Column> ExtractColumns(string connectionString, string tableName)
        {
            var columns = new List<Column>();

            const string query = @"
                SELECT
                    TRIM(rf.RDB$FIELD_NAME) AS COLUMN_NAME,
                    TRIM(rf.RDB$FIELD_SOURCE) AS DOMAIN_NAME,
                    rf.RDB$FIELD_POSITION,
                    rf.RDB$NULL_FLAG,
                    TRIM(rf.RDB$DEFAULT_SOURCE) AS DEFAULT_SOURCE,
                    f.RDB$FIELD_TYPE,
                    f.RDB$FIELD_LENGTH,
                    f.RDB$FIELD_PRECISION,
                    f.RDB$FIELD_SCALE
                FROM RDB$RELATION_FIELDS rf
                JOIN RDB$FIELDS f ON rf.RDB$FIELD_SOURCE = f.RDB$FIELD_NAME
                WHERE rf.RDB$RELATION_NAME = @tableName
                ORDER BY rf.RDB$FIELD_POSITION";

            try
            {
                using (var connection = _firebirdConnection.GetConnection(connectionString))
                using (var command = new FbCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@tableName", tableName);

                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        var fieldType = Convert.ToInt16(reader["RDB$FIELD_TYPE"]);
                        var length = reader["RDB$FIELD_LENGTH"] != DBNull.Value
                            ? Convert.ToInt32(reader["RDB$FIELD_LENGTH"])
                            : (int?)null;
                        var precision = reader["RDB$FIELD_PRECISION"] != DBNull.Value
                            ? Convert.ToInt32(reader["RDB$FIELD_PRECISION"])
                            : (int?)null;
                        var scale = reader["RDB$FIELD_SCALE"] != DBNull.Value
                            ? Convert.ToInt32(reader["RDB$FIELD_SCALE"])
                            : (int?)null;

                        var domainName = reader["DOMAIN_NAME"].ToString() ?? string.Empty;
                        var isCustomDomain = !domainName.StartsWith("RDB$");

                        var column = new Column
                        {
                            Name = reader["COLUMN_NAME"].ToString() ?? string.Empty,
                            DomainName = isCustomDomain ? domainName : null,
                            Position = Convert.ToInt32(reader["RDB$FIELD_POSITION"]),
                            IsNullable = reader["RDB$NULL_FLAG"] == DBNull.Value,
                            DefaultValue = reader["DEFAULT_SOURCE"] != DBNull.Value
                                ? reader["DEFAULT_SOURCE"].ToString()
                                : null,
                            FieldType = fieldType,
                            Length = length,
                            Precision = precision,
                            Scale = scale,
                            DataType = fieldType.ToSqlTypeString(length, precision, scale)
                        };

                        columns.Add(column);
                    }
                }

                return columns;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas wydobywania kolumn dla tabeli {tableName}: {ex.Message}", ex);
            }
        }

        public List<Procedure> ExtractProcedures(string connectionString)
        {
            var procedures = new List<Procedure>();

            const string query = @"
                SELECT
                    TRIM(RDB$PROCEDURE_NAME) AS PROCEDURE_NAME,
                    RDB$PROCEDURE_SOURCE AS SOURCE_CODE,
                    TRIM(RDB$DESCRIPTION) AS DESCRIPTION
                FROM RDB$PROCEDURES
                WHERE RDB$SYSTEM_FLAG = 0
                ORDER BY RDB$PROCEDURE_NAME";

            try
            {
                using (var connection = _firebirdConnection.GetConnection(connectionString))
                using (var command = new FbCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var procedure = new Procedure
                        {
                            Name = reader["PROCEDURE_NAME"].ToString() ?? string.Empty,
                            SourceCode = reader["SOURCE_CODE"] != DBNull.Value
                                ? reader["SOURCE_CODE"].ToString() ?? string.Empty
                                : string.Empty,
                            Description = reader["DESCRIPTION"] != DBNull.Value
                                ? reader["DESCRIPTION"].ToString()
                                : null
                        };

                        procedures.Add(procedure);
                    }
                }

                Console.WriteLine($"Wydobyto {procedures.Count} procedur");
                return procedures;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Błąd podczas wydobywania procedur: {ex.Message}", ex);
            }
        }

    }
}
