using System;
using Swisschain.Sirius.Sdk.Primitives;

namespace Indexer.Common.ReadModel.Blockchains
{
    public sealed class Blockchain
    {
        public string Id { get; set; }
        public Protocol Protocol { get; set; }
        public string TenantId { get; set; }
        public string Name { get; set; }
        public NetworkType NetworkType { get; set; }
        public string IntegrationUrl { get; set; }
    }
}
