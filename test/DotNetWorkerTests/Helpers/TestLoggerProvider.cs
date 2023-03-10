// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Functions.Worker.Tests
{
    public class TestLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider;
        private static Lazy<ILoggerFactory> _lazyFactory;

        static TestLoggerProvider()
        {
            _lazyFactory = new Lazy<ILoggerFactory>(() =>
            {
                return new ServiceCollection()
                            .AddLogging(b =>
                            {
                                b.AddProvider(new TestLoggerProvider());
                            })
                            .BuildServiceProvider()
                            .GetService<ILoggerFactory>();
            });
        }

        private ConcurrentDictionary<string, TestLogger> LoggerCache { get; } = new ConcurrentDictionary<string, TestLogger>();

        public static ILoggerFactory Factory => _lazyFactory.Value;

        public IEnumerable<TestLogger> CreatedLoggers => LoggerCache.Values;

        public ILogger CreateLogger(string categoryName)
        {
            return LoggerCache.GetOrAdd(categoryName, (key) => new TestLogger(key, _scopeProvider));
        }

        public IList<LogMessage> GetAllLogMessages()
        {
            return CreatedLoggers.SelectMany(l => l.GetLogMessages()).OrderBy(p => p.Timestamp).ToList();
        }

        /// <summary>
        /// Returns a single string that contains all log message strings on a separate line.
        /// </summary>
        /// <returns>The log message.</returns>
        public string GetLog()
        {
            return string.Join(Environment.NewLine, GetAllLogMessages());
        }

        public void ClearAllLogMessages()
        {
            foreach (TestLogger logger in CreatedLoggers)
            {
                logger.ClearLogMessages();
            }
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Dispose()
        {
        }
    }
}
