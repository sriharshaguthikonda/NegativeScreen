// Copyright 2011-2017 Melvyn Laïly
// [https://zerowidthjoiner.net](https://zerowidthjoiner.net)

// This file is part of NegativeScreen.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <[http://www.gnu.org/licenses/>](http://www.gnu.org/licenses/>).

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Security.Permissions;

namespace NegativeScreen
{
    class Configuration : IConfigurable
    {
        public string WorkingDirectoryConfigurationFileName { get; } =
            Path.Combine(Environment.CurrentDirectory, "negativescreen.conf");
        public string AppDataConfigurationFileName { get; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NegativeScreen/negativescreen.conf");
        public ConfigurationLocation Source { get; private set; } = ConfigurationLocation.Embedded;

        #region Default configuration
        public const string DefaultConfiguration =
@"# comments: if the character '#' is found, the rest of the line is ignored.
# WARNING: if the key is not valid, the program will probably crash...
Toggle=win+alt+N
Exit=win+alt+H

SmoothTransitions=true
SmoothToggles=true

# in miliseconds
MainLoopRefreshTime=100

InitialColorEffect=""Smart Inversion""

ActiveOnStartup=true

ShowAeroWarning=true

EnableApi=false

# If you do so, also remember to add an exception to your firewall.
ApiListeningUri=http://localhost:8990/

# where x is the value in the matrix.
Simple Inversion=win+alt+F1
{ -1,  0,  0,  0,  0 }
{  0, -1,  0,  0,  0 }
{  0,  0, -1,  0,  0 }
{  0,  0,  0,  1,  0 }
{  1,  1,  1,  0,  1 }

# Many thanks to Tom MacLeod who gave me the idea for these inversion modes.
Smart Inversion=win+alt+F2
{  0.3333333, -0.6666667, -0.6666667,  0.0000000,  0.0000000 }
{ -0.6666667,  0.3333333, -0.6666667,  0.0000000,  0.0000000 }
{ -0.6666667, -0.6666667,  0.3333333,  0.0000000,  0.0000000 }
{  0.0000000,  0.0000000,  0.0000000,  1.0000000,  0.0000000 }
{  1.0000000,  1.0000000,  1.0000000,  0.0000000,  1.0000000 }

# High saturation, good pure colors.
Smart Inversion Alt 1=win+alt+F3
{  1, -1, -1,  0,  0 }
{ -1,  1, -1,  0,  0 }
{ -1, -1,  1,  0,  0 }
{  0,  0,  0,  1,  0 }
{  1,  1,  1,  0,  1 }

# Overall desaturated, yellows and blue plain bad. actually relaxing and very usable.
Smart Inversion Alt 2=win+alt+F4
{  0.39, -0.62, -0.62,  0.00,  0.00 }
{ -1.21, -0.22, -1.22,  0.00,  0.00 }
{ -0.16, -0.16,  0.84,  0.00,  0.00 }
{  0.00,  0.00,  0.00,  1.00,  0.00 }
{  1.00,  1.00,  1.00,  0.00,  1.00 }

# High saturation. yellows and blues plain bad. actually quite readable.
Smart Inversion Alt 3=win+alt+F5
{  1.0895080, -0.9326327, -0.9326330,  0.0000000,  0.0000000 }
{ -1.8177180,  0.1683074, -1.8416920,  0.0000000,  0.0000000 }
{ -0.2445895, -0.2478156,  1.7621850,  0.0000000,  0.0000000 }
{  0.0000000,  0.0000000,  0.0000000,  1.0000000,  0.0000000 }
{  1.0000000,  1.0000000,  1.0000000,  0.0000000,  1.0000000 }

# Not so readable, good colors (CMY colors a bit desaturated, still more saturated than normal).
Smart Inversion Alt 4=win+alt+F6
{  0.50, -0.78, -0.78,  0.00,  0.00 }
{ -0.56,  0.72, -0.56,  0.00,  0.00 }
{ -0.94, -0.94,  0.34,  0.00,  0.00 }
{  0.00,  0.00,  0.00,  1.00,  0.00 }
{  1.00,  1.00,  1.00,  0.00,  1.00 }

Negative Sepia=win+alt+F7
{ -0.393, -0.349, -0.272,  0.000,  0.000 }
{ -0.769, -0.686, -0.534,  0.000,  0.000 }
{ -0.189, -0.168, -0.131,  0.000,  0.000 }
{  0.000,  0.000,  0.000,  1.000,  0.000 }
{  1.351,  1.203,  0.937,  0.000,  1.000 }

Negative Grayscale=win+alt+F8
{ -0.3, -0.3, -0.3,  0.0,  0.0 }
{ -0.6, -0.6, -0.6,  0.0,  0.0 }
{ -0.1, -0.1, -0.1,  0.0,  0.0 }
{  0.0,  0.0,  0.0,  1.0,  0.0 }
{  1.0,  1.0,  1.0,  0.0,  1.0 }

# Grayscaled
Negative Red=win+alt+F9
{ -0.3,  0.0,  0.0,  0.0,  0.0 }
{ -0.6,  0.0,  0.0,  0.0,  0.0 }
{ -0.1,  0.0,  0.0,  0.0,  0.0 }
{  0.0,  0.0,  0.0,  1.0,  0.0 }
{  1.0,  0.0,  0.0,  0.0,  1.0 }

# Grayscaled
Red=win+alt+F10
{  0.3,  0.0,  0.0,  0.0,  0.0 }
{  0.6,  0.0,  0.0,  0.0,  0.0 }
{  0.1,  0.0,  0.0,  0.0,  0.0 }
{  0.0,  0.0,  0.0,  1.0,  0.0 }
{  0.0,  0.0,  0.0,  0.0,  1.0 }

Grayscale=win+alt+F11
{ 0.3,  0.3,  0.3,  0.0,  0.0 }
{ 0.6,  0.6,  0.6,  0.0,  0.0 }
{ 0.1,  0.1,  0.1,  0.0,  0.0 }
{ 0.0,  0.0,  0.0,  1.0,  0.0 }
{ 0.0,  0.0,  0.0,  0.0,  1.0 }

# Red-Green Color Blindness   - Male Population: 1.01%, Female 0.02
Color blindness simulation: Protanopia (Red-Green Color Blindness)=
{ 0.567, 0.558, 0, 0, 0 }
{ 0.433, 0.442, 0.242, 0, 0 }
{ 0, 0, 0.758, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }

# Protanomaly (red-weak)  - Male Population: 1.08%, 0.03%
Color blindness simulation: Protanomaly (red-weak)=
{ 0.817, 0.333, 0, 0, 0 }
{ 0.183, 0.667, 0.125, 0, 0 }
{ 0, 0, 0.875, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }

# Deuteranopia (also called green-blind) - Male Population: 1%, Female Population: 0.1%
Color blindness simulation: Deuteranopia (green-blind)=
{ 0.625, 0.7, 0, 0, 0 }
{ 0.375, 0.3, 0.3, 0, 0 }
{ 0, 0, 0.7, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }

# Deuteranomaly (green-weak) - Male Population: 5%, Female Population: 0.35%
Color blindness simulation: Deuteranomaly (green-weak)=
{ 0.8, 0.258, 0, 0, 0 }
{ 0.2, 0.742, 0.142, 0, 0 }
{ 0, 0, 0.858, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }
# Tritanopia – Blue-Yellow Color Blindness - rare. Some sources estimate 0.008%
Color blindness simulation: Tritanopia (Blue-Yellow Color Blindness - rare)=
{ 0.95, 0, 0, 0, 0 }
{ 0.05, 0.433, 0.475, 0, 0 }
{ 0, 0.567, 0.525, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }

# Tritanomaly (blue-yellow weak) - Male 0.01%, Female 0.01%
Color blindness simulation: Tritanomaly (blue-yellow weak)=
{ 0.967, 0, 0, 0, 0 }
{ 0.033, 0.733, 0.183, 0, 0 }
{ 0, 0.267, 0.817, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }

# Total color blindness - Occurrences are estimated to be between 1 : 30’000 and 1 : 50’000.
Color blindness simulation: Achromatopsia (Total color blindness)=
{ 0.299, 0.299, 0.299, 0, 0 }
{ 0.587, 0.587, 0.587, 0, 0 }
{ 0.114, 0.114, 0.114, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }
Color blindness simulation: Achromatomaly (Total color weakness)=
{ 0.618, 0.163, 0.163, 0, 0 }
{ 0.32, 0.775, 0.32, 0, 0 }
{ 0.062, 0.062, 0.516, 0, 0 }
{ 0, 0, 0, 1, 0 }
{ 0, 0, 0, 0, 1 }

Binary (Black and white)=
{  127,  127,  127,  0,  0 }
{  127,  127,  127,  0,  0 }
{  127,  127,  127,  0,  0 }
{  0,  0,  0,  1,  0 }
{ -180, -180, -180,  0,  1 }
";
        #endregion

        private static Configuration? _current;
        public static Configuration Current
        {
            get
            {
                Initialize();
                return _current!;
            }
        }

        public static void Initialize()
        {
            if (_current == null)
            {
                _current = new Configuration();
            }
        }

        private Configuration()
        {
            ColorEffects = new List<KeyValuePair<HotKey, ScreenColorEffect>>();

            string configFileContent = ReadCurrentConfiguration();

            Parser.AssignConfiguration(configFileContent, this, new HotKeyParser(), new MatrixParser());
            if (!string.IsNullOrWhiteSpace(InitialColorEffectName))
            {
                try
                {
                    this.InitialColorEffect = this.ColorEffects.Single(x =>
                        x.Value.Description.ToLowerInvariant() == InitialColorEffectName.ToLowerInvariant()).Value;
                }
                catch (Exception)
                {
                    // probably not ideal
                    this.InitialColorEffect = new ScreenColorEffect(BuiltinMatrices.Negative, "Negative");
                }
            }
        }

        private string ReadCurrentConfiguration()
        {
            try
            {
                Source = ConfigurationLocation.AppData;
                return File.ReadAllText(AppDataConfigurationFileName);
            }
            catch (FileNotFoundException)
            {
                // File doesn't exist, try the next location
            }
            catch (UnauthorizedAccessException)
            {
                // No permission, try the next location
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading AppData config: {ex.Message}");
            }

            try
            {
                Source = ConfigurationLocation.WorkingDirectory;
                return File.ReadAllText(WorkingDirectoryConfigurationFileName);
            }
            catch (FileNotFoundException)
            {
                // File doesn't exist, use default
            }
            catch (UnauthorizedAccessException)
            {
                // No permission, use default
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading working directory config: {ex.Message}");
            }

            Source = ConfigurationLocation.Embedded;
            return DefaultConfiguration;
        }

        private void EnsureAppDataConfigurationFileExists()
        {
            string? directory = Path.GetDirectoryName(AppDataConfigurationFileName);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
            if (!File.Exists(AppDataConfigurationFileName))
            {
                File.WriteAllText(AppDataConfigurationFileName, DefaultConfiguration);
            }
        }

        public static void UserEditCurrentConfiguration()
        {
            void EditPath(string path)
            {
                Process.Start("notepad", path);
            }

            switch (Current.Source)
            {
                case ConfigurationLocation.AppData:
                    // If the source is AppData, use it:
                    EditPath(Current.AppDataConfigurationFileName);
                    break;
                case ConfigurationLocation.WorkingDirectory:
                case ConfigurationLocation.Embedded:
                default:
                    // If the source is the working directory, or no config file exists yet, try there first:
                    // (File.GetAccessControl() looks very scary and error prone, so we just try to open the file...)
                    try
                    {
                        if (!File.Exists(Current.WorkingDirectoryConfigurationFileName))
                        {
                            // create it with the default configuration template
                            File.WriteAllText(Current.WorkingDirectoryConfigurationFileName, DefaultConfiguration);
                        }
                        else
                        {
                            // just check we can access it
                            using (new FileStream(Current.WorkingDirectoryConfigurationFileName, FileMode.Open, FileAccess.ReadWrite)) { }
                        }
                        // Edit the working directory conf if we can
                        EditPath(Current.WorkingDirectoryConfigurationFileName);
                    }
                    catch (Exception)
                    {
                        // If we can't access the working directory conf, use the AppData one instead
                        Current.EnsureAppDataConfigurationFileExists();
                        EditPath(Current.AppDataConfigurationFileName);
                    }
                    break;
            }
        }

        [MatchingKey("Toggle", CustomParameter = HotKey.ToggleKeyId)]
        public HotKey ToggleKey { get; protected set; }

        [MatchingKey("Exit", CustomParameter = HotKey.ExitKeyId)]
        public HotKey ExitKey { get; protected set; }

        [MatchingKey("SmoothTransitions")]
        public bool SmoothTransitions { get; protected set; }

        [MatchingKey("SmoothToggles")]
        public bool SmoothToggles { get; protected set; }

        [MatchingKey("MainLoopRefreshTime", CustomParameter = 100)]
        public int MainLoopRefreshTime { get; protected set; }

        [MatchingKey("ActiveOnStartup", CustomParameter = true)]
        public bool ActiveOnStartup { get; protected set; }

        [MatchingKey("ShowAeroWarning", CustomParameter = true)]
        public bool ShowAeroWarning { get; protected set; }

        [MatchingKey("EnableApi", CustomParameter = false)]
        public bool EnableApi { get; protected set; }

        [MatchingKey("ApiListeningUri", CustomParameter = "http://localhost:8990/")]
        public string ApiListeningUri { get; protected set; } = "http://localhost:8990/";

        [MatchingKey("InitialColorEffect")]
        public string? InitialColorEffectName { get; protected set; }

        public ScreenColorEffect? InitialColorEffect { get; protected set; }

        public List<KeyValuePair<HotKey, ScreenColorEffect>> ColorEffects { get; }

        public void HandleDynamicKey(string key, string value)
        {
            // value is already trimmed
            if (value.StartsWith("{"))
            {
                // no hotkey
                this.ColorEffects.Add(new KeyValuePair<HotKey, ScreenColorEffect>(
                    HotKey.Empty,
                    new ScreenColorEffect(MatrixParser.StaticParseMatrix(value), key)));
            }
            else
            {
                // first part is the hotkey, second part is the matrix
                var splitted = value.Split(new char[] { '\n' }, 2);
                if (splitted.Length < 2)
                {
                    throw new Exception(string.Format(
                        "The value assigned to \"{0}\" is unexpected. The hotkey must be separated from the matrix by a new line.",
                        key));
                }
                this.ColorEffects.Add(new KeyValuePair<HotKey, ScreenColorEffect>(
                    HotKeyParser.StaticParse(splitted[0]),
                    new ScreenColorEffect(MatrixParser.StaticParseMatrix(splitted[1]), key)));
            }
        }
    }

