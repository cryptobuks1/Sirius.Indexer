using System.Collections.Generic;

namespace Indexer.Common.Telemetry
{
    public sealed class AppInsightOptions
    {
        internal string InstrumentationKey { get; private set; }
        internal IDictionary<string, string> DefaultProperties { get; private set; }
        
        public void SetInstrumentationKey(string instrumentationKey)
        {
            InstrumentationKey = instrumentationKey;
        }

        public void AddDefaultProperty(string name, string value)
        {
            if (DefaultProperties == null)
            {
                DefaultProperties = new Dictionary<string, string>();
            }

            DefaultProperties.Add(name, value);
        }
    }
}
