using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LogParser.Models;
using NLog;

namespace LogParser;

public class LogFileParser
{
    private readonly LogLevel[] _logLevels;
    private readonly DateTimeOffset _start;
    private readonly DateTimeOffset _end;
    
    private string _dateTimeMatch = @"\d{4}-\d{2}-\d{2}\s\d{2}:\d{2}:\d{2}[\.,]\d{3,4}";
    
    /// <summary>
    /// 
    /// </summary>
    /// <param name="logLevels">LogLevel, которые будут сохраняться в LogFileParseResult.</param>
    /// <param name="start">Дата начала, с которой сохраняются логи.</param>
    /// <param name="end">Дата конца, после которой не сохраняются логи.</param>
    public LogFileParser(NLog.LogLevel[] logLevels, DateTimeOffset start, DateTimeOffset end)
    {
        _logLevels = logLevels;
        _start = start;
        _end = end;
    }

    private bool IsLogWithDateTime(string fileRow)
    {
        return Regex.IsMatch(fileRow, _dateTimeMatch);
    }

    private (LogMetaData, string) GetLogMetaData(string fileRow)
    {
        Match match = Regex.Match(fileRow, _dateTimeMatch);

        LogLevel rowLogLevel = LogLevel.Trace;

        if (fileRow.Contains("Error", StringComparison.CurrentCultureIgnoreCase))
        {
            rowLogLevel = LogLevel.Error;
        }
        
        if (fileRow.Contains("Fatal", StringComparison.CurrentCultureIgnoreCase))
        {
            rowLogLevel = LogLevel.Fatal;
        }
        
        LogMetaData logMetaData = new()
        {
            LogLevel = rowLogLevel,
            DateTimeOffset = DateTimeOffset.Parse(match.Value, styles: DateTimeStyles.AdjustToUniversal)
        };

        string startContent = fileRow;
        
        return (logMetaData, startContent);
    }
    
    /// <summary>
    /// Получить очередной LogItem.
    /// </summary>
    /// <param name="fileRows">Строки файла.</param>
    /// <param name="startIndex">Индекс начала перебора строк.</param>
    /// <param name="endIndex">Индекс конца спарсенного лога (начала следующего).</param>
    /// <returns></returns>
    private LogItem? GetLogItem(string[] fileRows, int startIndex, out int endIndex)
    {
        if (startIndex >= fileRows.Length || startIndex < 0)
        {
            endIndex = -1;
            return null;
        }

        LogMetaData logMetaData = null;
        StringBuilder logContent = new();

        int i;
        
        for (i = startIndex; i < fileRows.Length; i++)
        {
            if (IsLogWithDateTime(fileRows[i]))
            {
                if (logMetaData is null)
                {
                    var logMetaDataAndStartContent = GetLogMetaData(fileRows[i]);

                    logMetaData = logMetaDataAndStartContent.Item1;
                    logContent.AppendLine(logMetaDataAndStartContent.Item2);
                }
                else
                {
                    break;
                }
            }
            else
            {
                if (logMetaData is null)
                {
                    endIndex = i + 1;
                    
                    return null;
                }
                
                logContent.AppendLine(fileRows[i]);
            }
        }

        endIndex = i;
        
        return new LogItem()
        {
            LogContent = logContent.ToString(),
            LogMetaData = logMetaData
        };
    }

    /// <summary>
    /// Считывает строки файла в LogFileParseResult.
    /// </summary>
    /// <param name="fileRows">Массив строк лог файла</param>
    /// <returns></returns>
    public LogFileParseResult Parse(string[] fileRows)
    {
        int endIndex = 0;
        int startIndex = 0;

        List<LogItem> logItems = new();
        
        while (endIndex != -1)
        {
            LogItem? logItem = GetLogItem(fileRows, startIndex, out endIndex);

            if (logItem is not null)
            {
                logItems.Add(logItem);
            }

            startIndex = endIndex;
        }

        logItems = logItems.Where(x =>
            _logLevels.Contains(x.LogMetaData.LogLevel) && x.LogMetaData.DateTimeOffset >= _start &&
            x.LogMetaData.DateTimeOffset <= _end)
            .ToList();
        
        return new LogFileParseResult()
        {
            LogItems = logItems
        };
    }
}