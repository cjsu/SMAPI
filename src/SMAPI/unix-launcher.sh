#!/bin/bash
# MonoKickstart Shell Script
# Written by Ethan "flibitijibibo" Lee
# Modified for StardewModdingAPI by Viz and Pathoschild

# Move to script's directory
cd "`dirname "$0"`"

# Get the system architecture
UNAME=`uname`
ARCH=`uname -m`

# MonoKickstart picks the right libfolder, so just execute the right binary.
if [ "$UNAME" == "Darwin" ]; then
    # ... Except on OSX.
    export DYLD_LIBRARY_PATH=$DYLD_LIBRARY_PATH:./osx/

    # El Capitan is a total idiot and wipes this variable out, making the
    # Steam overlay disappear. This sidesteps "System Integrity Protection"
    # and resets the variable with Valve's own variable (they provided this
    # fix by the way, thanks Valve!). Note that you will need to update your
    # launch configuration to the script location, NOT just the app location
    # (i.e. Kick.app/Contents/MacOS/Kick, not just Kick.app).
    # -flibit
    if [ "$STEAM_DYLD_INSERT_LIBRARIES" != "" ] && [ "$DYLD_INSERT_LIBRARIES" == "" ]; then
        export DYLD_INSERT_LIBRARIES="$STEAM_DYLD_INSERT_LIBRARIES"
    fi

    # this was here before
    ln -sf mcs.bin.osx mcs

    # fix "DllNotFoundException: libgdiplus.dylib" errors when loading images in SMAPI
    if [ -f libgdiplus.dylib ]; then
        rm libgdiplus.dylib
    fi
    if [ -f /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib ]; then
        ln -s /Library/Frameworks/Mono.framework/Versions/Current/lib/libgdiplus.dylib libgdiplus.dylib
    fi

    # launch SMAPI
    cp StardewValley.bin.osx StardewModdingAPI.bin.osx
    open -a Terminal ./StardewModdingAPI.bin.osx $@
else
    # choose launcher
    LAUNCHER=""
    if [ "$ARCH" == "x86_64" ]; then
        ln -sf mcs.bin.x86_64 mcs
        cp StardewValley.bin.x86_64 StardewModdingAPI.bin.x86_64
        LAUNCHER="./StardewModdingAPI.bin.x86_64 $@"
    else
        ln -sf mcs.bin.x86 mcs
        cp StardewValley.bin.x86 StardewModdingAPI.bin.x86
        LAUNCHER="./StardewModdingAPI.bin.x86 $@"
    fi

    # get cross-distro version of POSIX command
    COMMAND=""
    if command -v command 2>/dev/null; then
        COMMAND="command -v"
    elif type type 2>/dev/null; then
        COMMAND="type"
    fi

    # open SMAPI in terminal
    # some terms (like terminator) have broken -e: https://bugs.launchpad.net/terminator/+bug/1458137 ignore it
    if $COMMAND x-terminal-emulator 2>/dev/null &&  x-terminal-emulator -v |grep -q -v 'terminator'; then
        x-terminal-emulator -e "$LAUNCHER"
    elif $COMMAND xfce4-terminal 2>/dev/null; then
        xfce4-terminal -e "$LAUNCHER"
    elif $COMMAND gnome-terminal 2>/dev/null; then
        gnome-terminal -e "$LAUNCHER"
    elif $COMMAND xterm 2>/dev/null; then
        xterm -e "$LAUNCHER"
    elif $COMMAND konsole 2>/dev/null; then
        konsole -e "$LAUNCHER"
    elif $COMMAND terminal 2>/dev/null; then
        terminal -e "$LAUNCHER"
    else
        $LAUNCHER
    fi

    # some Linux users get error 127 (command not found) from the above block, even though
    # `command -v` indicates the command is valid. As a fallback, launch SMAPI without a terminal when
    # that happens and pass in an argument indicating SMAPI shouldn't try writing to the terminal
    # (which can be slow if there is none).
    if [ $? -eq 127 ]; then
        $LAUNCHER --no-terminal
    fi
fi
