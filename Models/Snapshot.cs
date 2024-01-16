using Alphaleonis.Win32.Vss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{

    public class Snapshot : IDisposable
    {
        IVssBackupComponents _backup;

        VssSnapshotProperties _props;

        Guid _set_id;

        Guid _snap_id;


        public Snapshot(IVssBackupComponents backup)
        {
            _backup = backup;
            _set_id = backup.StartSnapshotSet();
        }

        public void Dispose()
        {
            try { Delete(); } catch { }
        }


        public void AddVolume(string volumeName)
        {
            if (_backup.IsVolumeSupported(volumeName))
                _snap_id = _backup.AddToSnapshotSet(volumeName);
            else
                throw new VssVolumeNotSupportedException(volumeName);
        }


        public void Copy()
        {
            _backup.DoSnapshotSet();
        }


        public void Delete()
        {
            _backup.DeleteSnapshotSet(_set_id, false);
        }

        public string Root
        {
            get
            {
                if (_props == null)
                    _props = _backup.GetSnapshotProperties(_snap_id);
                return _props.SnapshotDeviceObject;
            }
        }
    }
}
