namespace Helpmebot.ChannelServices.Commands.ChannelManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Castle.Core.Logging;
    using Helpmebot.ChannelServices.Services.Interfaces;
    using Helpmebot.CoreServices.Model;
    using Helpmebot.CoreServices.Startup;
    using Stwalkerster.Bot.CommandLib.Attributes;
    using Stwalkerster.Bot.CommandLib.Commands.CommandUtilities;
    using Stwalkerster.Bot.CommandLib.Commands.CommandUtilities.Response;
    using Stwalkerster.Bot.CommandLib.Services.Interfaces;
    using Stwalkerster.IrcClient.Interfaces;
    using Stwalkerster.IrcClient.Messages;
    using Stwalkerster.IrcClient.Model.Interfaces;

    [CommandFlag(Flags.ChanOp)]
    [CommandInvocation("idleremove")]
    public class IdleRemoveCommand : CommandBase
    {
        private readonly IHelpeeManagementService helpeeManagementService;
        private readonly IModeMonitoringService modeMonitoringService;

        public IdleRemoveCommand(
            string commandSource,
            IUser user,
            IList<string> arguments,
            ILogger logger,
            IFlagService flagService,
            IConfigurationProvider configurationProvider,
            IIrcClient client,
            IHelpeeManagementService helpeeManagementService,
            IModeMonitoringService modeMonitoringService)
            : base(commandSource, user, arguments, logger, flagService, configurationProvider, client)
        {
            this.helpeeManagementService = helpeeManagementService;
            this.modeMonitoringService = modeMonitoringService;
        }

        [Help("[nickname...]", "Produces a list of who will be removed from the channel by the remove command")]
        [SubcommandInvocation("dryrun")]
        protected IEnumerable<CommandResponse> DryRun()
        {
            var removableHelpees = this.GetRemovableHelpees();

            var helpees = string.Join(", ", removableHelpees);

            yield return new CommandResponse {Message = $"Proposing removal of: {helpees}"};
        }

        [Help("[nickname...]", "Removes the listed helpees who have been inactive for more than 15 minutes by the bot's reckoning. Nicknames not eligible are automatically filtered out, but please use the dryrun subcommand to check prior to using this.")]
        [SubcommandInvocation("remove")]
        protected IEnumerable<CommandResponse> Remove()
        {
            var removableHelpees = this.GetRemovableHelpees();
            
            var channel = "#wikipedia-en-help";
            var removeMessage = "Idle; if you still need assistance editing Wikipedia, please re-join the channel";

            this.modeMonitoringService.PerformAsOperator(
                channel,
                ircClient =>
                {
                    foreach (var helpee in removableHelpees)
                    {
                        ircClient.Send(new Message("KICK", new[] {channel, helpee, removeMessage}));
                    }
                });

            return null;
        }

        private List<string> GetRemovableHelpees()
        {
            var removableHelpees = this.helpeeManagementService.Helpees
                .Where(
                    x =>
                    {
                        if (x.Value == DateTime.MinValue)
                        {
                            if ((DateTime.UtcNow - Launcher.StartupTime).TotalMinutes > 15)
                            {
                                return true;
                            }
                        }

                        if ((DateTime.UtcNow - x.Value).TotalMinutes > 15)
                        {
                            return true;
                        }

                        return false;
                    })
                .Select(x => x.Key.Nickname)
                .Intersect(this.Arguments)
                .ToList();
            return removableHelpees;
        }
    }
}