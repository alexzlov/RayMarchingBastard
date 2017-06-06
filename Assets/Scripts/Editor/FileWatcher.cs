using System.IO;

namespace CodeEdit
{
    public class FileWatcher
    {
        public delegate void OnChangeEvent();
        public event OnChangeEvent OnChange;

        private FileSystemWatcher _watcher;
        private bool _hasChanged;

        public void Start(string path)
        {
            _watcher = new FileSystemWatcher()
            {
                Path = Path.GetDirectoryName(path),
                NotifyFilter = NotifyFilters.LastAccess |
                               NotifyFilters.LastWrite  |
                               NotifyFilters.FileName   |
                               NotifyFilters.DirectoryName,
                Filter = Path.GetFileName(path),
            };            
            
            _watcher.Changed += OnChanged;
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.EnableRaisingEvents = true;
        }

        public void Stop()
        {
            if (_watcher != null)
            {
                _watcher.EnableRaisingEvents = false;
            }
        }

        public void Update()
        {
            if (_hasChanged && OnChange != null)
            {
                _hasChanged = false;
                OnChange();
            }
        }

        private void OnChanged(object source, FileSystemEventArgs args)
        {
            _hasChanged = true;
        }

        private void OnRenamed(object source, RenamedEventArgs args)
        {
            _hasChanged = true;
        }
    }
}