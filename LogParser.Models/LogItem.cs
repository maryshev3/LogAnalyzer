using NLog;

namespace LogParser.Models;

public class LogMetaData
{
    public LogLevel LogLevel { get; set; }
    public DateTimeOffset DateTimeOffset { get; set; }
}

public class LogItem
{
    public LogMetaData LogMetaData { get; set; }
    public string LogContent { get; set; }
}