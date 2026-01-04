using DbMetaTool.Models;

namespace DbMetaTool.Database
{
    public interface IMetadataExtractor
    {
        List<Domain> ExtractDomains(string connectionString);
        List<Table> ExtractTables(string connectionString);
        List<Procedure> ExtractProcedures(string connectionString);
    }
}
