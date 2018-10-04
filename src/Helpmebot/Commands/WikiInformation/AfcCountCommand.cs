namespace Helpmebot.Commands.WikiInformation
{
    using System.Collections.Generic;
    using Castle.Core.Logging;
    using Helpmebot.ExtensionMethods;
    using Helpmebot.Model;
    using NHibernate;
    using Stwalkerster.Bot.CommandLib.Attributes;
    using Stwalkerster.Bot.CommandLib.Commands.CommandUtilities;
    using Stwalkerster.Bot.CommandLib.Commands.CommandUtilities.Response;
    using Stwalkerster.Bot.CommandLib.Services.Interfaces;
    using Stwalkerster.IrcClient.Interfaces;
    using Stwalkerster.IrcClient.Model.Interfaces;

    [CommandFlag(Flags.Info)]
    [CommandInvocation("afccount")]
    public class AfcCountCommand : CommandBase
    {
        private readonly ISession databaseSession;

        public AfcCountCommand(
            string commandSource,
            IUser user,
            IList<string> arguments,
            ILogger logger,
            IFlagService flagService,
            IConfigurationProvider configurationProvider,
            IIrcClient client,
            ISession databaseSession) : base(
            commandSource,
            user,
            arguments,
            logger,
            flagService,
            configurationProvider,
            client)
        {
            this.databaseSession = databaseSession;
        }

        [Help("", "Returns the number of AfC submissions awaiting review")]
        protected override IEnumerable<CommandResponse> Execute()
        {
            var categoryName = "Pending AfC submissions";
            var mediaWikiSite = this.databaseSession.GetMediaWikiSiteObject(this.CommandSource);
            var categorySize = mediaWikiSite.GetCategorySize(categoryName);
            
            return new[]
            {
                new CommandResponse
                {
                    Message = string.Format("[[Category:{0}]] has {1} items", categoryName, categorySize)
                }
            };
        }
    }
}