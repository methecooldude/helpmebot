// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Helpmebot.AccountCreations.Configuration
{
    using System.Collections.Generic;

    public class NotificationReceiverConfiguration
    {
        public bool Enabled { get; set; }

        public IDictionary<string, IList<string>> NotificationTargets { get; set; }

        public int PollingInterval { get; set; }
    }
}