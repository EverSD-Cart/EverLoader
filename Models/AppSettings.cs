using System;
using System.Collections.Generic;
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

    public class Platform
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public string Group { get; set; }
        public int GroupItemSortOrder { get; set; } = 0; // 0 = not ordered
        public IdName[] TGDB_PlatformIds { get; set; } = new IdName[] { };
        public string[] RomFileExtensions { get; set; } = new string[] { };
        public string[] AltFileExtensions { get; set; } = new string[] { };
        public string[] BiosFiles { get; set; } = new string[] { };
        public Core BlastRetroCore { get; set; }
        public Core[] RetroArchCores { get; set; } = new Core[] { };
        public string GoogleSuffix { get; set; } //used for a future feature (Google image search)
        // GET /search?q=Kid+Icarus+(fds+%7c+nes+%7c+famicom)+(box%7ccover)+art&source=lnms&tbm=isch HTTP/1.1
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
