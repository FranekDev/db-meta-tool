using System;

namespace DbMetaTool.Database
{
    public static class FirebirdDataTypeExtensions
    {
        public static string ToSqlTypeString(this short fieldType, int? length = null, int? precision = null, int? scale = null)
        {
            if (!Enum.IsDefined(typeof(FirebirdDataType), fieldType))
            {
                return $"UNKNOWN_TYPE_{fieldType}";
            }

            var type = (FirebirdDataType)fieldType;
            return type.ToSqlTypeString(length, precision, scale);
        }

        public static string ToSqlTypeString(this FirebirdDataType dataType, int? length = null, int? precision = null, int? scale = null)
        {
            return dataType switch
            {
                FirebirdDataType.Smallint => "SMALLINT",
                FirebirdDataType.Integer => "INTEGER",
                FirebirdDataType.Float => "FLOAT",
                FirebirdDataType.Date => "DATE",
                FirebirdDataType.Time => "TIME",
                FirebirdDataType.Char when length.HasValue => $"CHAR({length})",
                FirebirdDataType.Char => "CHAR",
                FirebirdDataType.Int64 when precision.HasValue && scale.HasValue && scale != 0 =>
                    $"NUMERIC({precision},{Math.Abs(scale.Value)})",
                FirebirdDataType.Int64 when precision.HasValue && scale.HasValue =>
                    $"DECIMAL({precision},{Math.Abs(scale.Value)})",
                FirebirdDataType.Int64 => "BIGINT",
                FirebirdDataType.Boolean => "BOOLEAN",
                FirebirdDataType.Double => "DOUBLE PRECISION",
                FirebirdDataType.Timestamp => "TIMESTAMP",
                FirebirdDataType.Varchar when length.HasValue => $"VARCHAR({length})",
                FirebirdDataType.Varchar => "VARCHAR",
                FirebirdDataType.Blob => "BLOB",
                _ => dataType.ToString().ToUpper()
            };
        }
    }
}
