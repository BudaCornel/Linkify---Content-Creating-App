using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using MyLumaApp.Models;

namespace MyLumaApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public HomeController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> CreateVideo(GenerationRequestModel model)
        {
            if (string.IsNullOrEmpty(model.Prompt) || string.IsNullOrEmpty(model.ImageUrl))
            {
                ModelState.AddModelError("", "Prompt and ImageUrl are required.");
                return View("Index", model);
            }

            var requestBody = new
            {
                prompt = model.Prompt,
                keyframes = new
                {
                    frame0 = new
                    {
                        type = "image",
                        url = model.ImageUrl
                    }
                }
            };

            var httpClient = _httpClientFactory.CreateClient();
            var jsonContent = new StringContent(
                JsonSerializer.Serialize(requestBody),
                Encoding.UTF8,
                "application/json"
            );

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.lumalabs.ai/dream-machine/v1/generations");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("authorization", "Bearer luma-c6ae9a6c-a725-41ce-b262-851706a32d6e-34ebf5aa-41fd-40d7-a177-d5612b61ee56"); // Replace with your actual API key
            request.Content = jsonContent;

            var response = await httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Error initiating generation. Check your API key and parameters.");
                return View("Index", model);
            }

            var responseContent = await response.Content.ReadAsStringAsync();
            var genResponse = JsonSerializer.Deserialize<GenerationResponseModel>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Console.WriteLine(responseContent);
            TempData["GenerationId"] = genResponse.Id;
            TempData["Prompt"] = model.Prompt;
            TempData["ImageUrl"] = model.ImageUrl;

            ViewBag.Message = "Generation started. Click 'Check Status' to see if it's done.";
            return View("Index");
        }
        [HttpPost]
        public async Task<IActionResult> CheckStatus()
        {
            if (!TempData.ContainsKey("GenerationId"))
            {

                return RedirectToAction("Index");
            }

            var generationId = TempData["GenerationId"]?.ToString();
            var prompt = TempData["Prompt"]?.ToString();
            var imageUrl = TempData["ImageUrl"]?.ToString();


            TempData["GenerationId"] = generationId;
            TempData["Prompt"] = prompt;
            TempData["ImageUrl"] = imageUrl;

            var httpClient = _httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.lumalabs.ai/dream-machine/v1/generations/{generationId}");
            request.Headers.Add("accept", "application/json");
            request.Headers.Add("authorization", "Bearer******************"); 

            var response = await httpClient.SendAsync(request);
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseContent);

            if (!response.IsSuccessStatusCode)
            {
                ViewBag.Error = "Error retrieving generation status. Check your API key and GenerationId.";
                return View("Index");
            }

            var statusResponse = JsonSerializer.Deserialize<GenerationStatusResponseModel>(
                responseContent,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            if (statusResponse.State == "completed" && !string.IsNullOrEmpty(statusResponse.Assets?.Video))
            {
                return View("Result", statusResponse);
            }
            else
            {
                ViewBag.Message = "Video is not ready yet. Please wait and check again.";
                return View("Index");
            }
        }

    }
}
