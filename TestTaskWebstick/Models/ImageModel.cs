using System.ComponentModel.DataAnnotations;

namespace TestTaskWebstick.Models
{
    public class ImageModel
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public string Url { get; set; }
    }
}
