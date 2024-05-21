using TryFirst.Models.DTO;

namespace TryFirst.Data
{
    public static class VillaStore
    {
        public static List<VillaDTO> VillaList = new List<VillaDTO>
        {
            new VillaDTO { Id = 1, Name = "FirstVilla", Sqft = 300 },
            new VillaDTO { Id = 2, Name = "Beach", Sqft = 400 }
        };
     }
}
