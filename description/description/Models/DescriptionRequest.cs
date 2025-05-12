using System.ComponentModel.DataAnnotations;

namespace description.Models
{
    public class DescriptionRequest
    {
        [Display(Name = "Website Link (optional)")]
        public string? WebsiteLink { get; set; }

        [Display(Name = "Name (optional)")]
        public string? Name { get; set; }

        [Display(Name = "City (optional)")]
        public string? City { get; set; }

        [Display(Name = "Domain of Activity (optional)")]
        public string? DomainOfActivity { get; set; }

        public string? GeneratedDescription { get; set; }

        public bool HasBusinessInfo =>
            !string.IsNullOrWhiteSpace(Name) ||
            !string.IsNullOrWhiteSpace(City) ||
            !string.IsNullOrWhiteSpace(DomainOfActivity);

        public bool HasAnyInfo =>
            !string.IsNullOrWhiteSpace(WebsiteLink) || HasBusinessInfo;
    }
}
