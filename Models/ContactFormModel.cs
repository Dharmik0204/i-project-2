using System.ComponentModel.DataAnnotations;

namespace AerodyneCompressors.Models
{
    public class ContactFormModel
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter your name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [Phone]
        public string Phone { get; set; } = string.Empty;

        public string? Company { get; set; }

        [Required(ErrorMessage = "Subject is required")]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your question/message")]
        [Display(Name = "Question")]
        public string Question { get; set; } = string.Empty;
    }
}
