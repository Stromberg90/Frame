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

    public class Settings
    {
        bool hasSettingsLoaded;
        string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xml");
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
        public Dictionary<Command, Key> Hotkeys = new Dictionary<Command, Key>();
        public string ImageEditor;
        public System.Windows.Media.SolidColorBrush Background;
        public System.Drawing.Color BackgroundColor;

        public void Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                File.WriteAllText(SettingsFilePath, DefaultSettings);
            }
            ReadSettingsFile();
        }


        void ReadSettingsFile()
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
                                if (hasSettingsLoaded)
                                {
                                    Hotkeys.Clear();
                                }
                                while (SettingsReader.MoveToNextAttribute())
                                {
                                    switch (SettingsReader.Name)
                                    {
                                        case "NextImage":
                                            Hotkeys.Add(Command.NextImage, StringToKey(SettingsReader.Value));
                                            break;
                                        case "PreviousImage":
                                            Hotkeys.Add(Command.PreviousImage, StringToKey(SettingsReader.Value));
                                            break;
                                        case "DeleteImage":
                                            Hotkeys.Add(Command.DeleteImage, StringToKey(SettingsReader.Value));
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
