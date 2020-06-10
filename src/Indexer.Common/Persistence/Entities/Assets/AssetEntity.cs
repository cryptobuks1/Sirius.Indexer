namespace Indexer.Common.Persistence.Entities.Assets
{
    internal sealed class AssetEntity
    {
        // ReSharper disable InconsistentNaming
        public long id { get; set; }
        public string symbol { get; set; }
        public string address { get; set; }
        public int accuracy { get; set; }
    }
}
