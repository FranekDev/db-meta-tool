using FirebirdSql.Data.FirebirdClient;

namespace DbMetaTool.Database
{
    public interface IFirebirdConnection
    {
        void CreateDatabase(string databasePath);
        int ExecuteNonQuery(string connectionString, string sql);
        FbConnection GetConnection(string connectionString);
    }
}
