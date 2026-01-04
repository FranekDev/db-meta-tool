namespace DbMetaTool.Models
{
    public class Procedure
    {
        public string Name { get; set; } = string.Empty;

        public string SourceCode { get; set; } = string.Empty;

        public string? Description { get; set; }
    }
}
