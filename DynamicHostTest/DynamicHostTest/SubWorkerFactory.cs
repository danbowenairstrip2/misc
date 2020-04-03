using Microsoft.Extensions.Logging;

namespace DynamicHostTest
{
    public class SubWorkerFactory
    {
        private readonly ILogger<SubWorker> _logger;

        public SubWorkerFactory(ILogger<SubWorker> logger)
        {
            _logger = logger;
        }

        public SubWorker GetSubWorker(string subWorkerString)
        {
            return new SubWorker(_logger, subWorkerString);
        }
    }
}