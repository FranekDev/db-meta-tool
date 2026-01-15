using DbMetaTool.Models;

namespace DbMetaTool.Scripts
{
    public class ParsedScripts
    {
        public List<string> DomainScripts { get; set; } = new List<string>();
        public List<string> TableScripts { get; set; } = new List<string>();
        public List<string> ProcedureScripts { get; set; } = new List<string>();

        public List<Domain> ParsedDomains { get; set; } = new List<Domain>();
        public List<Table> ParsedTables { get; set; } = new List<Table>();

        public int TotalCount => DomainScripts.Count + TableScripts.Count + ProcedureScripts.Count;
    }
}
