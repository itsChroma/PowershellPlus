using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PowerPlus.Models
{
    public class FileDetailsModel
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public PathGeometry FileIcon { get; set; }
        public string FileExtension { get; set; }
        public string FileSize { get; set; }
        public string CreatedOn { get; set; }
        public string ModifiedOn { get; set; }
        public string AccessedOn { get; set; }
        public bool IsDirectory { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsHidden { get; set; }
        public bool IsImage { get; set; }
        public bool IsVideo { get; set; }
        public bool IsSelected { get; set; }
        public bool IsPinned { get; set; }

        internal string _Type { get; set; }
        public string Type => _Type = IsDirectory ? "Folder" : "File";
    }
}
