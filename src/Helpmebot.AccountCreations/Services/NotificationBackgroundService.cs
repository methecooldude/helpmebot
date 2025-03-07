﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="NotificationBackgroundService.cs" company="Helpmebot Development Team">
//   Helpmebot is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   Helpmebot is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with Helpmebot.  If not, see http://www.gnu.org/licenses/ .
// </copyright>
// <summary>
//   The notification service.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Helpmebot.AccountCreations.Services
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Linq;
    using System.Timers;
    using Castle.Core.Logging;
    using Helpmebot.AccountCreations.Configuration;
    using Helpmebot.AccountCreations.Services.Interfaces;
    using Helpmebot.Background;
    using Helpmebot.Model;
    using NHibernate;
    using NHibernate.Criterion;
    using NHibernate.Util;
    using Prometheus;
    using Stwalkerster.IrcClient.Interfaces;
    using ModuleConfiguration = Helpmebot.AccountCreations.Configuration.ModuleConfiguration;

    /// <summary>
    /// The notification service.
    /// </summary>
    public class NotificationBackgroundService : TimerBackgroundServiceBase, INotificationBackgroundService
    {
        private static readonly Counter NotificationsSent = Metrics.CreateCounter(
            "helpmebot_notifications_total",
            "Number of notifications sent",
            new CounterConfiguration
            {
                LabelNames = new[] {"channel"}
            });
        
        private readonly IIrcClient ircClient;
        private readonly ISession session;
        private readonly NotificationReceiverConfiguration configuration;

        /// <summary>
        /// The sync point.
        /// </summary>
        private readonly object syncLock = new object();

        public NotificationBackgroundService(
            IIrcClient ircClient,
            ILogger logger,
            ISession session,
            ModuleConfiguration configuration)
            : base(logger, configuration.Notifications.PollingInterval * 1000, configuration.Notifications.Enabled)
        {
            this.ircClient = ircClient;
            this.session = session;
            this.configuration = configuration.Notifications;
        }

        protected override void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            this.Logger.Debug("TimerOnElapsed()");

            // Check we aren't already doing a notification check.
            lock (this.syncLock)
            {
                this.Logger.Debug("Retrieving items from notification queue...");

                var transaction = this.session.BeginTransaction(IsolationLevel.RepeatableRead);
                if (!transaction.IsActive)
                {
                    this.Logger.Error("Could not start transaction in notification service");
                    return;
                }

                IList<Notification> list;

                // Get items from the notification queue
                try
                {
                    list = this.session.CreateCriteria<Notification>().Add(Restrictions.Eq(nameof(Notification.Handled), false)).List<Notification>();
                    
                    list.ForEach(
                        x =>
                        {
                            this.session.Refresh(x);
                            x.Handled = true;
                            this.session.Update(x);
                        });
                    
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    this.Logger.Error("Exception during retrieve of notifications", ex);
                    transaction.Rollback();
                    throw;
                }

                this.Logger.DebugFormat("Found {0} items.", list.Count);

                // Iterate to send them.
                foreach (var notification in list)
                {
                    List<string> destinations;

                    var notificationTypeKey = notification.Type.ToString(CultureInfo.InvariantCulture);
                    if (this.configuration.NotificationTargets.ContainsKey(notificationTypeKey))
                    {
                        destinations = this.configuration.NotificationTargets[notificationTypeKey].ToList();
                    }
                    else
                    {
                        this.Logger.ErrorFormat("Unknown notification with type '{0}' received", notificationTypeKey);
                        continue;
                    }

                    this.Logger.Debug(
                        $"Handling message {notification.Id} for {notification.Type}, dated {notification.Date:u}");

                    var sanitisedMessage = this.SanitiseMessage(notification.Text);
                    foreach (var x in destinations)
                    {
                        this.ircClient.SendMessage(x, sanitisedMessage);
                        NotificationsSent.WithLabels(x).Inc();
                    }
                }
            }
        }

        private string SanitiseMessage(string text)
        {
            return text.Replace("\r", "").Replace("\n", "");
        }
    }
}
