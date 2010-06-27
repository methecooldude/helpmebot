﻿#region Usings
using System.Reflection;

#endregion

namespace helpmebot6.Commands
{
    internal class Oauth : GenericCommand
    {
        protected override CommandResponseHandler execute(User source, string channel, string[] args)
        {
            Logger.instance( ).addToLog(
                "Method:" + MethodBase.GetCurrentMethod( ).DeclaringType.Name + MethodBase.GetCurrentMethod( ).Name,
                Logger.LogTypes.DNWB );


            if ( args.Length == 1  && args[0] != "")
            {
                Helpmebot6.twitter.authorise( args[ 0 ] );
            }
            else
            {
                Helpmebot6.twitter = new Twitter( Configuration.singleton( )[ "twitterConsumerKey" ],
                                                  Configuration.singleton( )[ "twitterConsumerSecret" ] );
            }

            return new CommandResponseHandler( Configuration.singleton( ).getMessage( "done" ) );
        }
    }
}