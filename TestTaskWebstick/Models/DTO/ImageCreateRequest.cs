using System.ComponentModel.DataAnnotations;

namespace TestTaskWebstick.Models.DTO
{
    public class ImageCreateRequest
    {
        [Required]
        public string Url { get; set; }
    }
}