    class HotKeyParser : ICustomParser
    {
        public Type ReturnType => typeof(HotKey);

        public object Parse(string rawValue, object customParameter)
        {
            int defaultId = -1;
            if (customParameter is int)
            {
                defaultId = (int)customParameter;
            }
            return StaticParse(rawValue, defaultId);
        }

        public static HotKey StaticParse(string rawValue, int defaultId = -1)
        {
            KeyModifiers modifiers = KeyModifiers.NONE;
            Keys key = Keys.None;
            string trimmed = rawValue.Trim();
            var splitted = trimmed.Split('+');
            foreach (var item in splitted)
            {
                switch (item.ToLowerInvariant())
                {
                    case "alt":
                        modifiers |= KeyModifiers.MOD_ALT;
                        break;
                    case "ctrl":
                        modifiers |= KeyModifiers.MOD_CONTROL;
                        break;
                    case "shift":
                        modifiers |= KeyModifiers.MOD_SHIFT;
                        break;
                    case "win":
                        modifiers |= KeyModifiers.MOD_WIN;
                        break;
                    default:
                        if (!Enum.TryParse(item, true, out key))
                        {
                            if (int.TryParse(item, out int numericValue))
                            {
                                if (Enum.IsDefined(typeof(Keys), numericValue))
                                {
                                    key = (Keys)numericValue;
                                }
                                else
                                {
                                    throw new Exception($"Invalid key code: {numericValue}");
                                }
                            }
                            else
                            {
                                throw new Exception($"Invalid key: {item}");
                            }
                        }
                        break;
                }
            }
            return new HotKey(modifiers, key, defaultId);
        }
    }

