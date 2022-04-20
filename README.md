# About EverLoader

EverLoader is a Windows application that helps you to load games onto a MicroSD card to be played on your Evercade™ using an EverSD cartridge.

**Disclaimer: EverSD and EverLoader are not affiliated in any way with EVERCADE™.\
EverSD does not support or condone piracy.**

# Downloading and Installing EverLoader

You can get the latest version of EverLoader from the [Releases](https://github.com/EverSD-Cart/EverLoader/releases) page.

### EverLoader is Portable
EverLoader is a portable Windows app, which means that after unzipping, you can run it from any drive or directory to which you have read/write access.
On usage, it creates a folder named `everloader_data` alongside the EverLoader executable to hold all games, artwork images and other game information.

# Using EverLoader

With EverLoader, you can load retro games onto a MicroSD card in three easy steps:

## 1. Add Game Files
When you add new games/roms to EverLoader (using the 'Add New ROMs' button), it will automatically take care of scraping the game descriptions, box art images, banners and other information from the Internet using the online TheGamesDB database.

## 2. Edit Game Info
After you've added new games to EverLoader, you can edit all the game information, like Title, Descripton, Genre, Release date, but also information on the button mappings. Besides that, you can upload custom box art or banners in case the scraper didn't find any images that you like. And if nothing was scraped, you can try the 'Scrape by game title' option, which shows you a list of possible matches - filtered by the console platform - for you to choose from. In case multiple banners are found, you can pick the one you like and move the cut-out up or down.

## 3. Sync Games with your MicroSD
After you're done editing the game information, you can tick the checkboxes of the games you want to synchonize to a MicroSD card connected to your computer. EverLoader will copy the games and images in the correct format to your MicroSD, so you can play them on your Evercade™ using an EverSD cartridge.

# Information for Developers

EverLoader is an Open-Source project. You can have a look at the sources,  compile it locally, or even contribute to it by forking the project and sending us a pull request.

## Building EverLoader
To build EverLoader, you need Visual Studio 2019 or higher. The [Community Edition of Visual Studio](https://visualstudio.microsoft.com/vs/community/) will do and is free for open-source and individual developers.

When opening the solution file, you will notice the file `secrets.json` is missing. This is because it contains our key for the TheGamesDB API, which we don't want to share publicly. You can get the key by asking around on the EverSD discord server, or use your own TheGamesDB API key.

```
{
    "Secrets": 
    { 
        "TheGamesDBApi_ApiKey": "[API key here]" 
    }
}
```

## Description of `appsettings.json`

The `appsettings.json` file describes the "rules" of EverLoader and contains the configuration from which basically everything is controlled. This file is embedded in EverLoader, but you can override it by putting a copy of it into the `everloader_data` directory, allowing you to fiddle with the settings and test your changes.

This is a short description of the main settings inside `appsettings.json`:

### `ReleasesEndpoint`
This is the URL where EverLoader checks for new versions. No need to change it.

### `Platforms` array 
The Platforms array within `appsettings.json` contains the list of supported gaming platforms with their rom file extensions, BIOS files, emulator names and their source locations, etc. The `Platforms` array is where you need to make changes if you want EverLoader to:
* Support a new platform
* Add an additional rom file extension
* Add an alternative Libretro core
* Specify required or optional BIOS file(s)
* Add specific game options for RetroArch

Detailed description of Platform-JSON properties:
* `Id` - Unique ID for each platform. When adding a new platform, make sure to pick a new, unique number.
* `Alias` - Unique alias for the platform. This also names the folder in which platform-specific BIOS files are stored under the `everloader_data` directory.
* `Name` - Name of the platform, as it is displayed in the platform dropdown.
* `Group` [optional] - Name of the platform group, under which it is displayed in the platform dropdown.
* `SupportedExtensions` - Array of game file/ROM extensions (including the ".") which can be loaded by the emulator/core.
* `TGDB_PlatformIds` - Array of matching platforms from the TheGamesDB.net website. This is used for better matching when scraping game information. Note: when no platform is provided, EverLoader will assume the PC platform for game matching during scraping. For a list of all the platforms supported by TheGamesDB, see https://thegamesdb.net/list_platforms.php
    * `Id` - Matching platform Id from TheGamesDB.net
    * `Name` - Matching platform Name from TheGamesDB.net
* `BiosFiles` [optional] - List of BIOS filenames and optionally their MD5 hashes.
    * `FileName` - The BIOS filename, which will be copied either to the `/sdcard/bios` directory (for internal emulator) or to the `/sdcard/retroarch/system` directory (for RetroArch emulators)
    * `MD5` [optional] - Array of supported MD5 hashes for the BIOS file. This helps users to ensure that their uploaded BIOS file is the right one.
    * `Required` - false or true. When true and BIOS file is missing, then you cannot sync games for this platform to MicroSD.
    * `SupportedExtensions` [optional] - Array of ROM extensions that require this BIOS file. Example: See NES settings, which only require the `disksys.rom` BIOS for game files with the ".fds" extension (= Famicom Disk System emulation).
* `InternalEmulator` [optional] - Only used for Libretro cores which are supported by Evercade's internal emulator, integrating nicely with the popup menu and default buttons.
    * `CoreFileName` - The filename of the core. For 'autolaunch' cores, this name cannot be changed, as it is hardcoded.
    * `AutoLaunch` - Directly pass the rom-file to the Evercade emulator, skipping the use of an auto-generated special shell script.
    * `SupportedExtensions` [optional] - Only provide extensions here if they are different from the extensions under the Platform object.
    * `Files` - Array of File objects, which describe the source (url) and destination paths for the emulator core and other required files.
        * `SourceUrl` - Url containing the source of the file. If the url path ends with ".zip", it will be extracted automatically.
        * `SourcePath` [optional] - Path of the source file inside the source zip file. When the file isn't inside a zip, you can omit this parameter.
        * `TargetPath` - Target path of the file on the MicroSD card.
* `RetroArchCores` - Array of RetroArch cores that can run games files for the platform. For some consoles (e.g. NES) there are multiple good emulators.
    * `DisplayName` - Name of the emulator core, as displayed in the dropdown next to 'External RetroArch Core'. 
    * `CoreFileName` - Filename of the core, as used in launch scripts
    * `SupportedExtensions` [optional] - Only provide extensions here if they are different from the extensions under the Platform object.
    * `Files` - Array of File objects, which describe the source (url) and destination paths for the emulator core and other required files. In case the url path ends with .zip, it will be extracted automatically.
        * `SourceUrl` - Url containing the source of the file. If the url path ends with ".zip", it will be extracted automatically.
        * `SourcePath` [optional] - Path of the file in the source zip file. When the file isn't inside a zip, you can omit this parameter.
        * `SourceContent` [optional] - Instead of a SourceUrl, you can optionally provide an array of text content lines. This can be used to do some game-specific settings in an RetroArch .opt file. For example the Atari 5200 platform uses this.
        * `TargetPath` - Target path of the file on the MicroSD card. For RetroArch core files, this will be `retroarch/cores/[name of core].so`. Note that if the target path contains the literal string "[game.Id]", it will be replaced by the game id. This is used to create some specific Atari 5200 emulator setting files.

### `Genres`
An array of genres shown in the Genre dropdown list. 
* `Name` - Name of the genre, e.g. "Action"
* `TGDB_GenreIds` - Array of mapped genre Ids from TheGamesDB. This will select the right genre for scraped games.

## Structure of the `everloader_data` directory

The `everloader_data` directory holds all the loaded game files, including artork images and game metadata. You can easily create a backup of EverLoader by just copying `EverLoader.exe` and the complete `everloader_data` directory.

* `usersettings.json` [file] - Contains optional user preferences (e.g. for optimized images yes/no)
* `bios` [dir] - Contains the BIOS files uploaded by the user for each platform
* `cache` [dir] - Contains the downloaded emulator cores, which will be cached locally for 24 hours.
* `games` [dir] - Contains a list of game subdirectories. Each game subdirectory has a unique name, which is also the EverLoader game-Id.
    * `[game-id].json` [file] - Contains all game metadata. This is basically containing all game properties needed by Evercade, extended with other properties needed for EverLoader.
    * `images` [dir] - Contains the scaled artwork images. Subfolder `source` contain the source images, needed for doing banner image cut-outs.
    * `rom` [dir] - Contains the original game file(s) or ROM file. In case of multidisc/multidisk games, it contains all files from all discs.

## Compiling Libretro cores for the Evercade

Most existing Libretro cores are written in C++, so you'll need to have a specific GCC-based toolchain on your machine to correctly compile for the Evercade. This is also known a "cross-compiling".

Luckily for us, someone already did the hard work and provided [instructions on how to build a C and C++ cross-compilation toolchain for the Evercade](https://github.com/strager/evercade-hacking/blob/master/toolchain.md). You'll need to build the toolchain from a Linux machine. If you're on Windows, you can use WSL. Or - if you're lazy like me - use the pre-configured Docker container.

The sources of most Libretro cores contain a Makefile which supports the `classic_armv7_a7` platform that uses some predefined flags and options to targeting ARM-based retro console emulators like the (S)NES mini and C64 mini. Good news: these specific builds are compatible with the Evercade, which also uses the ARMv7 + Neon GPU!

### Example: Building the Vice Libretro core to run on the Evercade

 * I'm assuming you have the cross-compilation toolchain binaries already available, for example in the directory `/git/evercade-hacking/build/usr/bin`
 * Get the sources of the Vice Libretro core (without git history):\
 `git clone --depth 1 https://github.com/libretro/vice-libretro.git`
 * Now `cd` into the vice-libretro directory and run `make`, passing `platform`, `CC`, `CXX` and `AR` as environment variables like this:

 ```make EMUTYPE=x64 platform=classic_armv7_a7 CC=/git/evercade-hacking/build/usr/bin/arm-linux-gnueabihf-gcc CXX=/git/evercade-hacking/build/usr/bin/arm-linux-gnueabihf-g++ AR=/git/evercade-hacking/build/usr/bin/arm-linux-gnueabihf-ar```

  * Note the additional `EMUTYPE=x64` parameter, which is specific for the Vice core Makefile to compile the (faster) x64 emulator, which works best on the Evercade.
  * Now wait for the build process to complete and then copy the resulting binary `vice_x64_libretro.so` to the RetroArch `/cores` subdirectory on your MicroSD card to test your freshly built Vice x64 Libretro core!

---
Disclaimer: EverSD and EverLoader are not affiliated in any way with EVERCADE™.\
EverSD does not support or condone piracy.



