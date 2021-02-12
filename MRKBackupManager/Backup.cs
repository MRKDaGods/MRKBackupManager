using System;

namespace MRKBackupManager {
    public class Backup {
        public string Name { get; set; }
        public string Source { get; set; }
        public string Location { get; set; }
        public DateTime CreationDate { get; set; }
        public DateTime LastModificationDate { get; set; }
    }
}
