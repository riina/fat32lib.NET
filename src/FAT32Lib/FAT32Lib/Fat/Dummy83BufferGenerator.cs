/*
 * This library (fat32lib.NET) is a port of the library fat32lib obtained from
 * https://android.googlesource.com/platform/external/fat32lib/
 * The original license for this file is replicated below.
 * 
 * Copyright (C) 2012 Google, Inc.
 *
 * This library is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation; either version 2.1 of the License, or
 * (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library; If not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.Text;

namespace FAT32Lib.Fat {

    /// <summary>
    /// Generates dummy 8.3 buffers that are associated with the long names.
    /// </summary>
    public sealed class Dummy83BufferGenerator {
        private readonly Random mRandom;

        /// <summary>
        /// Creates a new instance of <see cref="Dummy83BufferGenerator"/> that uses
        /// randomness only to avoid short name collisions.
        /// </summary>
        public Dummy83BufferGenerator() {
            mRandom = new Random();
        }

        /// <summary>
        /// Its in the DOS manual!(DOS 5: page 72) Valid: A..Z 0..9 _ ^ $ ~ ! # % &amp; - {} () @ ' `
        /// Invalid: spaces/periods,
        /// </summary>
        /// <param name="toTest"></param>
        /// <returns></returns>
        public static bool ValidChar(char toTest) {
            if (toTest >= 'A' && toTest <= 'Z') return true;
            if (toTest >= 'a' && toTest <= 'z') return true;
            if (toTest >= '0' && toTest <= '9') return true;
            if (toTest == '_' || toTest == '^' || toTest == '$' || toTest == '~' ||
                    toTest == '!' || toTest == '#' || toTest == '%' || toTest == '&' ||
                    toTest == '-' || toTest == '{' || toTest == '}' || toTest == '(' ||
                    toTest == ')' || toTest == '@' || toTest == '\'' || toTest == '`')
                return true;

            return false;
        }

        public static bool IsSkipChar(char c) {
            return (c == '.') || (c == ' ');
        }

        public static string TidyString(string dirty) {
            var result = new StringBuilder();

            /* epurate it from alien characters */
            for (var src = 0; src < dirty.Length; src++) {
                var toTest = char.ToUpperInvariant(dirty[src]);
                if (IsSkipChar(toTest)) continue;

                if (ValidChar(toTest)) {
                    result.Append(toTest);
                }
                else {
                    result.Append('_');
                }
            }

            return result.ToString();
        }

        public static bool CleanString(string s) {
            for (var i = 0; i < s.Length; i++) {
                if (IsSkipChar(s[i])) return false;
                if (!ValidChar(s[i])) return false;
            }

            return true;
        }

        public static string StripLeadingPeriods(string str) {
            var sb = new StringBuilder(str.Length);

            for (var i = 0; i < str.Length; i++) {
                if (str[i] != '.') {
                    sb.Append(str.Substring(i));
                    break;
                }
            }

            return sb.ToString();
        }    /*
              * These characters are all invalid in 8.3 names, plus have been shown to be
              * harmless on all tested devices
              */
        private static readonly char[] Invalidchar = {
            (char) 0x01, (char) 0x02, (char) 0x03, (char) 0x04, (char) 0x05, (char) 0x06,
            (char) 0x0B,
            (char) 0x0C, (char) 0x0E, (char) 0x0F, (char) 0x10, (char) 0x11, (char) 0x12,
            (char) 0x13,
            (char) 0x14, (char) 0x15, (char) 0x16, (char) 0x17, (char) 0x18, (char) 0x19,
            (char) 0x1A,
            (char) 0x1B, (char) 0x1C, (char) 0x1D, (char) 0x1E, (char) 0x1F, (char) 0x22,
            (char) 0x2a,
            (char) 0x3a, (char) 0x3c, (char) 0x3e, (char) 0x3f, (char) 0x5b, (char) 0x5d,
            (char) 0x7c
    };
        
        /// <summary>
        /// See original C Linux patch by Andrew Tridgell (tridge@samba.org)
        /// build a 11 byte 8.3 buffer which is not a short filename. We want 11
        /// bytes which: - will be seen as a constant string to all APIs on Linux and
        /// Windows - cannot be matched with wildcard patterns - cannot be used to
        /// access the file - has a low probability of collision within a directory -
        /// has an invalid 3 byte extension - contains at least one non-space and
        /// non-nul byte
        /// </summary>
        /// <param name="longFullName">the long file name to generate the buffer for</param>
        /// <returns>the generated 8.3 buffer</returns>
        public ShortName Generate83BufferNew(string longFullName) {
            if (longFullName == null) {
                throw new ArgumentNullException(nameof(longFullName));
            }

            var retBuffer = new char[11];

            var hasRealShortName = false;// getRealShortNameInstead(longFullName,
                                          // retBuffer);
            if (!hasRealShortName) {
                int i, tildePos, slashPos;
                var randomNumber = Math.Abs(mRandom.Next());

                /*
                 * the '/' makes sure that even unpatched Linux systems can't get at
                 * files by the 8.3 entry.
                 */

                slashPos = randomNumber % 8;
                randomNumber >>= 3;

                /*
                 * fill in the first 8 bytes with invalid characters. Note that we
                 * need to be careful not to run out of randomness. We use the same
                 * extension for all buffers.
                 */
                for (i = 0; i < 8; i++) {
                    if (i == slashPos)
                        retBuffer[i] = '/';
                    else {
                        retBuffer[i] =
                                Invalidchar[randomNumber % Invalidchar.Length];
                        randomNumber /= Invalidchar.Length;
                        if (randomNumber < Invalidchar.Length)
                            randomNumber = Math.Abs(mRandom.Next());
                    }
                }

                for (i = 0; i < 8; i++) {
                    if (retBuffer[i] == 0xe5) {
                        throw new Exception();
                    }
                }

                retBuffer[8] = 'i';
                retBuffer[9] = 'f';
                retBuffer[10] = 'l';
            }
            var retName = new ShortName(retBuffer);
            retName.SetHasShortNameOnly(hasRealShortName);
            return retName;
        }

    }

}