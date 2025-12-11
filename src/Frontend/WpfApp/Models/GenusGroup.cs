using System.Collections.Generic;
using Models = global::Models;

namespace WpfApp.Models
{
    public class GenusGroup
    {
        public string Genus { get; set; } = string.Empty;
        public List<global::Models.AntSpecies> Species { get; set; } = new();
        public string? RepresentativePhotoUrl { get; set; }
    }
}


