using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EverLoader.Models
{
    public class AppSettings
    {
        public Platform[] Platforms { get; set; }
        public Genre[] Genres { get; set; }
        public Secrets Secrets { get; set; }
        public string ReleasesEndpoint { get; set; }
    }

    public class Genre
    {
        public string Name { get; set; }
        public int[] TGDB_GenreIds { get; set; } = new int[] { };
    }

    public class Secrets
    {
        public string TheGamesDBApi_ApiKey { get; set; }
    }

    public class BiosFile
    {
        public string FileName { get; set; }
        public string[] MD5 { get; set; } = new string[] { };
        public bool Required { get; set; } = false;
        public string[] SupportedExtensions { get; set; } = new string[] { };
    }

    public class Platform
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public string[] SupportedExtensions { get; set; } = new string[] { };
        public int GroupItemSortOrder { get; set; } = 0; // 0 = not ordered
        public IdName[] TGDB_PlatformIds { get; set; } = new IdName[] { };
        public BiosFile[] BiosFiles { get; set; } = new BiosFile[] { };
        public Core InternalEmulator { get; set; }
        public Core[] RetroArchCores { get; set; } = new Core[] { };

        public string GroupAndName => $"{(Group != null ? Group + " " : null)}{Name}";
    }

    public class IdName
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class Core
    {
        public string CoreFileName { get; set; }
        public string DisplayName { get; set; }
        //a core can have specific supported extensions
        public string[] SupportedExtensions { get; set; } = new string[] { };
        public bool AutoLaunch { get; set; }
        public ExternalFile[] Files { get; set; } = new ExternalFile[] { };
    }

    public class ExternalFile
    {
        public string SourceUrl { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
    }
}
