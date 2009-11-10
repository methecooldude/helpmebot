﻿using System;
using System.Collections.Generic;
using System.Text;

namespace helpmebot6.Commands.CategoryWatcherCommand
{
    class Enable : GenericCommand
    {
        public Enable( )
        {
            accessLevel = GlobalFunctions.commandAccessLevel(  );
        }

        protected override CommandResponseHandler execute( User source , string channel , string[ ] args )
        {
            Monitoring.WatcherController.Instance( ).addWatcherToChannel( args[ 0 ] , channel );
            return null;
        }
    }
}
