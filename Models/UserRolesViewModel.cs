namespace MedAppointments.Models
{
    public class UserRolesViewModel
    {
        public string UserId { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;

        public IList<string> Roles { get; set; } = new List<string>();
    }
}