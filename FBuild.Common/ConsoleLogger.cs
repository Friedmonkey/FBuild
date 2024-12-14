using System;
using System.Collections.Generic;
using System.Linq;
namespace FBuild.Common;
public class ConsoleLogger : Ilogger
{
    private List<LogMessage> logMessages { get; set; } = new List<LogMessage>();
    public int Count(LogType filter) => logMessages.Where(m => filter.HasFlag(m.LogType)).Count();
    public void Clear()
    {
        logMessages.Clear();
        Console.Clear();
    }
    public void AddLog(LogType type, string message)
    {
        logMessages.Add(new(type, message));
        Console.WriteLine(logMessages.Last().ToString());
    }

    public void Refresh(LogType filter)
    {
        Console.Clear();
        foreach (var lm in logMessages.Where(m => filter.HasFlag(m.LogType)))
        {
            Console.ForegroundColor = lm.LogType switch
            {
                LogType.Info => ConsoleColor.Cyan,
                LogType.Warning => ConsoleColor.DarkYellow,
                LogType.Error => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
            Console.WriteLine(lm.ToString());
        }
    }
}