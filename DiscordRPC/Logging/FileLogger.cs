namespace DiscordRPC.Logging;

/// <summary>
///     Logs the outputs to a file
/// </summary>
public class FileLogger : ILogger
{
    private readonly object _fileLock;

    /// <summary>
    ///     Creates a new instance of the file logger
    /// </summary>
    /// <param name="path">The path of the log file.</param>
    /// <param name="level">The level to assign to the logger.</param>
    public FileLogger(string path, LogLevel level = LogLevel.Info)
    {
        Level = level;
        File = path;
        _fileLock = new object();
    }

    /// <summary>
    ///     Should the output be coloured?
    /// </summary>
    private string File { get; }

    /// <summary>
    ///     The level of logging to apply to this logger.
    /// </summary>
    public LogLevel Level { get; set; }


    /// <summary>
    ///     Informative log messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public void Trace(string message, params object[] args)
    {
        if (Level > LogLevel.Trace) return;
        lock (_fileLock)
        {
            System.IO.File.AppendAllText(File,
                "\r\nTRCE: " + (args.Length > 0 ? string.Format(message, args) : message));
        }
    }

    /// <summary>
    ///     Informative log messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public void Info(string message, params object[] args)
    {
        if (Level > LogLevel.Info) return;
        lock (_fileLock)
        {
            System.IO.File.AppendAllText(File,
                "\r\nINFO: " + (args.Length > 0 ? string.Format(message, args) : message));
        }
    }

    /// <summary>
    ///     Warning log messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public void Warning(string message, params object[] args)
    {
        if (Level > LogLevel.Warning) return;
        lock (_fileLock)
        {
            System.IO.File.AppendAllText(File,
                "\r\nWARN: " + (args.Length > 0 ? string.Format(message, args) : message));
        }
    }

    /// <summary>
    ///     Error log messages
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    public void Error(string message, params object[] args)
    {
        if (Level > LogLevel.Error) return;
        lock (_fileLock)
        {
            System.IO.File.AppendAllText(File,
                "\r\nERR : " + (args.Length > 0 ? string.Format(message, args) : message));
        }
    }
}