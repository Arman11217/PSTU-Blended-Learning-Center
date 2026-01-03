using PBLC.Web.Models.Enums;

namespace PBLC.Web.Models
{
    public class ProfileViewModel
    {
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string FacultyName { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }
    }
}
