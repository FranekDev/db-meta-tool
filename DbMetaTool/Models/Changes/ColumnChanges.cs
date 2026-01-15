using System.Collections.Generic;

namespace DbMetaTool.Models.Changes
{
    public class ColumnChanges
    {
        public List<Column> ColumnsToAdd { get; } = new List<Column>();
        public List<(string Name, List<string> AlterStatements)> ColumnsToAlter { get; } = new List<(string, List<string>)>();

        public bool HasChanges => ColumnsToAdd.Count > 0 || ColumnsToAlter.Count > 0;
    }
}
