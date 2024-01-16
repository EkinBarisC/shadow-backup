using Alphaleonis.Win32.Filesystem;
using Alphaleonis.Win32.Vss;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{

    public class VssBackup : IDisposable
    {

        bool ComponentMode = false;
        IVssBackupComponents _backup;
        Snapshot _snap;

        public VssBackup()
        {
            InitializeBackup();
        }

        public async Task Setup(string volumeName)
        {
            await Task.Run(() =>
            {
                Discovery(volumeName);
                PreBackup();
            });
        }

        public void Dispose()
        {
            try { Complete(true); } catch { }

            if (_snap != null)
            {
                _snap.Dispose();
                _snap = null;
            }

            if (_backup != null)
            {
                _backup.Dispose();
                _backup = null;
            }
        }

        void InitializeBackup()
        {
            IVssFactory vss = VssFactoryProvider.Default.GetVssFactory();
            _backup = vss.CreateVssBackupComponents();
            _backup.InitializeForBackup(null);
            _backup.GatherWriterMetadata();
        }

        void Discovery(string fullPath)
        {
            _backup.FreeWriterMetadata();
            _snap = new Snapshot(_backup);
            _snap.AddVolume(Path.GetPathRoot(fullPath));
        }

        void PreBackup()
        {
            Debug.Assert(_snap != null);
            _backup.SetBackupState(ComponentMode,
                  true, VssBackupType.Full, false);

            _backup.PrepareForBackup();
            _snap.Copy();
        }

        public string GetSnapshotPath(string localPath)
        {
            if (Path.IsPathRooted(localPath))
            {
                string root = Path.GetPathRoot(localPath);
                localPath = localPath.Replace(root, String.Empty);
            }
            string slash = Path.DirectorySeparatorChar.ToString();
            if (!_snap.Root.EndsWith(slash) && !localPath.StartsWith(slash))
                localPath = localPath.Insert(0, slash);
            localPath = localPath.Insert(0, _snap.Root);

            return localPath;
        }

        void Complete(bool succeeded)
        {
            if (ComponentMode)
            {
                IList<IVssExamineWriterMetadata> writers = _backup.WriterMetadata;
                foreach (IVssExamineWriterMetadata metadata in writers)
                {
                    foreach (IVssWMComponent component in metadata.Components)
                    {
                        _backup.SetBackupSucceeded(
                              metadata.InstanceId, metadata.WriterId,
                              component.Type, component.LogicalPath,
                              component.ComponentName, succeeded);
                    }
                }

                _backup.FreeWriterMetadata();
            }

            try
            {
                _backup.BackupComplete();
            }
            catch (VssBadStateException) { }
        }

    }
}
