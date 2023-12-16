using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Back_It_Up.Models
{
    public class MetadataItem
    {
        public string Type { get; set; }
        public string Path { get; set; }
        public string RootPath { get; set; } // Add this property
    }

}
