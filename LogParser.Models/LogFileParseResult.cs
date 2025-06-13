namespace LogParser.Models;

public class LogFileParseResult
{
    public string SourceFileName { get; set; }
    public List<LogItem> LogItems { get; set; }
}