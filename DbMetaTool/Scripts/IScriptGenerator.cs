using DbMetaTool.Models;

namespace DbMetaTool.Scripts
{
    public interface IScriptGenerator
    {
        string GenerateDomainScript(Domain domain);
        string GenerateTableScript(Table table);
        string GenerateProcedureScript(Procedure procedure);
        void SaveToFiles(string outputDirectory, List<Domain> domains, List<Table> tables, List<Procedure> procedures);
    }
}
