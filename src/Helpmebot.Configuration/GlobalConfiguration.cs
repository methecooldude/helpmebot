namespace Helpmebot.Configuration
{
    using System.Collections.Generic;

    public class GlobalConfiguration
    {
        public BotConfiguration General { get; set; }
        public DatabaseConfiguration Database { get; set; }
        public IrcConfiguration Irc { get; set; }
        
        public List<LoadableModuleConfiguration> Modules { get; set; }
        
        public MediaWikiDocumentationConfiguration Documentation { get; set; }
        public WikimediaUrlShortnerConfiguration WikimediaShortener { get; set; }
        
        public CommandOverrideConfiguration CommandOverrides { get; set; }
    }
}