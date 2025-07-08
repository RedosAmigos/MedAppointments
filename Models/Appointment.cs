using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

public enum AppointmentStatus
{
    Pending,
    Approved,
    Rejected
}

public class Appointment
{
    public int Id { get; set; }

    [Required, StringLength(100)]
    [Display(Name = "Nom du patient")]
    public string PatientName { get; set; } = string.Empty;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Date du rendez‑vous")]
    public DateOnly Date { get; set; }

    [Required]
    [DataType(DataType.Time)]
    [DateTimeNotInPast]
    [Display(Name = "Heure du rendez‑vous")]
    public TimeOnly Time { get; set; }

    public string? UserId { get; set; }
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    [Display(Name = "Statut")]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;

    public class DateTimeNotInPastAttribute : ValidationAttribute
    {
        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var appointment = (Appointment)validationContext.ObjectInstance;
            var selectedDateTime = appointment.Date.ToDateTime(appointment.Time);

            if (selectedDateTime < DateTime.Now)
            {
                return new ValidationResult("La date et l'heure ne peuvent pas être dans le passé.");
            }

            return ValidationResult.Success;
        }
    }
}
