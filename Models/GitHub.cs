using System;
using System.Collections.Generic;
using System.Text;

namespace EverLoader.Models
{
    public class GitHubRelease
    {
        public string tag_name { get; set; }
        public string name { get; set; }
        public bool draft { get; set; }
        public bool prerelease { get; set; }
        public GitHubAsset[] assets { get; set; }
        public string body { get; set; }
    }

    public class GitHubAsset
    {
        public string name { get; set; }
        public string browser_download_url { get; set; }
    }
}
