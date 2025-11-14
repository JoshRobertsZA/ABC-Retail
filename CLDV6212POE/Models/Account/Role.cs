using System.ComponentModel.DataAnnotations;

namespace CLDV6212POE.Models.Account
{
    public class Role
    {
        [Key]
        public Guid RoleId { get; set; }

        [Required, MaxLength(50)]
        public string RoleName { get; set; }

        public ICollection<UserRole> UserRoles { get; set; }
    }
}
