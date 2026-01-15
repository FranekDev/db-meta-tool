using System.Collections.Generic;

namespace DbMetaTool.Models.Changes
{
    public class DomainChanges
    {
        public List<Domain> DomainsToCreate { get; } = new List<Domain>();
        public List<(string Name, List<string> AlterStatements)> DomainsToAlter { get; } = new List<(string, List<string>)>();
    }
}
