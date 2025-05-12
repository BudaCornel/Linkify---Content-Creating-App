using Microsoft.AspNetCore.Mvc;
using description.Models;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using System;
using description.Models;

namespace Description.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string _openAiApiKey;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<HomeController> logger)
        {
            _httpClientFactory = httpClientFactory;
            _openAiApiKey = configuration["OpenAI:ApiKey"] ?? throw new ArgumentNullException("OpenAI API key not found in configuration.");
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new DescriptionRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DescriptionRequest request)
        {
            _logger.LogInformation("Received request: WebsiteLink={WebsiteLink}, Name={Name}, City={City}, DomainOfActivity={DomainOfActivity}",
                request.WebsiteLink, request.Name, request.City, request.DomainOfActivity);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("ModelState is invalid.");
                ModelState.AddModelError(string.Empty, "Please correct the errors below.");
                return View(request);
            }

            if (!request.HasAnyInfo)
            {
                _logger.LogWarning("No information provided.");
                ModelState.AddModelError(string.Empty, "Please provide at least business details or a website link.");
                return View(request);
            }

            string prompt;

            if (request.HasBusinessInfo && !string.IsNullOrWhiteSpace(request.WebsiteLink))
            {
                prompt = $"Create an Instagram-worthy promotional description for a business." +
                         (string.IsNullOrWhiteSpace(request.Name) ? "" : $" Name: '{request.Name}'.") +
                         (string.IsNullOrWhiteSpace(request.City) ? "" : $" Located in {request.City}.") +
                         (string.IsNullOrWhiteSpace(request.DomainOfActivity) ? "" : $" Specializes in {request.DomainOfActivity}.") +
                         $" Additionally, refer to the website for more information: {request.WebsiteLink}." +
                         " Make it appealing, engaging, and suitable for a social media post!";
            }
            else if (request.HasBusinessInfo)
            {
                prompt = $"Create an Instagram-worthy promotional description for a business." +
                         (string.IsNullOrWhiteSpace(request.Name) ? "" : $" Name: '{request.Name}'.") +
                         (string.IsNullOrWhiteSpace(request.City) ? "" : $" Located in {request.City}.") +
                         (string.IsNullOrWhiteSpace(request.DomainOfActivity) ? "" : $" Specializes in {request.DomainOfActivity}.") +
                         " Make it appealing, engaging, and suitable for a social media post!";
            }
            else
            {
                string websiteContent = await FetchWebsiteContent(request.WebsiteLink);
                if (string.IsNullOrWhiteSpace(websiteContent))
                {
                    _logger.LogWarning("Failed to fetch or parse website content.");
                    ModelState.AddModelError(string.Empty, "Unable to retrieve information from the provided website link.");
                    return View(request);
                }

                var extractedInfo = ExtractBusinessInfo(websiteContent);
                if (string.IsNullOrWhiteSpace(extractedInfo.BusinessName))
                {
                    _logger.LogWarning("Failed to extract business name from website.");
                    ModelState.AddModelError(string.Empty, "Unable to determine the business type from the provided website.");
                    return View(request);
                }

                prompt = $"Create an Instagram-like promotional description for a business based on the following information:\n" +
                         $"Name: {extractedInfo.BusinessName}\n" +
                         $"Description: {extractedInfo.Description}\n" +
                         $"Services: {extractedInfo.Services}\n" +
                         $"Make it appealing, engaging, and suitable for a social media post.";
            }

            _logger.LogInformation("Constructed prompt: {Prompt}", prompt);

            request.GeneratedDescription = await CallOpenAiApi(prompt);

            _logger.LogInformation("Generated Description: {GeneratedDescription}", request.GeneratedDescription);

            return View(request);
        }

        private async Task<string> FetchWebsiteContent(string url)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to fetch website content. Status Code: {StatusCode}", response.StatusCode);
                    return null;
                }

                var htmlContent = await response.Content.ReadAsStringAsync();
                return htmlContent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while fetching website content.");
                return null;
            }
        }

        private (string BusinessName, string Description, string Services) ExtractBusinessInfo(string htmlContent)
        {
            try
            {
                var doc = new HtmlDocument();
                doc.LoadHtml(htmlContent);

                string businessName = doc.DocumentNode.SelectSingleNode("//title")?.InnerText.Trim() ??
                                      doc.DocumentNode.SelectSingleNode("//h1")?.InnerText.Trim() ?? "Unknown Business";

                string description = doc.DocumentNode.SelectSingleNode("//meta[@name='description']")?
                                      .GetAttributeValue("content", "").Trim() ?? "No description available.";

               
                string services = "";

                var serviceNodes = doc.DocumentNode.SelectNodes("//h2[contains(translate(text(), 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz'), 'services')]/following-sibling::ul[1]/li");
                if (serviceNodes != null)
                {
                    services = string.Join(", ", serviceNodes.Select(node => node.InnerText.Trim()));
                }
                else
                {
                    var paragraphNodes = doc.DocumentNode.SelectNodes("//p");
                    if (paragraphNodes != null)
                    {
                        services = string.Join(" ", paragraphNodes.Select(node => node.InnerText.Trim()).Take(2));
                    }
                }

                return (businessName, description, services);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred while parsing website content.");
                return ("Unknown Business", "No description available.", "No services listed.");
            }
        }

        private async Task<string> CallOpenAiApi(string prompt)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _openAiApiKey);

                var requestBody = new
                {
                    model = "gpt-3.5-turbo",
                    messages = new[]
                    {
                        new { role = "system", content = "You are a helpful assistant that crafts social media style promotional descriptions." },
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 150,
                    temperature = 0.7
                };

                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                using var response = await client.PostAsync("https://api.openai.com/v1/chat/completions", content);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("OpenAI API call failed with status code {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                    return "Error generating description. Please try again later.";
                }

                var responseJson = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseJson);
                var firstChoice = doc.RootElement.GetProperty("choices")[0];
                var message = firstChoice.GetProperty("message");
                var generatedContent = message.GetProperty("content").GetString();

                return generatedContent ?? "No description generated.";
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request to OpenAI API failed.");
                return "Error communicating with the AI service. Please try again later.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unexpected error occurred while calling OpenAI API.");
                return "An unexpected error occurred. Please try again.";
            }
        }
    }
}
