using System.Collections.Generic;

namespace DbMetaTool.Models.Changes
{
    public class TableChanges
    {
        public List<Table> TablesToCreate { get; } = new List<Table>();
        public List<(string Name, ColumnChanges Changes)> TablesToAlter { get; } = new List<(string, ColumnChanges)>();
    }
}
