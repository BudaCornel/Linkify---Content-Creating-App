using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http.Headers;
using flux_ai_imagegen.Models;
using System.Text;

namespace flux_ai_imagegen.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SubmitTask(FluxRequest request)
        {
            try
            {
                var apiKey = _config["Flux:ApiKey"] ?? "c025773f-38ee-49ab-a716-f7488a25de1f";
                var apiUrl = "https://api.bfl.ml/v1/flux-pro-1.1";

                var client = new HttpClient();
                var jsonRequest = JsonConvert.SerializeObject(request);

                var httpRequest = new HttpRequestMessage
                {
                    Method = HttpMethod.Post,
                    RequestUri = new Uri(apiUrl),
                    Headers = { { "X-Key", apiKey } },
                    Content = new StringContent(jsonRequest, Encoding.UTF8, "application/json")
                };

                using (var response = await client.SendAsync(httpRequest))
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"Task Submission Response: {responseContent}");

                    if (!response.IsSuccessStatusCode)
                    {
                        ViewBag.Error = $"API Error: {response.StatusCode} - {responseContent}";
                        return View("Index");
                    }

                    var taskResponse = JsonConvert.DeserializeObject<FluxApiResponse>(responseContent);
                    string taskId = taskResponse?.Id;

                    if (string.IsNullOrEmpty(taskId))
                    {
                        ViewBag.Error = "No task ID returned from the API.";
                        return View("Index");
                    }


                    return RedirectToAction("GetResult", new { id = taskId });
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unexpected error: {ex.Message}";
                return View("Index");
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetResult(string id)
        {
            try
            {
                var apiKey = _config["Flux:ApiKey"] ?? "****************";
                var apiUrl = $"https://api.bfl.ml/v1/get_result?id={id}";
                var client = new HttpClient();

                const int maxRetries = 20; 
                const int delayBetweenRetries = 500;
                int retryCount = 0;

                while (retryCount < maxRetries)
                {
                    var httpRequest = new HttpRequestMessage
                    {
                        Method = HttpMethod.Get,
                        RequestUri = new Uri(apiUrl),
                        Headers = { { "X-Key", apiKey } }
                    };

                    using (var response = await client.SendAsync(httpRequest))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();
                        Debug.WriteLine($"Result Response: {responseContent}");

                        if (!response.IsSuccessStatusCode)
                        {
                            ViewBag.Error = $"API Error: {response.StatusCode} - {responseContent}";
                            return View("Index");
                        }

                        var jsonResponse = JsonConvert.DeserializeObject<FluxApiResponse>(responseContent);

                        if (jsonResponse.Status == "Ready")
                        {
                            if (jsonResponse.Result != null && !string.IsNullOrEmpty(jsonResponse.Result.Sample))
                            {
                                ViewBag.GeneratedImageUrl = jsonResponse.Result.Sample;
                                return View("Index");
                            }

                            ViewBag.Error = "The task was ready, but no image URL was returned.";
                            return View("Index");
                        }

                        Debug.WriteLine($"Task status: {jsonResponse.Status}. Retrying...");
                    }

                    retryCount++;
                    await Task.Delay(delayBetweenRetries);
                }

                ViewBag.Error = "The task did not complete in time. Please try again later.";
                return View("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Unexpected error: {ex.Message}";
                return View("Index");
            }
        }
    }
}
