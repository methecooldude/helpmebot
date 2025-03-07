﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WelcomeUser.cs" company="Helpmebot Development Team">
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
//   The welcome user.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Helpmebot.Model
{
    using System.Text;
    using Helpmebot.Persistence;

    /// <summary>
    /// The welcome user.
    /// </summary>
    public class WelcomeUser : EntityBase
    {
        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        public virtual string Channel { get; set; }

        /// <summary>
        /// Gets or sets the nick.
        /// </summary>
        public virtual string Nick { get; set; }

        /// <summary>
        /// Gets or sets the user.
        /// </summary>
        public virtual string User { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        public virtual string Host { get; set; }
        
        public virtual string Account { get; set; }
        
        public virtual string RealName { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exception.
        /// </summary>
        public virtual bool Exception { get; set; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var sb = new StringBuilder();
            if (this.Nick != ".*")
            {
                sb.AppendFormat(" Nick: {0}", this.Nick);
            }
            if (this.User != ".*")
            {
                sb.AppendFormat(" User: {0}", this.User);
            }
            if (this.Host != ".*")
            {
                sb.AppendFormat(" Host: {0}", this.Host);
            }
            if (this.Account != ".*")
            {
                sb.AppendFormat(" Account: {0}", this.Account);
            }
            if (this.RealName != ".*")
            {
                sb.AppendFormat(" RealName: {0}", this.RealName);
            }

            return string.Format("{0} {1}", sb.ToString().TrimStart(), this.Exception ? "(!)" : string.Empty);
        }
    }
}
