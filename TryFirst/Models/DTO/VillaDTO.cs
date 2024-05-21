using System.ComponentModel.DataAnnotations;

namespace TryFirst.Models.DTO
{
    public class VillaDTO
    {
        public int Id { get; set; }
        [Required]
        [MinLength(6)]
        [MaxLength(30)]
        public string Name { get; set; }

        public int Sqft { get; set; }

    }
}
