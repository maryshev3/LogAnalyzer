using System.Net.Http.Headers;
using System.Text;
using LogParser;
using LogParser.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogAnalyzer.Controllers;

public class LogAnalyzer : ControllerBase
{
    private async Task<HttpClient> CreateClient()
    {
        // GetClient.
        var httpClient = new HttpClient();
        
        httpClient
            .DefaultRequestHeaders
            .Accept
            .Add(new MediaTypeWithQualityHeaderValue("application/json"));
        
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://ngw.devices.sberbank.ru:9443/api/v2/oauth");
        requestMessage.Content = new FormUrlEncodedContent(new Dictionary<string, string>()
        {
            ["scope"]="GIGACHAT_API_PERS"
        });
        
        requestMessage.Headers.Add("RqUID", Guid.NewGuid().ToString());
        requestMessage.Headers.Add("Authorization", "Basic {ApiKeyGigaChat}");

        var response = await httpClient.SendAsync(requestMessage);

        string content = await response.Content.ReadAsStringAsync();
        
        string accessToken = (string)JObject.Parse(content)["access_token"];

        var resultHttpClient = new HttpClient();
        
        resultHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        resultHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        
        return resultHttpClient;
    }
    
    [HttpPost("analyze")]
    public async Task<IActionResult> Analyze(IFormFileCollection logFiles, DateTimeOffset errorTime)
    {
        List<LogFileParseResult> logFileParseResults = new(logFiles.Count);
        
        foreach (IFormFile logFile in logFiles)
        {
            using Stream stream = logFile.OpenReadStream();
            using StreamReader streamReader = new StreamReader(stream);

            List<string> lines = new();
            string currentLine = "";
            while (currentLine is not null)
            {
                currentLine = streamReader.ReadLine();
                
                if (currentLine is not null)
                    lines.Add(currentLine);
            }

            var logFileParser = new LogFileParser(new NLog.LogLevel[] { NLog.LogLevel.Error }, errorTime.AddMinutes(-2), errorTime.AddMinutes(2));

            var parseResult = logFileParser.Parse(lines.ToArray());

            logFileParseResults.Add(parseResult);
        }

        var scopedLogs = logFileParseResults
            .SelectMany(x => x.LogItems)
            .Select(x => x.LogContent)
            .ToArray();

        if (scopedLogs.Any())
        {
            string logsArrayString = String.Join("\n||||", scopedLogs);

            string requestGpt =
                $"У меня есть несколько сервисов. В одном из них произошла ошибка. Я хочу понять возможные причины ошибки и как её решить. "
                + $" У меня есть логи некоторых сервисов, которые могут быть как связаны, так и не связаны между собой. "
                + $"Логи разделены между собой символом новой строки и \"||||\". "
                + $"Вот список логов, содержащие ошибки:\n{logsArrayString}";

            var client = await CreateClient();

            var response = await client.PostAsync(
                "https://gigachat.devices.sberbank.ru/api/v1/chat/completions",
                new StringContent(
                    JsonConvert.SerializeObject(new
                    {
                        model = "GigaChat",
                        messages = new[]
                        {
                            new
                            {
                                role = "user",
                                content = requestGpt
                            }
                        },
                        stream = false,
                        repetition_penalty = 1
                    }),
                    Encoding.UTF8,
                    "application/json")
                );

            var content = await response.Content.ReadAsStringAsync();

            var responseGpt = (string)JObject.Parse(content)["choices"][0]["message"]["content"];

            return Ok(responseGpt);
        }
        
        return Ok();
    }
}