NegativeScreen

http://arcanesanctum.net/negativescreen/


Description:

NegativeScreen's main goal is to support your poor tearful eyes when enjoying the bright white interweb in a dark room.
This task is joyfully achieved by inverting the colors of your screen.

Unlike the Windows Magnifier, which is also capable of such color inversion,
NegativeScreen was specifically designed to be easy and convenient to use.

The application now includes a modern settings window to choose which monitors or windows are inverted and whether the app starts minimized.
You can give each monitor a custom name so similar displays are easier to tell apart. Names are tied to a persistent monitor identifier so they survive display reordering.


Features:

Invert screen's colors :)
Simple UI to select which monitors or windows to invert.

Different inversion modes, including "smart" modes,
allowing blacks and whites inversion, while keeping colors (about) the sames.

Exit the program from the tray icon menu.
Monitors are listed with their friendly names and resolutions to help identify similar displays.
You can also assign custom names to each monitor from the settings window, and these names are saved for future sessions.
Selected monitors and windows are remembered across application restarts.
"Open minimized on startup" preference is also saved.
Dark mode theme for the settings window.
Dark mode is enabled by default on first launch.
Press F2 in the monitor list to quickly rename a selected display.
Tray icon and settings list displays monitor indices for easier identification.
Settings window temporarily hides overlays so it stays visible on inverted monitors.
Exiting the application now cleans up all overlays and system event hooks so no stray processes remain.


Windows Aero must be enabled, or the program won't start.
This prevent some undesirable behaviours (black screens, 100% CPU usage...)
- Continuous integration with GitHub Actions uses the Node 20 runner to build
  the solution on Windows and uploads the Release artifacts.


Useful controls:

-Press Win+Alt+H to Halt the program immediately
-Press Win+Alt+N to toggle color inversion (mnemonic: Night vision :))

-Press Win+Alt+F1-to-F10 to change inversion mode:
	F1: standard inversion
	F2: smart inversion1 - theoretical optimal transfomation (but ugly desaturated pure colors)
	F3: smart inversion2 - high saturation, good pure colors
	F4: smart inversion3 - overall desaturated, yellows and blues plain bad. actually relaxing and very usable
	F5: smart inversion4 - high saturation. yellows and blues  plain bad. actually quite readable
	F6: smart inversion5 - not so readable. good colors. (CMY colors a bit desaturated, still more saturated than normal)
	F7: negative sepia
	F8: negative gray scale
	F9: negative red
	F10: red

-Press Win+Alt+Add to increase the refresh timer
	(go easier on the CPU and the GPU, but if set too high, can lead to annoying stuttering)
-Press Win+Alt+Substract to decrease the refresh timer
-Press Win+Alt+Multiply to Reset the timer to its default value

Default constants:
Increase/Decrease: +/-10ms
Reset timer: 10ms


Many thanks to Tom MacLeod who gave me the idea for the "smart" inversion mode :)


Enjoy!
