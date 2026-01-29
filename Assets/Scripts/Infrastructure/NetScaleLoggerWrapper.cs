using System;
using Microsoft.Extensions.Logging;
using UnityEngine;

namespace Infrastructure
{
    public class NetScaleLoggerWrapper : Microsoft.Extensions.Logging.ILogger
    {
        public NetScaleLoggerWrapper()
        {
        }

        public IDisposable BeginScope<TState>(TState state)
        {
            return NoopDisposable.Instance;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);

            if (exception != null)
            {
                message += Environment.NewLine + exception;
            }

            switch (logLevel)
            {
                case LogLevel.Trace:
                    Debug.Log(message);
                    break;
                case LogLevel.Debug:
                    Debug.Log(message);
                    break;
                case LogLevel.Information:
                    Debug.Log(message);
                    break;
                case LogLevel.Warning:
                    Debug.LogWarning(message);
                    break;
                case LogLevel.Error:
                    Debug.LogError(message);
                    break;
                case LogLevel.Critical:
                    Debug.LogError(message);
                    break;
                case LogLevel.None:
                    throw new ArgumentOutOfRangeException("logLevel", logLevel, logLevel.ToString());
            }
        }

        private class NoopDisposable : IDisposable
        {
            public static readonly NoopDisposable Instance = new NoopDisposable();

            private NoopDisposable()
            {
            }

            public void Dispose()
            {
            }
        }
    }
}