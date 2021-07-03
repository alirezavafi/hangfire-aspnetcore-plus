using Hangfire.Console.Extensions.Serilog;
using Serilog;
using Serilog.Core;
using Serilog.Events;

namespace Hangfire.Logging
{
  public class HangfireSerilogLogger : IHangfireLogger
    {
        private readonly ILogger _defaultLogger;
        private Logger _hangfireLogger;

        public HangfireSerilogLogger(ILogger defaultLogger)
        {
            _defaultLogger = defaultLogger;
            _hangfireLogger = new LoggerConfiguration()
                .WriteTo.Hangfire()
                .Enrich.WithHangfireContext()
                .CreateLogger();
        }
        
        public void Write(LogEvent logEvent)
        {
            _hangfireLogger.Write(logEvent);
            _defaultLogger.Write(logEvent);
        }
    }
}