    class MatrixParser : ICustomParser
    {
        public Type ReturnType => typeof(float[,]);

        public object Parse(string rawValue, object customParameter)
            => StaticParseMatrix(rawValue);

        public static float[,] StaticParseMatrix(string rawValue)
        {
            float[,] matrix = new float[5, 5];
            var rows = System.Text.RegularExpressions.Regex.Matches(rawValue, @"{\s*(?<row>[^}]*)\s*}",
                System.Text.RegularExpressions.RegexOptions.ExplicitCapture);
            if (rows.Count != 5)
            {
                throw new Exception("The matrices must have 5 rows.");
            }
            for (int x = 0; x < rows.Count; x++)
            {
                var row = rows[x];
                var columnSplit = row.Groups["row"].Value.Split(',');
                if (columnSplit.Length != 5)
                {
                    throw new Exception("The matrices must have 5 columns.");
                }
                for (int y = 0; y < matrix.GetLength(1); y++)
                {
                    if (!float.TryParse(columnSplit[y],
                        System.Globalization.NumberStyles.Float,
                        System.Globalization.NumberFormatInfo.InvariantInfo,
                        out float value))
                    {
                        throw new Exception(string.Format("Unable to parse \"{0}\" to a float.", columnSplit[y]));
                    }
                    matrix[x, y] = value;
                }
            }
            return matrix;
        }
    }

