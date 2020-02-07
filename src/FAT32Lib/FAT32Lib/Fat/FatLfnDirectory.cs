/*
 * This library (fat32lib.NET) is a port of the library fat32lib obtained from
 * https://android.googlesource.com/platform/external/fat32lib/
 * The original license for this file is replicated below.
 * 
 * Copyright (C) 2003-2009 JNode.org
 *               2009,2010 Matthias Treydte <mt@waldheinz.de>
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
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// The <see cref="IFsDirectory"/> implementation for FAT file systems. This
    /// implementation aims to fully comply to the FAT specification, including
    /// the quite complex naming system regarding the long file names (LFNs) and
    /// their corresponding 8+3 short file names. This also means that an
    /// <see cref="FatLfnDirectory"/> is case-preserving but not case-sensitive.
    /// </summary>
    public sealed class FatLfnDirectory : AbstractFsObject, IFsDirectory {

        /// <summary>
        /// This set is used to check if a file name is already in use in this
        /// directory. The FAT specification says that file names must be unique
        /// ignoring the case, so this set contains all names converted to
        /// lower-case, and all checks must be performed using lower-case strings.
        /// </summary>
        private readonly HashSet<string> usedNames;
        private readonly Fat fat;
        private readonly Dictionary<ShortName, FatLfnDirectoryEntry> shortNameIndex;
        private readonly Dictionary<string, FatLfnDirectoryEntry> longNameIndex;
        private readonly Dictionary<FatDirectoryEntry, FatFile> entryToFile;
        private readonly Dictionary<FatDirectoryEntry, FatLfnDirectory> entryToDirectory;
        private readonly Dummy83BufferGenerator dbg;

        internal readonly AbstractDirectory Dir;

        internal FatLfnDirectory(AbstractDirectory dir, Fat fat, bool readOnly) : base(readOnly) {

            if ((dir == null) || (fat == null)) throw new NullReferenceException();

            this.fat = fat;
            this.Dir = dir;

            shortNameIndex = new Dictionary<ShortName, FatLfnDirectoryEntry>();

            longNameIndex = new Dictionary<string, FatLfnDirectoryEntry>();

            entryToFile = new Dictionary<FatDirectoryEntry, FatFile>();

            entryToDirectory = new Dictionary<FatDirectoryEntry, FatLfnDirectory>();

            usedNames = new HashSet<string>();
            dbg = new Dummy83BufferGenerator();

            ParseLfn();
        }

        internal FatFile GetFile(FatDirectoryEntry entry) {
            entryToFile.TryGetValue(entry, out var file);

            if (file == null) {
                file = FatFile.Get(fat, entry);
                entryToFile.Add(entry, file);
            }

            return file;
        }

        internal FatLfnDirectory GetDirectory(FatDirectoryEntry entry) {
            entryToDirectory.TryGetValue(entry, out var result);

            if (result == null) {
                var storage = Read(entry, fat);
                result = new FatLfnDirectory(storage, fat, IsReadOnly());
                entryToDirectory.Add(entry, result);
            }

            return result;
        }

        /// <summary>
        /// According to the FAT file system specification, leading and trailing
        /// spaces in the name are ignored by this method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public IFsDirectoryEntry AddFile(string name) {
            CheckWritable();
            CheckUniqueName(name);

            name = name.Trim();
            var sn = MakeShortName(name, false);

            var entry = new FatLfnDirectoryEntry(name, sn, this, false);

            Dir.AddEntries(entry.CompactForm());

            shortNameIndex.Add(sn, entry);
            longNameIndex.Add(name.ToLowerInvariant(), entry);

            GetFile(entry.RealEntry);

            Dir.SetDirty();
            return entry;
        }

        internal bool IsFreeName(string name) {
            return true;
        }

        private void CheckUniqueName(string name) {
        }

        private void FreeUniqueName(string name) {
        }

        private ShortName MakeShortName(String name, bool isDirectory) {
            ShortName result;

            try {
                result = dbg.Generate83BufferNew(name);
            }
            catch (ArgumentException ex) {
                throw new IOException(
                        "could not generate short name for \"" + name + "\"", ex);
            }
            return result;
        }

        /// <summary>
        /// According to the FAT file system specification, leading and trailing
        /// spaces in the name are ignored by this method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        /// <exception cref="IOException"></exception>
        public IFsDirectoryEntry AddDirectory(string name) {
            CheckWritable();
            CheckUniqueName(name);

            name = name.Trim();
            var sn = MakeShortName(name, true);
            var real = Dir.CreateSub(fat);
            real.SetShortName(sn);
            var e = new FatLfnDirectoryEntry(this, real, name);

            try {
                Dir.AddEntries(e.CompactForm());
            }
            catch (IOException ex) {
                var cc = new ClusterChain(fat, real.GetStartCluster(), false);
                cc.SetChainLength(0);
                Dir.RemoveEntry(real);
                throw ex;
            }

            shortNameIndex.Add(sn, e);
            longNameIndex.Add(name.ToLowerInvariant(), e);

            GetDirectory(real);

            Flush();
            return e;
        }

        /// <summary>
        /// According to the FAT file system specification, leading and trailing
        /// spaces in the name are ignored by this method.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public IFsDirectoryEntry GetEntry(string name) {
            name = name.Trim().ToLowerInvariant();

            longNameIndex.TryGetValue(name, out var entry);

            if (entry == null) {
                if (!ShortName.CanConvert(name)) return null;
                return shortNameIndex[ShortName.Get(name)];
            }

            return entry;
        }

        private void ParseLfn() {
            var i = 0;
            var size = Dir.GetEntryCount();

            while (i < size) {
                // jump over empty entries
                while (i < size && Dir.GetEntry(i) == null) {
                    i++;
                }

                if (i >= size) {
                    break;
                }

                var offset = i; // beginning of the entry
                                // check when we reach a real entry
                while (Dir.GetEntry(i).IsLfnEntry()) {
                    i++;
                    if (i >= size) {
                        // This is a cutted entry, forgive it
                        break;
                    }
                }

                if (i >= size) {
                    // This is a cutted entry, forgive it
                    break;
                }

                var current =
                        FatLfnDirectoryEntry.Extract(this, offset, ++i - offset);

                if (!current.RealEntry.IsDeleted() && current.IsValid()) {
                    CheckUniqueName(current.GetName());

                    shortNameIndex.Add(current.RealEntry.GetShortName(), current);
                    longNameIndex.Add(current.GetName().ToLowerInvariant(), current);
                }
            }
        }

        private void UpdateLfn() {
            var dest =
                    new List<FatDirectoryEntry>();

            foreach (var currentEntry in shortNameIndex.Values) {
                var encoded = currentEntry.CompactForm();
                dest.AddRange(encoded);
            }

            var size = dest.Count;

            Dir.ChangeSize(size);
            Dir.SetEntries(dest);
        }

        public void Flush() {
            CheckWritable();

            foreach (var f in entryToFile.Values) {
                f.Flush();
            }

            foreach (var d in entryToDirectory.Values) {
                d.Flush();
            }

            UpdateLfn();
            Dir.Flush();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            foreach (var x in shortNameIndex.Values)
                yield return x;
        }

        /// <summary>
        /// Remove the entry with the given name from this directory.
        /// </summary>
        /// <param name="name">the name of the entry to remove</param>
        /// <exception cref="IOException">IOException on error removing the entry</exception>
        /// <exception cref="InvalidOperationException">InvalidOperationException on an attempt to remove the dot entries</exception>
        public void Remove(string name) {
            CheckWritable();

            if (!(GetEntry(name) is FatLfnDirectoryEntry entry)) return;

            UnlinkEntry(entry);

            var cc = new ClusterChain(
                    fat, entry.RealEntry.GetStartCluster(), false);

            cc.SetChainLength(0);

            FreeUniqueName(name);
            UpdateLfn();
        }

        /// <summary>
        /// Unlinks the specified entry from this directory without actually
        /// deleting it.
        /// </summary>
        /// <param name="entry">the entry to be unlinked</param>
        /// <seealso cref="LinkEntry(FatLfnDirectoryEntry)"/>
        internal void UnlinkEntry(FatLfnDirectoryEntry entry) {
            var sn = entry.RealEntry.GetShortName();

            if (sn.Equals(ShortName.Dot) || sn.Equals(ShortName.DotDot)) throw
                    new ArgumentException(
                        "the dot entries can not be removed");

            var lowerName = entry.GetName().ToLowerInvariant();

            if (!longNameIndex.ContainsKey(lowerName))
                throw new Exception();
            longNameIndex.Remove(lowerName);

            if (!shortNameIndex.ContainsKey(sn))
                throw new Exception();
            shortNameIndex.Remove(sn);

            if (entry.IsFile()) {
                entryToFile.Remove(entry.RealEntry);
            }
            else {
                entryToDirectory.Remove(entry.RealEntry);
            }
        }

        /// <summary>
        /// Links the specified entry to this directory, updating the entrie's
        /// short name.
        /// </summary>
        /// <param name="entry">the entry to be linked (added) to this directory</param>
        /// <seealso cref="UnlinkEntry(FatLfnDirectoryEntry)"/>
        internal void LinkEntry(FatLfnDirectoryEntry entry) {
            CheckUniqueName(entry.GetName());
            ShortName name;
            name = dbg.Generate83BufferNew(entry.GetName());
            entry.RealEntry.SetShortName(name);

            longNameIndex.Add(entry.GetName().ToLowerInvariant(), entry);
            shortNameIndex.Add(entry.RealEntry.GetShortName(), entry);

            UpdateLfn();
        }

        private static ClusterChainDirectory Read(FatDirectoryEntry entry, Fat fat) {

            if (!entry.IsDirectory()) throw
                    new ArgumentException(entry + " is no directory");

            var chain = new ClusterChain(
                    fat, entry.GetStartCluster(),
                    entry.IsReadonlyFlag());

            var result =
                    new ClusterChainDirectory(chain, false);

            result.Read();
            return result;
        }


    }

}