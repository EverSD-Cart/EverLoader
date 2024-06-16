using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EverLoader.Models
{
     public class GameInfoTreeNode
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Path { get; set; }
        public bool IsFolder { get; set; }
        public bool IsMissngInCollection { get; set; }
        public bool IsMissngOnCartridge { get; set; }
    }
}
