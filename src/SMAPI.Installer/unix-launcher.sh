#!/usr/bin/env bash
# MonoKickstart Shell Script
# Written by Ethan "flibitijibibo" Lee
# Modified for SMAPI by various contributors

# Move to script's directory
cd "$(dirname "$0")" || exit $?

# Get the system architecture
UNAME=$(uname)
ARCH=$(uname -m)

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
    open -a Terminal ./StardewModdingAPI.bin.osx "$@"
else
    # choose launcher
    LAUNCHER=""
    if [ "$ARCH" == "x86_64" ]; then
        ln -sf mcs.bin.x86_64 mcs
        cp StardewValley.bin.x86_64 StardewModdingAPI.bin.x86_64
        LAUNCHER="./StardewModdingAPI.bin.x86_64 $*"
    else
        ln -sf mcs.bin.x86 mcs
        cp StardewValley.bin.x86 StardewModdingAPI.bin.x86
        LAUNCHER="./StardewModdingAPI.bin.x86 $*"
    fi

    # get cross-distro version of POSIX command
    COMMAND=""
    if command -v command 2>/dev/null; then
        COMMAND="command -v"
    elif type type 2>/dev/null; then
        COMMAND="type"
    fi

    # select terminal (prefer xterm for best compatibility, then known supported terminals)
    for terminal in xterm gnome-terminal kitty terminator xfce4-terminal konsole terminal termite alacritty mate-terminal x-terminal-emulator; do
        if $COMMAND "$terminal" 2>/dev/null; then
            # Find the true shell behind x-terminal-emulator
            if [ "$(basename "$(readlink -f $(which "$terminal"))")" != "x-terminal-emulator" ]; then
                export LAUNCHTERM=$terminal
                break;
            else
                export LAUNCHTERM="$(basename "$(readlink -f $(which x-terminal-emulator))")"
                # Remember that we're using x-terminal-emulator just in case it points outside the $PATH
                export XTE=1
                break;
            fi
        fi
    done

    # if no terminal was found, run in current shell or with no output
    if [ -z "$LAUNCHTERM" ]; then
        sh -c 'TERM=xterm $LAUNCHER'
        if [ $? -eq 127 ]; then
            $LAUNCHER --no-terminal
        fi
        exit
    fi

    # run in selected terminal and account for quirks
    case $LAUNCHTERM in
        terminator)
            # Terminator converts -e to -x when used through x-terminal-emulator for some reason
            if $XTE; then
                terminator -e "sh -c 'TERM=xterm $LAUNCHER'"
            else
                terminator -x "sh -c 'TERM=xterm $LAUNCHER'"
            fi
            ;;
        kitty)
            # Kitty overrides the TERM varible unless you set it explicitly
            kitty -o term=xterm $LAUNCHER
            ;;
        alacritty)
            # Alacritty doesn't like the double quotes or the variable
            if [ "$ARCH" == "x86_64" ]; then
                alacritty -e sh -c 'TERM=xterm ./StardewModdingAPI.bin.x86_64 $*'
            else
                alacritty -e sh -c 'TERM=xterm ./StardewModdingAPI.bin.x86 $*'
            fi
            ;;
        xterm|xfce4-terminal|gnome-terminal|terminal|termite|mate-terminal)
            $LAUNCHTERM -e "sh -c 'TERM=xterm $LAUNCHER'"
            ;;
        konsole)
            konsole -p Environment=TERM=xterm -e "$LAUNCHER"
            ;;
        *)
            # If we don't know the terminal, just try to run it in the current shell.
            sh -c 'TERM=xterm $LAUNCHER'
            # if THAT fails, launch with no output
            if [ $? -eq 127 ]; then
                $LAUNCHER --no-terminal
            fi
    esac
fi
