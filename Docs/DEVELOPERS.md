# Information for Developers

EverLoader is an Open-Source project. You can have a look at the sources,  compile it locally, or even contribute to it by forking the project and sending us a pull request.

## Building EverLoader
To build EverLoader, you need Visual Studio 2022 or higher. The [Community Edition of Visual Studio](https://visualstudio.microsoft.com/vs/community/) will do and is free for open-source and individual developers. Be sure to select at least the '.NET desktop development' workload when installing.

After opening the solution (`EverLoader.sln`) in Visual Studio, you might see a notification that you need to install .NET Core 3.1 runtime, if you didn't already had it installed. Clicking this notification should help you to install the .NET Core 3.1 runtime for you.

Note: when opening one of the EverLoader forms (e.g. `MainForm.cs`) in Visual Studio, you could get a message about the scaling of your display not being more than 100%. In that case choose to Restart Visual Studio with 100% scaling.

When opening the solution file, you will notice the file `secrets.json` is missing. This is because it contains our API key for the TheGamesDB API, which we don't want to share publicly. You can get the API key by asking around on the EverSD discord server, or use your own TheGamesDB API key. Then you'll have to add the secrets.json file manually, containing the following json:


```
{
    "Secrets": 
    { 
        "TheGamesDBApi_ApiKey": "[API key here]" 
    }
}
```
Note: replace [API key here] with a valid TheGamesDB API key.

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

## Creating a new version of EverLoader (for an official release)
Note: this is something that can only be done by EverLoader team members.

### Preparing
To prepare an official EverLoader release, follow these steps:

1) Create a new git branch `releases/[VERSION-NUMBER]` to work on new features or bug-fixes. Note that you'll need to replace `[VERSION-NUMBER]` with the next (upcoming) version of EverLoader. Note that you can create a new branch (and automatically checkout the new branch) directly from Visual Studio by opening the EverLoader solution and from the top menu select `Git -> New Branch...`

2) Now checkout this branch on your local machine if you hadn't done that already in step 1.

3) In the EverLoader project file, increment the version. There are two ways you can do this:
    * In Visual Studio from the Solution Explorer window, right-click the EverLoader project (not the solution) and select `Properties`. Now search for the `Package Version` property and increment it.
    * or... close Visual Studio, open `EverLoader.csproj` in a text editor and increment the version found between the `<Version>` tags.

4) Now work on your new features and/or bug fixes.

5) Commit your changes and push them to the git branch you've created in step 1.

### Publishing
To publish an official Everloader release, follow these steps:

1) First you need to build the `EverLoader.exe` executable file: In Visual Studio, you need to right-click the "EverLoader" project (not the solution) and select "Publish". Note that this does not actually publish EverLoader, but just brings you to a screen that allows you to build & create the `EverLoader.exe` file on your local machine.
    * [In case you didn't create a publish profile before, you will need to create one]: \
   In the new publish profile, select the `Folder` target, click `Next` and select `Folder` again as specific target and click `Next` again. The folder location will show as `bin\Release\netcoreapp3.1\publish\`. Now click `Finish`. In the publish window under Settings, click `Show all settings` and select Deployment mode `Self-Contained`, target runtime `win-x86` and under File publish options, check `Produce single file`. Then `Save` the profile.
    * Click the `Publish` button on the top-right of the publish window and wait for the packaging/publishing to finish its work.
    * The target folder `bin\Release\netcoreapp3.1\publish\win-x86\` should now contain the file `EverLoader.exe`

2) In the target folder `bin\Release\netcoreapp3.1\publish\win-x86\`, double-click EverLoader.exe and verify that the "About EverLoader" window contains the correct new version number. Also verify that your new features still work correctly and check if the normal flow still works. Once you're happy with this version, zip the `EverLoader.exe` and name it `EverLoader-v[VERSION-NUMBER]-portable.zip`, where you should replace `[VERSION-NUMBER]` with the version number in step 1.

3) Go to the EverLoader github releases page and [Draft a new release](https://github.com/EverSD-Cart/EverLoader/releases/new).
    * Under dropdown 'Choose a tag' create a new tag `[VERSION-NUMBER]`
    * Under dropdown 'Target' select the corresponding branch `releases/[VERSION-NUMBER]`
    * Release title should be `EverLoader v[VERSION-NUMBER]` (this is not really required, but just the convention)
    * Create a description the new features/bugfixes in this release
    * Attach the `EverLoader-v[VERSION-NUMBER]-portable.zip` binary which you created in step 2.
    * Publish the release (or save it as draft if you want to publish it later)

4) After the release was published, you should merge your changes back to the main branch.

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
