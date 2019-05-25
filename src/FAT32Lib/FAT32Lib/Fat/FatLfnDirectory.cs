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

        internal readonly AbstractDirectory dir;

        internal FatLfnDirectory(AbstractDirectory dir, Fat fat, bool readOnly) : base(readOnly) {

            if ((dir == null) || (fat == null)) throw new NullReferenceException();

            this.fat = fat;
            this.dir = dir;

            shortNameIndex = new Dictionary<ShortName, FatLfnDirectoryEntry>();

            longNameIndex = new Dictionary<string, FatLfnDirectoryEntry>();

            entryToFile = new Dictionary<FatDirectoryEntry, FatFile>();

            entryToDirectory = new Dictionary<FatDirectoryEntry, FatLfnDirectory>();

            usedNames = new HashSet<string>();
            dbg = new Dummy83BufferGenerator();

            ParseLfn();
        }

        internal FatFile GetFile(FatDirectoryEntry entry) {
            entryToFile.TryGetValue(entry, out FatFile file);

            if (file == null) {
                file = FatFile.Get(fat, entry);
                entryToFile.Add(entry, file);
            }

            return file;
        }

        internal FatLfnDirectory GetDirectory(FatDirectoryEntry entry) {
            entryToDirectory.TryGetValue(entry, out FatLfnDirectory result);

            if (result == null) {
                ClusterChainDirectory storage = Read(entry, fat);
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
            ShortName sn = MakeShortName(name, false);

            FatLfnDirectoryEntry entry = new FatLfnDirectoryEntry(name, sn, this, false);

            dir.AddEntries(entry.CompactForm());

            shortNameIndex.Add(sn, entry);
            longNameIndex.Add(name.ToLowerInvariant(), entry);

            GetFile(entry.realEntry);

            dir.SetDirty();
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
            ShortName sn = MakeShortName(name, true);
            FatDirectoryEntry real = dir.CreateSub(fat);
            real.SetShortName(sn);
            FatLfnDirectoryEntry e = new FatLfnDirectoryEntry(this, real, name);

            try {
                dir.AddEntries(e.CompactForm());
            }
            catch (IOException ex) {
                ClusterChain cc = new ClusterChain(fat, real.GetStartCluster(), false);
                cc.SetChainLength(0);
                dir.RemoveEntry(real);
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

            longNameIndex.TryGetValue(name, out FatLfnDirectoryEntry entry);

            if (entry == null) {
                if (!ShortName.CanConvert(name)) return null;
                return shortNameIndex[ShortName.Get(name)];
            }
            else {
                return entry;
            }
        }

        private void ParseLfn() {
            int i = 0;
            int size = dir.GetEntryCount();

            while (i < size) {
                // jump over empty entries
                while (i < size && dir.GetEntry(i) == null) {
                    i++;
                }

                if (i >= size) {
                    break;
                }

                int offset = i; // beginning of the entry
                                // check when we reach a real entry
                while (dir.GetEntry(i).IsLfnEntry()) {
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

                FatLfnDirectoryEntry current =
                        FatLfnDirectoryEntry.Extract(this, offset, ++i - offset);

                if (!current.realEntry.IsDeleted() && current.IsValid()) {
                    CheckUniqueName(current.GetName());

                    shortNameIndex.Add(current.realEntry.GetShortName(), current);
                    longNameIndex.Add(current.GetName().ToLowerInvariant(), current);
                }
            }
        }

        private void UpdateLFN() {
            List<FatDirectoryEntry> dest =
                    new List<FatDirectoryEntry>();

            foreach (FatLfnDirectoryEntry currentEntry in shortNameIndex.Values) {
                FatDirectoryEntry[] encoded = currentEntry.CompactForm();
                dest.AddRange(encoded);
            }

            int size = dest.Count;

            dir.ChangeSize(size);
            dir.SetEntries(dest);
        }

        public void Flush() {
            CheckWritable();

            foreach (FatFile f in entryToFile.Values) {
                f.Flush();
            }

            foreach (FatLfnDirectory d in entryToDirectory.Values) {
                d.Flush();
            }

            UpdateLFN();
            dir.Flush();
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

            ClusterChain cc = new ClusterChain(
                    fat, entry.realEntry.GetStartCluster(), false);

            cc.SetChainLength(0);

            FreeUniqueName(name);
            UpdateLFN();
        }

        /// <summary>
        /// Unlinks the specified entry from this directory without actually
        /// deleting it.
        /// </summary>
        /// <param name="entry">the entry to be unlinked</param>
        /// <seealso cref="LinkEntry(FatLfnDirectoryEntry)"/>
        internal void UnlinkEntry(FatLfnDirectoryEntry entry) {
            ShortName sn = entry.realEntry.GetShortName();

            if (sn.Equals(ShortName.DOT) || sn.Equals(ShortName.DOT_DOT)) throw
                    new ArgumentException(
                        "the dot entries can not be removed");

            string lowerName = entry.GetName().ToLowerInvariant();

            if (!longNameIndex.ContainsKey(lowerName))
                throw new Exception();
            longNameIndex.Remove(lowerName);

            if (!shortNameIndex.ContainsKey(sn))
                throw new Exception();
            shortNameIndex.Remove(sn);

            if (entry.IsFile()) {
                entryToFile.Remove(entry.realEntry);
            }
            else {
                entryToDirectory.Remove(entry.realEntry);
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
            entry.realEntry.SetShortName(name);

            longNameIndex.Add(entry.GetName().ToLowerInvariant(), entry);
            shortNameIndex.Add(entry.realEntry.GetShortName(), entry);

            UpdateLFN();
        }

        private static ClusterChainDirectory Read(FatDirectoryEntry entry, Fat fat) {

            if (!entry.IsDirectory()) throw
                    new ArgumentException(entry + " is no directory");

            ClusterChain chain = new ClusterChain(
                    fat, entry.GetStartCluster(),
                    entry.IsReadonlyFlag());

            ClusterChainDirectory result =
                    new ClusterChainDirectory(chain, false);

            result.Read();
            return result;
        }


    }

}