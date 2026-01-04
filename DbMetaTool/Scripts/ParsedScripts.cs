namespace DbMetaTool.Scripts
{
    public class ParsedScripts
    {
        public List<string> DomainScripts { get; set; } = new List<string>();
        public List<string> TableScripts { get; set; } = new List<string>();
        public List<string> ProcedureScripts { get; set; } = new List<string>();

        public int TotalCount => DomainScripts.Count + TableScripts.Count + ProcedureScripts.Count;
    }
}
