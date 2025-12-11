using Models;

namespace ConsoleApp
{
    public static class CsvLoader
    {
        public static List<AntSpecies> LoadDummyData()
        {
            // Simulamos carga de un archivo Kaggle
            return new List<AntSpecies>
            {
                new AntSpecies { ScientificName = "Messor barbarus", AntWikiUrl = "https://antwiki.org/Messor_barbarus" },
                new AntSpecies { ScientificName = "Lasius niger", AntWikiUrl = "https://antwiki.org/Lasius_niger" },
                new AntSpecies { ScientificName = "Camponotus cruentatus", AntWikiUrl = "https://antwiki.org/Camponotus_cruentatus" },
                new AntSpecies { ScientificName = "Pheidole pallidula", AntWikiUrl = "https://antwiki.org/Pheidole_pallidula" },
                new AntSpecies { ScientificName = "Crematogaster scutellaris", AntWikiUrl = "https://antwiki.org/Crematogaster_scutellaris" }
            };
        }
    }
}