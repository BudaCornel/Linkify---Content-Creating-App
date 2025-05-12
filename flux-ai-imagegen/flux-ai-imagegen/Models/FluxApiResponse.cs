namespace flux_ai_imagegen.Models
{
    public class FluxApiResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public FluxApiResult Result { get; set; }
    }

    public class FluxApiResult
    {
        public string Sample { get; set; }
    }
}
