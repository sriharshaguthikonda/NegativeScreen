//Copyright 2011-2012 Melvyn Laily
//http://arcanesanctum.net

//This file is part of NegativeScreen.

//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

//You should have received a copy of the GNU General Public License
//along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace NegativeScreen
{
	class Program
	{
		private static readonly string LogPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "debug.log");
		
		private static void LogDebug(string message)
		{
			string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}";
			Console.WriteLine(logMessage);
			File.AppendAllText(LogPath, logMessage + Environment.NewLine);
		}

		[STAThread]
		static void Main(string[] args)
		{
			LogDebug("Application starting...");
			
			// Add DPI awareness before anything else
			Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
			Application.SetCompatibleTextRenderingDefault(false);
			Application.EnableVisualStyles();

			//check whether the current process is running under WoW64 mode
			if (NativeMethods.IsX86InWow64Mode())
			{
				LogDebug("Error: Running 32-bit version on 64-bit Windows");
				//see http://social.msdn.microsoft.com/Forums/en-US/windowsaccessibilityandautomation/thread/6cc761ea-8a54-4403-9cca-2fa8680f4409/
				System.Windows.Forms.MessageBox.Show(
@"You are trying to run this program on a 64 bits processor whereas it was compiled for a 32 bits processor.
To avoid known bugs relative to the used APIs, please instead run the 64 bits compiled version.", "Warning", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Exclamation, System.Windows.Forms.MessageBoxDefaultButton.Button1);
				return;
			}
			//check whether aero is enabled
			if (!NativeMethods.DwmIsCompositionEnabled())
			{
				LogDebug("Warning: Windows Aero is not enabled");
				var result = System.Windows.Forms.MessageBox.Show("Windows Aero should be enabled for this program to work properly!\nOtherwise, you may experience bad performances.", "Warning", System.Windows.Forms.MessageBoxButtons.OKCancel, System.Windows.Forms.MessageBoxIcon.Information, System.Windows.Forms.MessageBoxDefaultButton.Button1);
				if (result != System.Windows.Forms.DialogResult.OK)
				{
					return;
				}
			}
			LogDebug("Setting DPI awareness...");
			//without this call, and with custom DPI settings,
			//the magnified window is either partially out of the screen,
			//or blurry, if the transformation scale is forced to 1.
			NativeMethods.SetProcessDPIAware();
			try
			{
				LogDebug("Initializing OverlayManager...");
				var manager = new OverlayManager();
				LogDebug("OverlayManager initialized successfully");
				
				// Run the Windows message loop
				Application.Run(manager);  // This is crucial - run the form's message loop
				
				LogDebug("Application shutting down normally");
			}
			catch (Exception ex)
			{
				LogDebug($"Error: OverlayManager initialization failed: {ex}");
				MessageBox.Show($"Error initializing OverlayManager: {ex}", "Error", 
					MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}
	}
}