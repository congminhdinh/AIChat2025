using Microsoft.Extensions.Logging;

namespace Infrastructure.Logging
{
    public interface IAppLogger<T>
    {
        void LogInformation(string message, params object[] args);
        void LogWarning(string message, params object[] args);
        void LogError(string message, params object[] args);
        void LogError(Exception ex, string message, params object[] args);
        void LogCritical(Exception ex, string message, params object[] args);
    }

    public class LoggerAdapter<T> : IAppLogger<T>
    {
        private readonly ILogger<T> _logger;
        public LoggerAdapter(ILogger<T> logger)
        {
            _logger = logger;
        }

        public void LogCritical(Exception ex, string message, params object[] args)
        {
            _logger.LogCritical(ex, $"{typeof(T).Name} {message}", args);
        }

        public void LogError(string message, params object[] args)
        {
            _logger.LogError($"{typeof(T).Name} {message}", args);
        }

        public void LogError(Exception ex, string message, params object[] args)
        {
            _logger.LogError(ex, $"{typeof(T).Name} {message}", args);
        }

        public void LogInformation(string message, params object[] args)
        {
            _logger.LogInformation($"{typeof(T).Name} {message}", args);
        }

        public void LogWarning(string message, params object[] args)
        {
            _logger.LogWarning($"{typeof(T).Name} {message}", args);
        }
    }

}
