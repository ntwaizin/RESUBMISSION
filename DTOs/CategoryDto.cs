using System.ComponentModel.DataAnnotations;

namespace PROG_P2.DTOs
{
    /// <summary>Data needed to create or update a category.</summary>
    public class CategoryDTO
    {
        [Required, StringLength(50)]
        public string CategoryName { get; set; } = "";

        [Range(0.01, 999.99)]
        public decimal DistanceKM { get; set; }

        [Range(0, 99999999.99)]
        public decimal EntryFee { get; set; }
    }

    public class CategoryResponseDTO
    {
        public int CategoryID { get; set; }
        public int EventID { get; set; }
        public string CategoryName { get; set; } = "";
        public decimal DistanceKM { get; set; }
        public decimal EntryFee { get; set; }
    }
}
