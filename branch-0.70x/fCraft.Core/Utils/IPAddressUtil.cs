// Copyright 2009-2012 Matvei Stefarov <me@matvei.org>
using System;
using System.Net;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility methods for working with IP addresses and ranges. </summary>
    public static class IPAddressUtil {
        static readonly Regex RegexIP = new Regex( @"\b(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\b",
                                                   RegexOptions.Compiled );


        /// <summary> Checks to see if the specified IP, is a valid IPv4 address. </summary>
        /// <param name="ipString"> IPv4 address to verify. </param>
        /// <returns> Whether or not the IP is a valid IPv4 address. </returns>
        public static bool IsIP( [NotNull] string ipString ) {
            if( ipString == null ) throw new ArgumentNullException( "ipString" );
            return RegexIP.IsMatch( ipString );
        }


        /// <summary> Checks whether an IP address may belong to LAN or localhost (192.168.0.0/16, 10.0.0.0/24, or 127.0.0.0/24). </summary>
        /// <param name="addr"> IPv4 address to check. </param>
        /// <returns> True if given IP is local; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> If addr is null. </exception>
        public static bool IsLocal( [NotNull] this IPAddress addr ) {
            if( addr == null ) throw new ArgumentNullException( "addr" );
            byte[] bytes = addr.GetAddressBytes();
            return (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 10) ||
                   (bytes[0] == 127);
        }


        /// <summary> Represents an IPv4 address as an integer. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        public static int AsInt( [NotNull] this IPAddress thisAddr ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            return IPAddress.HostToNetworkOrder( BitConverter.ToInt32( thisAddr.GetAddressBytes(), 0 ) );
        }


        /// <summary> Represents an IPv4 address as an unsigned integer. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        public static uint AsUInt( [NotNull] this IPAddress thisAddr ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            return (uint)IPAddress.HostToNetworkOrder( BitConverter.ToInt32( thisAddr.GetAddressBytes(), 0 ) );
        }
    }
}