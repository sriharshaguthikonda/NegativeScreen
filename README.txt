NegativeScreen

http://arcanesanctum.net/negativescreen/


Description:

NegativeScreen's main goal is to support your poor tearful eyes when enjoying the bright white interweb in a dark room.
This task is joyfully achieved by inverting the colors of your screen.

Unlike the Windows Magnifier, which is also capable of such color inversion,
NegativeScreen was specifically designed to be easy and convenient to use.

The application now provides a simple settings dialog accessible from the system tray.


Features:

* Invert screen's colors :)
* Display names are shown in the tray menu and can be toggled individually
* Exit option available from the tray icon
* Settings GUI to choose active displays
* Selected displays are saved to a configuration file

Different inversion modes, including "smart" modes,
allowing blacks and whites inversion, while keeping colors (about) the sames.

Windows Aero must be enabled, or the program won't start.
This prevent some undesirable behaviours (black screens, 100% CPU usage...)
- Continuous integration with GitHub Actions builds the solution automatically on Windows.


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





lets see if this build works!
