# EverLoader

EverLoader is a Windows app that helps you to load games onto a MicroSD card to be played on your Evercade™ using an EverSD cartridge.

## 1. Adding Games and Scraping
When you add new games/roms to EverLoader (using the 'Add New ROMs' button), it will automatically take care of scraping the game descriptions, box art images, banners and other information from the Internet using the online TheGamesDB database.

## 2. Editing Game Info
After you've added new games to EverLoader, you can edit all the game information, like Title, Descripton, Genre, Release date, but also information on the button mappings. Besides that, you can upload custom box art or banners in case the scraper didn't find any images that you like. And if nothing was scraped, you can try the 'Scrape by game title' option, which shows you a list of possible matches - filtered by the console platform - for you to choose from. In case multiple banners are found, you can pick the one you like and move the cut-out up or down.

## 3. Sync with MicroSD
After you're done editing the game information, you can select the games you want to synchonize to a MicroSD card connected to your computer. EverLoader will copy the games and images in the correct format to your MicroSD, so you can play them on your Evercade™ using an EverSD cartridge.

## EverLoader is Portable
EverLoader is a portable Windows app, which means that after unzipping, you can run it from any drive or directory to which you have read/write access.
On usage, it creates a folder named `everloader_data` alongside the EverLoader executable to hold all games, images and other information.

Enjoy!

Disclaimer: EverSD and EverLoader are not affiliated in any way with EVERCADE™.
EverSD does not support or condone piracy.

# AppSettings.json

# Compiling Libretro cores for the Evercade

Most existing Libretro cores are written in C++, so you'll need to have a specific GCC-based toolchain on your machine to correctly compile for the Evercade. This is also known a "cross-compiling".

Luckily for us, someone already did the hard work and provided [instructions on how to build a C and C++ cross-compilation toolchain for the Evercade](https://github.com/strager/evercade-hacking/blob/master/toolchain.md). You'll need to build the toolchain from a Linux machine. If you're on Windows, you can use WSL. Or - if you're lazy like me - use the pre-configured Docker container.

Most Libretro cores contain a Makefile which supports the `classic_armv7_a7` platform using predefined flags and options. This is used to build for mini-consoles like the NES mini, SNES mini, C64 mini, etc. These builds are compatible with the Evercade, which also uses the ARMv7 + Neon GPU.

### Example: Building the Vice Libretro core
 * I'm assuming you have the cross-compilation toolchain binaries already available in the directory `/git/evercade-hacking/build/usr/bin`
 * Get the sources of the Vice Libretro core (without git history):\
 `git clone --depth 1 https://github.com/libretro/vice-libretro.git`
 * Now go into the vice-libretro directory (command `cd vice-libretro`) and run make, passing `platform`, `CC`, `CXX` and `AR` as environment variables:\
 ```make EMUTYPE=x64 platform=classic_armv7_a7 CC=/git/evercade-hacking/build/usr/bin/arm-linux-gnueabihf-gcc CXX=/git/evercade-hacking/build/usr/bin/arm-linux-gnueabihf-g++ AR=/git/evercade-hacking/build/usr/bin/arm-linux-gnueabihf-ar```\
 -> Note the additional `EMUTYPE=x64` parameter, which is specific for the Vice makefile to compile the (faster) x64 emulator, which works best on the Evercade.
  * Now wait for the build process to complete and then copy the resulting binary `vice_x64_libretro.so` to the RetroArch /cores subdirectory on your MicroSD card to be able to test your freshly built Vice x64 Libretro core!