    struct HotKey
    {
        public const int ToggleKeyId = 42;
        public const int ExitKeyId = 43;

        private static int CurrentId = 100;

        public static readonly HotKey Empty = new HotKey()
        {
            Id = 0,
            Key = Keys.None,
            Modifiers = KeyModifiers.NONE
        };

        public KeyModifiers Modifiers { get; private set; }
        public Keys Key { get; private set; }
        public int Id { get; private set; }

        public HotKey(KeyModifiers modifiers, Keys key, int id = -1)
            : this()
        {
            Modifiers = modifiers;
            Key = key & Keys.KeyCode;
            if (id == -1)
            {
                Id = CurrentId;
                CurrentId++;
            }
            else
            {
                Id = id;
            }
        }

        public override int GetHashCode()
            => (int)Key | (int)Modifiers << 16 | Id << 20;

        public override bool Equals(object? obj)
        {
            if (obj is not HotKey other)
            {
                return false;
            }
            return other.GetHashCode() == this.GetHashCode();
        }

        public static bool operator ==(HotKey a, HotKey b)
            => a.GetHashCode() == b.GetHashCode();

        public static bool operator !=(HotKey a, HotKey b)
            => a.GetHashCode() != b.GetHashCode();

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            if (Modifiers.HasFlag(KeyModifiers.MOD_ALT))
            {
                sb.Append("Alt+");
            }
            if (Modifiers.HasFlag(KeyModifiers.MOD_CONTROL))
            {
                sb.Append("Ctrl+");
            }
            if (Modifiers.HasFlag(KeyModifiers.MOD_SHIFT))
            {
                sb.Append("Shift+");
            }
            if (Modifiers.HasFlag(KeyModifiers.MOD_WIN))
            {
                sb.Append("Win+");
            }
            sb.Append(Enum.GetName(typeof(Keys), Key) ?? ((int)Key).ToString());
            return sb.ToString();
        }
    }

    struct ScreenColorEffect
    {
        public float[,] Matrix { get; }
        public string Description { get; }

        public ScreenColorEffect(float[,] matrix, string description)
            : this()
        {
            Matrix = matrix;
            Description = description;
        }
    }

    enum ConfigurationLocation
    {
        Embedded,
        AppData,
        WorkingDirectory,
    }
}