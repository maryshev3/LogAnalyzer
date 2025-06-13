namespace LogParser.Models;

public class ParseAndAnalyzeResult
{
    public List<LogFileParseResult> LogFileParseResult { get; set; }
    public string AnalyzeResult { get; set; }
}