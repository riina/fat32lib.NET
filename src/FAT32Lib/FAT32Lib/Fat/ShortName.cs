/*
 * This library (fat32lib.NET) is a port of the library fat32lib obtained from
 * https://android.googlesource.com/platform/external/fat32lib/
 * The original license for this file is replicated below.
 * 
 * Copyright (C) 2009,2010 Matthias Treydte <mt@waldheinz.de>
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

namespace FAT32Lib.Fat {

    /// <summary>
    /// Represents a "short" (8.3) file name as used by DOS.
    /// </summary>
    public sealed class ShortName {

        /// <summary>
        /// These are taken from the FAT specification.
        /// </summary>
        private static readonly byte[] IllegalChars = {
            0x22, 0x2A, 0x2B, 0x2C, 0x2E, 0x2F, 0x3A, 0x3B,
            0x3C, 0x3D, 0x3E, 0x3F, 0x5B, 0x5C, 0x5D, 0x7C
    };

        /// <summary>
        /// The name of the "current directory" (".") entry of a FAT directory.
        /// </summary>
        public static readonly ShortName Dot = new ShortName(".", "");

        /// <summary>
        /// The name of the "parent directory" ("..") entry of a FAT directory.
        /// </summary>
        public static readonly ShortName DotDot = new ShortName("..", "");

        private readonly char[] name;
        private bool mShortNameOnly;

        private ShortName(string nameExt) {
            if (nameExt.Length > 12)
                throw new ArgumentException("name too long");

            var i = nameExt.IndexOf('.');
            String nameString, extString;

            if (i < 0) {
                nameString = nameExt.ToUpperInvariant();
                extString = "";
            }
            else {
                nameString = nameExt.Substring(0, i).ToUpperInvariant();
                extString = nameExt.Substring(i + 1).ToUpperInvariant();
            }

            name = ToCharArray(nameString, extString);
            CheckValidChars(name);
        }

        public ShortName(string name, string ext) {
            this.name = ToCharArray(name, ext);
        }

        internal ShortName(char[] name) {
            this.name = name;
        }

        public ShortName(char[] nameArr, char[] extArr) {
            var result = new char[11];
            Array.Copy(nameArr, result, nameArr.Length);
            Array.Copy(extArr, 0, result, 8, extArr.Length);
            name = result;
        }

        private static char[] ToCharArray(string name, String ext) {
            CheckValidName(name);
            CheckValidExt(ext);

            var result = new char[11];
            for (var i = 0; i < result.Length; i++)
                result[i] = ' ';
            Array.Copy(name.ToCharArray(), result, name.Length);
            Array.Copy(ext.ToCharArray(), 0, result, 8, ext.Length);

            return result;
        }

        /**
         * Calculates the checksum that is used to test a long file name for it's
         * validity.
         * 
         * @return the {@code ShortName}'s checksum
         */
        public byte CheckSum() {
            var dest = new byte[11];
            for (var i = 0; i < 11; i++)
                dest[i] = (byte)name[i];

            int sum = dest[0];
            for (var i = 1; i < 11; i++) {
                sum = dest[i] + (((sum & 1) << 7) + ((sum & 0xfe) >> 1));
            }

            return (byte)(sum & 0xff);
        }

        /**
         * Parses the specified string into a {@code ShortName}.
         * 
         * @param name the name+extension of the {@code ShortName} to get
         * @return the {@code ShortName} representing the specified name
         * @throws IllegalArgumentException if the specified name can not be parsed
         *             into a {@code ShortName}
         * @see #canConvert(java.lang.String)
         */
        public static ShortName Get(string name) {
            if (name == ".")
                return Dot;
            if (name == "..")
                return DotDot;
            return new ShortName(name);
        }

        /**
         * Tests if the specified string can be converted to a {@code ShortName}.
         * 
         * @param nameExt the string to test
         * @return if the string can be converted
         * @see #get(java.lang.String)
         */
        public static bool CanConvert(string nameExt) {
            /* TODO: do this without exceptions */
            try {
                Get(nameExt);
                return true;
            }
            catch (ArgumentException) {
                return false;
            }
        }

        public static ShortName Parse(byte[] data) {
            var nameArr = new char[8];

            for (var i = 0; i < nameArr.Length; i++) {
                nameArr[i] = (char)data[i];
            }

            var extArr = new char[3];
            for (var i = 0; i < extArr.Length; i++) {
                extArr[i] = (char)data[0x08 + i];
            }

            return new ShortName(nameArr, extArr);
        }

        public void Write(byte[] dest) {
            for (var i = 0; i < 11; i++) {
                dest[i] = (byte)name[i];
            }
        }

        public string AsSimpleString() {
            return new string(name).Trim();
        }

        private static void CheckValidName(string name) {
            CheckString(name, "name", 1, 8);
        }

        private static void CheckValidExt(string ext) {
            CheckString(ext, "extension", 0, 3);
        }

        private static void CheckString(string str, string strType,
                int minLength, int maxLength) {

            if (str == null)
                throw new ArgumentException(strType +
                        " is null");
            if (str.Length < minLength)
                throw new ArgumentException(strType +
                        " must have at least " + minLength +
                        " characters: " + str);
            if (str.Length > maxLength)
                throw new ArgumentException(strType +
                        " has more than " + maxLength +
                        " characters: " + str);
        }

        public override bool Equals(object obj) {
            if (obj is ShortName other) {
                for (var i = 0; i < 11; i++)
                    if (name[i] != other.name[i])
                        return false;
                return true;
            }
            return false;
        }

        public override int GetHashCode() {
            return AsSimpleString().GetHashCode();
        }

        public void SetHasShortNameOnly(bool hasShortNameOnly) {
            mShortNameOnly = hasShortNameOnly;
        }

        public bool HasShortNameOnly() {
            return mShortNameOnly;
        }

        /**
         * Checks if the specified char array consists only of "valid" byte values
         * according to the FAT specification.
         * 
         * @param chars the char array to test
         * @throws IllegalArgumentException if invalid chars are contained
         */
        public static void CheckValidChars(char[] chars) {

            if (chars[0] == 0x20)
                throw new ArgumentException(
                        "0x20 can not be the first character");

            for (var i = 0; i < chars.Length; i++) {
                if ((chars[i] & 0xff) != chars[i])
                    throw new ArgumentException("multi-byte character at " + i);

                var toTest = (byte)(chars[i] & 0xff);

                if (toTest < 0x20 && toTest != 0x05)
                    throw new ArgumentException("character < 0x20 at" + i);

                for (var j = 0; j < IllegalChars.Length; j++) {
                    if (toTest == IllegalChars[j])
                        throw new ArgumentException("illegal character " +
                                IllegalChars[j] + " at " + i);
                }
            }
        }
    }

}