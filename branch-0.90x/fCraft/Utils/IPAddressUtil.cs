// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Net;
using System.Text.RegularExpressions;
using JetBrains.Annotations;

namespace fCraft {
    /// <summary> Provides utility methods for working with IP addresses and ranges. </summary>
    public static class IPAddressUtil {
        static readonly Regex RegexIP = new Regex(@"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$", RegexOptions.Compiled);


        /// <summary> Checks whether an IP address may belong to LAN or localhost (192.168.0.0/16, 10.0.0.0/24, or 127.0.0.0/24). </summary>
        /// <param name="addr"> IPv4 address to check. </param>
        /// <returns> True if given IP is local; otherwise false. </returns>
        /// <exception cref="ArgumentNullException"> addr is null. </exception>
        public static bool IsLocal([NotNull] this IPAddress addr) {
            if (addr == null) throw new ArgumentNullException("addr");
            byte[] bytes = addr.GetAddressBytes();
            return (bytes[0] == 192 && bytes[1] == 168) ||
                   (bytes[0] == 10) ||
                   (bytes[0] == 127);
        }


        /// <summary> Represents an IPv4 address as an unsigned integer. </summary>
        /// <exception cref="ArgumentNullException"> thisAddr is null. </exception>
        public static uint AsUInt([NotNull] this IPAddress thisAddr) {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            return (uint)IPAddress.HostToNetworkOrder(BitConverter.ToInt32(thisAddr.GetAddressBytes(), 0));
        }


        /// <summary> Represents an IPv4 address as a signed integer. </summary>
        /// <exception cref="ArgumentNullException"> thisAddr is null. </exception>
        public static int AsInt([NotNull] this IPAddress thisAddr) {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            return IPAddress.HostToNetworkOrder(BitConverter.ToInt32(thisAddr.GetAddressBytes(), 0));
        }


        /// <summary> Checks to see if the specified string is a valid IPv4 address. </summary>
        /// <param name="ipString"> String representation of the IPv4 address. </param>
        /// <returns> Whether or not the string is a valid IPv4 address. </returns>
        public static bool IsIP([NotNull] string ipString) {
            if (ipString == null) throw new ArgumentNullException("ipString");
            return RegexIP.IsMatch(ipString);
        }


        internal static bool Match([NotNull] this IPAddress thisAddr, uint otherAddr, uint mask) {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            uint thisAsUInt = thisAddr.AsUInt();
            return (thisAsUInt & mask) == (otherAddr & mask);
        }


        /// <summary> Finds the starting IPv4 address of the given address range. </summary>
        /// <exception cref="ArgumentNullException"> thisAddr is null </exception>
        /// <exception cref="ArgumentOutOfRangeException"> range is over 32 </exception>
        [NotNull]
        public static IPAddress RangeMin([NotNull] this IPAddress thisAddr, byte range) {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            if (range > 32) throw new ArgumentOutOfRangeException("range");
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)NetMask(range);
            return new IPAddress((uint)IPAddress.HostToNetworkOrder(thisAsInt & mask));
        }


        /// <summary> Finds the ending IPv4 address of the given address range. </summary>
        /// <exception cref="ArgumentNullException"> thisAddr is null </exception>
        /// <exception cref="ArgumentOutOfRangeException"> range is over 32 </exception>
        [NotNull]
        public static IPAddress RangeMax([NotNull] this IPAddress thisAddr, byte range) {
            if (thisAddr == null) throw new ArgumentNullException("thisAddr");
            if (range > 32) throw new ArgumentOutOfRangeException("range");
            int thisAsInt = thisAddr.AsInt();
            int mask = (int)~NetMask(range);
            return new IPAddress((uint)IPAddress.HostToNetworkOrder(thisAsInt | mask));
        }


        /// <summary> Creates an IPv4 mask for the given CIDR range. </summary>
        /// <exception cref="ArgumentOutOfRangeException"> range is over 32 </exception>
        public static uint NetMask(byte range) {
            if (range > 32) throw new ArgumentOutOfRangeException("range");
            if (range == 0) {
                return 0;
            } else {
                return 0xffffffff << (32 - range);
            }
        }
    }
}
