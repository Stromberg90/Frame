// TODO
// Add file watcher to settings file, to reload or create if deleted.
// Handle parsing errors, like if something is spelled wrong.
// Handle missing hotkeys or similar.
using System;
using System.Windows.Input;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace ImageViewer
{

    static public class Settings
    {
        static bool hasSettingsLoaded;
        static string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xml");
        const string DefaultSettings =
@"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings>
    <Processing Editor = ""null""/>
    <Colors Background = ""32, 32, 32"" />
    <Hotkeys>
        NextImage = ""Right""
        PreviousImage = ""Left""
        DeleteImage = ""Delete""
    </Hotkeys>
</Settings>";

        // TODO
        // So this isn't really working, if I remove this and try and load it from the file, I get a none value in release mode, but not in debug.
        // Implement save settings file.
        static public Key NextImage = Key.Right;
        static public Key PreviousImage = Key.Left;
        static public Key DeleteImage = Key.Delete;

        static public string ImageEditor;
        static public System.Windows.Media.SolidColorBrush Background;
        static public System.Drawing.Color BackgroundColor;

        static public void Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                File.WriteAllText(SettingsFilePath, DefaultSettings);
            }
            ReadSettingsFile();
        }


        static void ReadSettingsFile()
        {
            if (hasSettingsLoaded)
            {
                System.Threading.Thread.Sleep(10);
            }
            using (var SettingsReader = System.Xml.XmlReader.Create(SettingsFilePath))
            {
                while (SettingsReader.Read())
                {
                    if (SettingsReader.IsStartElement())
                    {
                        switch (SettingsReader.Name)
                        {
                            case "Processing":
                                while (SettingsReader.MoveToNextAttribute())
                                {
                                    switch (SettingsReader.Name)
                                    {
                                        case "Editor":
                                            ImageEditor = SettingsReader.GetAttribute(SettingsReader.Name);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                SettingsReader.MoveToElement();
                                break;
                            case "Colors":
                                while (SettingsReader.MoveToNextAttribute())
                                {
                                    switch (SettingsReader.Name)
                                    {
                                        case "Background":
                                            var color = RGBColorFromCSV(SettingsReader.GetAttribute(SettingsReader.Name));
                                            BackgroundColor = System.Drawing.Color.FromArgb(color.R, color.G, color.B);
                                            Background = new System.Windows.Media.SolidColorBrush(color);
                                            break;
                                    }
                                }
                                SettingsReader.MoveToElement();
                                break;
                            case "Hotkeys":
                                while (SettingsReader.MoveToNextAttribute())
                                {
                                    switch (SettingsReader.Name)
                                    {
                                        case "NextImage":
                                            NextImage = StringToKey(SettingsReader.Value);
                                            break;
                                        case "PreviousImage":
                                            PreviousImage = StringToKey(SettingsReader.Value);
                                            break;
                                        case "DeleteImage":
                                            DeleteImage = StringToKey(SettingsReader.Value);
                                            break;
                                        default:
                                            // TODO Add a message box and quit the program, or something else.
                                            throw new KeyNotFoundException($"Invalid Hotkey: {SettingsReader.Name} : {SettingsReader.Value}");
                                    }
                                }
                                SettingsReader.MoveToElement();
                                break;
                        }
                    }
                }
            }
            if (!hasSettingsLoaded)
            {
                hasSettingsLoaded = true;
            }
        }

        static System.Windows.Media.Color RGBColorFromCSV(string cvs_string)
        {
            var background_raw = cvs_string.Split(',');
            return System.Windows.Media.Color.FromRgb(byte.Parse(background_raw[0], CultureInfo.InvariantCulture), byte.Parse(background_raw[1], CultureInfo.InvariantCulture), byte.Parse(background_raw[2], CultureInfo.InvariantCulture));
        }

        static public void Save()
        {
            throw new NotImplementedException();
        }

        static Key StringToKey(string s)
        {
            // TODO Add more keys.
            switch (s)
            {
                case "A":
                    return Key.A;
                case "B":
                    return Key.B;
                case "R":
                    return Key.R;
                case "G":
                    return Key.G;
                case "Right":
                    return Key.Right;
                case "Left":
                    return Key.Left;
                case "Delete":
                    return Key.Delete;
                default:
                    throw new KeyNotFoundException("Invalid Key");
            }
        }
    }
}
