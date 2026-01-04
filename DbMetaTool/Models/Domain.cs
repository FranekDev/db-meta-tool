namespace DbMetaTool.Models
{
    public class Domain
    {
        public string Name { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public int? Length { get; set; }

        public int? Precision { get; set; }

        public int? Scale { get; set; }

        public bool IsNullable { get; set; } = true;

        public string? DefaultValue { get; set; }
    }
}
