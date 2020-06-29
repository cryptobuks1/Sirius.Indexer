using System.Collections.Generic;

namespace Indexer.Common.Configuration
{
    public class AppConfig
    {
        public CommonDbConfig CommonDb { get; set; }
        public RabbitMqConfig RabbitMq { get; set; }
        public IReadOnlyDictionary<string, BlockchainConfig> Blockchains { get; set; }
    }
}
