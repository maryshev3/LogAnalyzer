using System.Net.Http.Headers;
using System.Text;
using LogAnalyzer.Clients;
using LogParser;
using LogParser.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LogAnalyzer.Controllers;

public class LogAnalyzer : ControllerBase
{
    private readonly GigaChatHttpClient _gigaChatHttpClient;
    
    public LogAnalyzer(GigaChatHttpClient gigaChatHttpClient)
    {
        _gigaChatHttpClient = gigaChatHttpClient;
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

            var logFileParser = new LogFileParser(
                new NLog.LogLevel[]
                {
                    NLog.LogLevel.Error,
                    NLog.LogLevel.Fatal
                },
                DateTimeOffset.MinValue,
                DateTimeOffset.MaxValue
            );
            
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

            string prompt =
                $"У меня есть несколько сервисов. В одном из них произошла ошибка. Я хочу понять возможные причины ошибки и как её решить. "
                + $" У меня есть логи некоторых сервисов, которые могут быть как связаны, так и не связаны между собой. "
                + $"Логи разделены между собой символом новой строки и \"||||\". "
                + $"Вот список логов, содержащие ошибки:\n{logsArrayString}";
            
            string responseGpt = await _gigaChatHttpClient.SendChatPromptAsync(prompt);

            return Ok(responseGpt);
        }
        
        return Ok();
    }
}