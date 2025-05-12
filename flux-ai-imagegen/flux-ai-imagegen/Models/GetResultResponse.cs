namespace flux_ai_imagegen.Models
{
    public class GetResultResponse
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public Result Result { get; set; }
    }

    public class Result
    {
        public string Image { get; set; }
    }
}
