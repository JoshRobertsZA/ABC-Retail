using System.ComponentModel.DataAnnotations;

namespace CLDV6212POE.Models.Account
{
    public class User
    {
        [Key]
        public Guid UserId { get; set; }

        [Required, MaxLength(100)]
        public string Fullname { get; set; }

        [Required, MaxLength(100)]
        public string Email { get; set; }

        [Required, MaxLength(255)]
        public string Password { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}
