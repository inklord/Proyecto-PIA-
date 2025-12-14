using System;

namespace Models
{
    public class AntSpecies
    {
        public int Id { get; set; }
        public string ScientificName { get; set; } = string.Empty;
        public string? AntWikiUrl { get; set; }
        public string? PhotoUrl { get; set; }
        public string? InaturalistId { get; set; }
        public string? Description { get; set; }

        public override string ToString()
        {
            return ScientificName;
        }
    }
}
