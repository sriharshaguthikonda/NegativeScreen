# NegativeScreen #

https://zerowidthjoiner.net/negativescreen

***

## Description

NegativeScreen's main goal is to support your poor tearful eyes when enjoying the bright white interweb in a dark room.
This task is joyfully achieved by inverting the colors of your screen.

Unlike the Windows Magnifier, which is also capable of such color inversion,
NegativeScreen was specifically designed to be easy and convenient to use.

It comes with a minimal graphic interface in the form of a system tray icon with a context menu,
but don't worry, this only makes it easier to use!


## Features

Invert screen's colors.

Additionally, many color effects can be applied.
For example, different inversion modes, including "smart" modes,
swapping blacks and whites while keeping colors (about) the sames.

You can now configure the color effects manually via a configuration file.
You can also configure the hot keys for every actions, using the same configuration file.

A basic web api is part of NegativeScreen >= 2.5.
It is disabled by default. When enabled, it listens by default on port 8990, localhost only.
See the configuration file to enable the api or change the listening uri...

All commands must be sent with the POST http method.
The following commands are implemented:
- TOGGLE
- ENABLE
- DISABLE
- SET "Color effect name" (without the quotes)

Any request sent with a method other than POST will not be interpreted,
and a response containing the application version will be sent.


## Requirements

NegativeScreen < 2.0 needs at least Windows Vista to run.

Versions 2.0+ need at least Windows 7.

Both run on Windows 8 or superior.

Graphic acceleration (Aero) must be enabled.

.NET 9 is required. Install the latest .NET Desktop Runtime 9.0 on your system.


## Default controls

- Press <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>H</kbd> to Halt the program immediately
- Press <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>N</kbd> to toggle color inversion (mnemonic: Night vision :))
- Press <kbd>Win</kbd>+<kbd>Alt</kbd>+<kbd>F1</kbd>-to-<kbd>F11</kbd> to switch between inversion modes:
	* <kbd>F1</kbd>: standard inversion
	* <kbd>F2</kbd>: smart inversion1 - theoretical optimal transformation (but ugly desaturated pure colors)
	* <kbd>F3</kbd>: smart inversion2 - high saturation, good pure colors
	* <kbd>F4</kbd>: smart inversion3 - overall desaturated, yellows and blues plain bad. actually relaxing and very usable
	* <kbd>F5</kbd>: smart inversion4 - high saturation. yellows and blues  plain bad. actually quite readable
	* <kbd>F6</kbd>: smart inversion5 - not so readable. good colors. (CMY colors a bit desaturated, still more saturated than normal)
	* <kbd>F7</kbd>: negative sepia
	* <kbd>F8</kbd>: negative gray scale
	* <kbd>F9</kbd>: negative red
	* <kbd>F10</kbd>: red
	* <kbd>F11</kbd>: grayscale


## Configuration file

A customizable configuration file is created the first time you use "Edit Configuration" from the context menu.

The default location for this file is next to NegativeScreen.exe, and is called "negativescreen.conf"

If the default location is inaccessible,
NegativeScreen will try to create the configuration file in %AppData%/NegativeScreen/negativescreen.conf

This feature allows to deploy NegativeScreen.exe in a read-only location for unprivileged users.
Each user can then have its own configuration file.

The order of priority for trying to read a configuration file when starting NegativeScreen is as follows:
- %AppData%/NegativeScreen/negativescreen.conf
- negativescreen.conf in the directory where NegativeScreen.exe is located
- If the above fails, the embedded default configuration is used

Should something go wrong (syntax error, bad hot key...), you can simply delete the configuration file,
the internal default configuration will be used.

If the configuration file is missing, you can use the "Edit Configuration" menu to regenerate the default one.

Syntax: see in the configuration file...


***

Many thanks to Tom MacLeod who gave me the idea for the "smart" inversion mode :)


Enjoy!
