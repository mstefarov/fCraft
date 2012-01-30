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


        /// <summary> Checks whether two IP addresses are in the same mask-defined range. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr or otherAddr is null. </exception>
        public static bool Match( [NotNull] this IPAddress thisAddr, [NotNull] IPAddress otherAddr, uint mask ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( otherAddr == null ) throw new ArgumentNullException( "otherAddr" );
            uint thisAsUInt = thisAddr.AsUInt();
            uint otherAsUInt = otherAddr.AsUInt();
            return (thisAsUInt & mask) == (otherAsUInt & mask);
        }

        /// <summary> Checks whether two IP addresses are in the same mask-defined range. </summary>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        internal static bool Match( [NotNull] this IPAddress thisAddr, uint otherAddr, uint mask ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            uint thisAsUInt = thisAddr.AsUInt();
            return (thisAsUInt & mask) == (otherAddr & mask);
        }



        /// <summary> Finds the first IPv4 address in the given range. </summary>
        /// <param name="thisAddr"> Base address for the range. </param>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If range byte is not in valid range. </exception>
        public static IPAddress FirstIAddressInRange( [NotNull] this IPAddress thisAddr, byte range ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)NetMask( range );
            return new IPAddress( IPAddress.HostToNetworkOrder( thisAsInt & mask ) );
        }


        /// <summary> Finds the last IP address in the given range. </summary>
        /// <param name="thisAddr"> Base address for the range. </param>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <exception cref="ArgumentNullException"> If thisAddr is null. </exception>
        /// <exception cref="ArgumentOutOfRangeException"> If range byte is not in valid range. </exception>
        public static IPAddress LastAddressInRange( [NotNull] this IPAddress thisAddr, byte range ) {
            if( thisAddr == null ) throw new ArgumentNullException( "thisAddr" );
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)~NetMask( range );
            return new IPAddress( (uint)IPAddress.HostToNetworkOrder( thisAsInt | mask ) );
        }


        /// <summary> Creates a mask for given range. </summary>
        /// <param name="range"> CIDR range byte (0-32). </param>
        /// <exception cref="ArgumentOutOfRangeException"> If range byte is not in valid range. </exception>
        public static uint NetMask( byte range ) {
            if( range > 32 ) throw new ArgumentOutOfRangeException( "range" );
            if( range == 0 ) {
                return 0;
            } else {
                return 0xffffffff << (32 - range);
            }
        }
    }
}