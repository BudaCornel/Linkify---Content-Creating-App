namespace flux_ai_imagegen.Models
{
    public class FluxRequest
    {
        public string prompt { get; set; } = string.Empty;
        public int width { get; set; } = 1024;
        public int height { get; set; } = 768;
        public bool prompt_upsampling { get; set; } = false;
        public int? seed { get; set; } = null;
        public int safety_tolerance { get; set; } = 2;
        public string output_format { get; set; } = "jpeg";
    }
}
