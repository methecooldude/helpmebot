namespace Helpmebot.ChannelServices.Commands.Configuration
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using Castle.Core.Logging;
    using Helpmebot.CoreServices.Model;
    using Helpmebot.Model;
    using NDesk.Options;
    using NHibernate;
    using Stwalkerster.Bot.CommandLib.Attributes;
    using Stwalkerster.Bot.CommandLib.Commands.CommandUtilities;
    using Stwalkerster.Bot.CommandLib.Commands.CommandUtilities.Response;
    using Stwalkerster.Bot.CommandLib.Services.Interfaces;
    using Stwalkerster.IrcClient.Interfaces;
    using Stwalkerster.IrcClient.Model.Interfaces;

    [CommandInvocation("welcomer")]
    [CommandFlag(Flags.LocalConfiguration)]
    [CommandFlag(Flags.Configuration, true)]
    public class WelcomerConfigurationCommand : CommandBase
    {
        private readonly ISession databaseSession;

        public WelcomerConfigurationCommand(
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

        [SubcommandInvocation("list")]
        [Help("", "Lists all masks configured to be welcomed in this channel")]
        protected IEnumerable<CommandResponse> ListMode()
        {
            var welcomeForChannel =
                this.databaseSession.QueryOver<WelcomeUser>().Where(x => x.Channel == this.CommandSource).List();

            if (welcomeForChannel.Count == 0)
            {
                yield return new CommandResponse {Message = string.Format("Not welcoming in {0}", this.CommandSource)};
                yield break;
            }
            
            yield return new CommandResponse
            {
                Message = string.Format(
                    "Welcoming these masks to {0}: {1}",
                    this.CommandSource,
                    string.Join(" ; ", welcomeForChannel.Select(x => x.ToString())))
            };
        }

        [SubcommandInvocation("add")]
        [RequiredArguments(1)]
        [Help("[--ignore] <mask>", new[]{"Adds a mask to the welcome list for the current channel.", "Use the --ignore flag to make this an exception rule instead of a match rule."})]
        protected IEnumerable<CommandResponse> AddMode()
        {
            var exception = false;
            var opts = new OptionSet
            {
                {"ignore", x => exception = true}
            };
            var extra = opts.Parse(this.Arguments);

            this.databaseSession.BeginTransaction(IsolationLevel.RepeatableRead);
            try
            {
                var welcomeUser = new WelcomeUser
                {
                    Nick = ".*",
                    User = ".*",
                    Host = string.Join(" ", extra),
                    Account = ".*",
                    RealName = ".*",
                    Channel = this.CommandSource,
                    Exception = exception
                };

                this.databaseSession.Save(welcomeUser);
                this.databaseSession.Transaction.Commit();
                
                return new[] {new CommandResponse {Message = "Done."}};
            }
            catch (Exception e)
            {
                this.Logger.Error("Error occurred during addition of welcome mask.", e);
                
                this.databaseSession.Transaction.Rollback();

                return new[] {new CommandResponse {Message = e.Message}};
            }
        }

        [SubcommandInvocation("del")]
        [SubcommandInvocation("delete")]
        [SubcommandInvocation("remove")]
        [RequiredArguments(1)]
        [Help("[--ignore] <mask>", new[]{"Removes a mask from the welcome list for the current channel.", "Use the --ignore flag to remove an exception rule instead of a match rule."})]
        protected IEnumerable<CommandResponse> DeleteMode()
        {
            var exception = false;
            var opts = new OptionSet
            {
                {"ignore", x => exception = true}
            };
            var extra = opts.Parse(this.Arguments);
            
            this.databaseSession.BeginTransaction(IsolationLevel.RepeatableRead);
            
            try
            {
                this.Logger.Debug("Getting list of welcomeusers ready for deletion!");

                var implode = string.Join(" ", extra);

                var welcomeUsers =
                    this.databaseSession.QueryOver<WelcomeUser>()
                        .Where(
                            x => x.Exception == exception 
                                 && x.Host == implode 
                                 && x.User == ".*" 
                                 && x.Nick == ".*"
                                 && x.Account == ".*"
                                 && x.RealName == ".*"
                                 && x.Channel == this.CommandSource)
                        .List();

                this.Logger.Debug("Got list of WelcomeUsers, proceeding to Delete...");

                foreach (var welcomeUser in welcomeUsers)
                {
                    this.databaseSession.Delete(welcomeUser);
                }

                this.Logger.Debug("All done, cleaning up and sending message to IRC");

                this.databaseSession.Transaction.Commit();
                return new[] {new CommandResponse {Message = "Done."}};
            }
            catch (Exception e)
            {
                this.Logger.Error("Error occurred during addition of welcome mask.", e);

                this.databaseSession.Transaction.Rollback();
                
                return new[] {new CommandResponse {Message = e.Message}};
            }
        }

        [RequiredArguments(1)]
        [SubcommandInvocation("mode")]
        [SubcommandInvocation("override")]
        [SubcommandInvocation("overridemode")]
        [Help(
            new[] {"none", "<mode>"},
            new[]
            {
                "Sets the welcomer override mode",
                "This enables a specific override rule for the welcomer allowing a different welcome message to be used for users matching pre-defined conditions"
            })]
        protected IEnumerable<CommandResponse> WelcomerMode()
        {
            this.databaseSession.BeginTransaction(IsolationLevel.RepeatableRead);
            try
            {
                string flagName = null;

                if (this.Arguments[0] != "none")
                {
                    Channel channelAlias = null;
                    var welcomerOverride = this.databaseSession.QueryOver<WelcomerOverride>()
                        .Inner.JoinAlias(x => x.Channel, () => channelAlias)
                        .Where(x => x.ActiveFlag == this.Arguments[0])
                        .And(x => channelAlias.Name == this.CommandSource)
                        .SingleOrDefault();

                    if (welcomerOverride == null)
                    {
                        return new[]
                        {
                            new CommandResponse
                            {
                                Message = $"Unable to find welcomer override configuration with alias {this.Arguments[0]}"
                            }
                        };
                    }

                    flagName = welcomerOverride.ActiveFlag;
                }

                var channel = this.databaseSession.QueryOver<Channel>()
                    .Where(x => x.Name == this.CommandSource)
                    .SingleOrDefault();
                channel.WelcomerFlag = flagName;
                this.databaseSession.SaveOrUpdate(channel);

                this.databaseSession.Transaction.Commit();

                return new[] {new CommandResponse {Message = "Done."}};
            }
            catch (Exception e)
            {
                this.Logger.Error("Error occurred during addition of welcome mask.", e);

                this.databaseSession.Transaction.Rollback();

                return new[] {new CommandResponse {Message = e.Message}};
            }
            finally
            {
                if (this.databaseSession.Transaction != null && this.databaseSession.Transaction.IsActive)
                {
                    this.databaseSession.Transaction.Rollback();
                }
            }
        }
    }
}