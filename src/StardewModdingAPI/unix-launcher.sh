#!/bin/bash
# MonoKickstart Shell Script
# Written by Ethan "flibitijibibo" Lee
# Modified for StardewModdingAPI by Viz, Pathoschild and EnderHDMC

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

	ln -sf mcs.bin.osx mcs
	cp StardewValley.bin.osx StardewModdingAPI.bin.osx
	open -a Terminal ./StardewModdingAPI.bin.osx $@
else
	# check if "--disable-smapi" argument was passed (checks all arguments)
	if [[ $@ =~ "--disable-smapi" ]]; then
		# launch vanilla Stardew Valley
		./StardewValley-original $@
	else
		# launch smapi

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

		# check if "--disable-terminal" or "--disable-console" argument was passed (checks all arguments)
		if [[ $@ =~ --disable-terminal ]] || [[ $@ =~ --disable-console ]]; then
			# launch SMAPI without terminal to bypass a bug where steam does not launch the game when using terminal
			$LAUNCHER
		else
			# launch SMAPI normally (with terminal)

			# get cross-distro version of POSIX command
			COMMAND=""
			if command -v command 2>/dev/null; then
				COMMAND="command -v"
			elif type type 2>/dev/null; then
				COMMAND="type"
			fi

			# open SMAPI in terminal
			if $COMMAND x-terminal-emulator 2>/dev/null; then
				x-terminal-emulator -e "$LAUNCHER"
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
		fi
	fi

	# some Linux users get error 127 (command not found) from the above block, even though
	# `command -v` indicates the command is valid. As a fallback, launch SMAPI without a terminal when
	# that happens and pass in an argument indicating SMAPI shouldn't try writing to the terminal
	# (which can be slow if there is none).
	if [ $? -eq 127 ]; then
		$LAUNCHER --no-terminal
	fi
fi
