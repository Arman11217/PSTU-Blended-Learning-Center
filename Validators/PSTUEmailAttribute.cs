using System.ComponentModel.DataAnnotations;

namespace PBLC.Web.Validators
{
    public class PSTUEmailAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
            {
                return ValidationResult.Success; // Let [Required] handle null/empty
            }

            var email = value.ToString();
            
            // Check if email ends with pstu.ac.bd (allowing subdomains like @cse.pstu.ac.bd, @esdm.pstu.ac.bd, etc.)
            if (!email!.EndsWith("pstu.ac.bd", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult("Only PSTU email addresses (e.g., @pstu.ac.bd, @cse.pstu.ac.bd) are allowed.");
            }

            // Ensure there's an @ sign before pstu.ac.bd
            var atIndex = email.LastIndexOf('@');
            if (atIndex < 0 || !email.Substring(atIndex + 1).EndsWith("pstu.ac.bd", StringComparison.OrdinalIgnoreCase))
            {
                return new ValidationResult("Only PSTU email addresses (e.g., @pstu.ac.bd, @cse.pstu.ac.bd) are allowed.");
            }

            return ValidationResult.Success;
        }
    }
}
