using System.ComponentModel.DataAnnotations;

namespace Recipe_Sharing_Platform_API.Models
{
    public class User
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(30)]
        public string Username { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;  

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        public ICollection<Receipt>? Receipts { get; set; }
        public ICollection<Like>? Likes { get; set; }
    }
}