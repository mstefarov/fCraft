// Part of fCraft | Copyright 2009-2013 Matvei Stefarov <me@matvei.org> | BSD-3 | See LICENSE.txt

using System;
using System.Threading;
using JetBrains.Annotations;

namespace fCraft {
    internal static class RwlsExtension {
        public static ReadLockHelper ReadLock( [NotNull] this ReaderWriterLockSlim readerWriterLock ) {
            return new ReadLockHelper( readerWriterLock );
        }

        public static UpgradeableReadLockHelper UpgradableReadLock( [NotNull] this ReaderWriterLockSlim readerWriterLock ) {
            return new UpgradeableReadLockHelper( readerWriterLock );
        }

        public static WriteLockHelper WriteLock( [NotNull] this ReaderWriterLockSlim readerWriterLock ) {
            return new WriteLockHelper( readerWriterLock );
        }

        public struct ReadLockHelper : IDisposable {
            readonly ReaderWriterLockSlim readerWriterLock;

            public ReadLockHelper( [NotNull] ReaderWriterLockSlim readerWriterLock ) {
                readerWriterLock.EnterReadLock();
                this.readerWriterLock = readerWriterLock;
            }

            public void Dispose() {
                readerWriterLock.ExitReadLock();
            }
        }

        public struct UpgradeableReadLockHelper : IDisposable {
            readonly ReaderWriterLockSlim readerWriterLock;

            public UpgradeableReadLockHelper( [NotNull] ReaderWriterLockSlim readerWriterLock ) {
                readerWriterLock.EnterUpgradeableReadLock();
                this.readerWriterLock = readerWriterLock;
            }

            public void Dispose() {
                readerWriterLock.ExitUpgradeableReadLock();
            }
        }

        public struct WriteLockHelper : IDisposable {
            readonly ReaderWriterLockSlim readerWriterLock;

            public WriteLockHelper( [NotNull] ReaderWriterLockSlim readerWriterLock ) {
                readerWriterLock.EnterWriteLock();
                this.readerWriterLock = readerWriterLock;
            }

            public void Dispose() {
                readerWriterLock.ExitWriteLock();
            }
        }
    }
}
