using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogAnalyzer.Clients;

public class GigaChatHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    private readonly string _apiKey;
    private readonly Uri _authUrl;
    private readonly Uri _charPromptUrl;
    
    public GigaChatHttpClient(
        HttpClient httpClient,
        IConfiguration configuration
    )
    {
        _httpClient = httpClient;
        _configuration = configuration;

        _apiKey = _configuration.GetValue<string>("GigaChatOptions:ApiKey");
        _authUrl = _configuration.GetValue<Uri>("GigaChatOptions:AuthUrl");
        _charPromptUrl = _configuration.GetValue<Uri>("GigaChatOptions:ChatPromptUrl");
    }

    public async Task UpdateAuthorizationTokenAsync()
    {
        HttpRequestMessage requestMessage = new(
            HttpMethod.Post,
            _authUrl
        );
        
        requestMessage.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["scope"]="GIGACHAT_API_PERS"
        });
        
        requestMessage.Headers.Add("RqUID", Guid.NewGuid().ToString());
        requestMessage.Headers.Add("Authorization", $"Basic {_apiKey}");
        
        HttpResponseMessage response = await _httpClient.SendAsync(requestMessage);

        string content = await response.Content.ReadAsStringAsync();
        
        string accessToken = (string)JObject.Parse(content)["access_token"];
        
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            accessToken
        );
    }

    private async Task<HttpResponseMessage> PostChatPromptAsync(string prompt)
    {
        return await _httpClient.PostAsync(
            _charPromptUrl,
            new StringContent(
                JsonConvert.SerializeObject(new
                {
                    model = "GigaChat",
                    messages = new[]
                    {
                        new
                        {
                            role = "user",
                            content = prompt
                        }
                    },
                    stream = false,
                    repetition_penalty = 1
                }),
                Encoding.UTF8,
                "application/json"
            )
        );
    }
    
    public async Task<string> SendChatPromptAsync(string prompt)
    {
        HttpResponseMessage response = await PostChatPromptAsync(prompt);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            await UpdateAuthorizationTokenAsync();
            
            response = await PostChatPromptAsync(prompt);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new UnauthorizedAccessException();
            }
        }

        if (response.StatusCode == HttpStatusCode.RequestEntityTooLarge)
        {
            throw new ArgumentException("В логах содержится слишком много ошибок." +
                " Либо сократите файл с логами, либо уменьшите количество ошибок в нем.");
        }

        string content = await response.Content.ReadAsStringAsync();

        string responseGpt = (string)JObject.Parse(content)["choices"][0]["message"]["content"];

        return responseGpt;
    }
}