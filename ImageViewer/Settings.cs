// TODO
// Add file watcher to settings file, to reload or create if deleted.
// Handle errors, like if something is spelled wrong.
using System;
using System.IO;
using System.Globalization;

namespace ImageViewer
{

    static public class Settings
    {
        static string SettingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Settings.xml");

        static public string ImageEditor;
        static public System.Windows.Media.SolidColorBrush Background;
        static public System.Drawing.Color BackgroundColor = System.Drawing.Color.FromArgb(32, 32, 32);

        static public void Load()
        {
            if (!File.Exists(SettingsFilePath))
            {
                Save();
                return;
            }
            ReadSettingsFile();
        }


        static void ReadSettingsFile()
        {
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
                        }
                    }
                }
            }
        }

        static System.Windows.Media.Color RGBColorFromCSV(string cvs_string)
        {
            var background_raw = cvs_string.Split(',');
            return System.Windows.Media.Color.FromRgb(byte.Parse(background_raw[0], CultureInfo.InvariantCulture), byte.Parse(background_raw[1], CultureInfo.InvariantCulture), byte.Parse(background_raw[2], CultureInfo.InvariantCulture));
        }

        static public void Save()
        {
            using (var settingsWriter = System.Xml.XmlWriter.Create(SettingsFilePath))
            {
                settingsWriter.WriteStartDocument();
                settingsWriter.WriteStartElement("Settings");
                settingsWriter.WriteStartElement("Processing");
                settingsWriter.WriteAttributeString("Editor", $"{ImageEditor}");
                settingsWriter.WriteEndElement();
                
                settingsWriter.WriteStartElement("Colors");
                settingsWriter.WriteAttributeString("Background", $"{BackgroundColor.R}, {BackgroundColor.G}, {BackgroundColor.B}");
                settingsWriter.WriteEndElement();

                settingsWriter.WriteEndElement();
                settingsWriter.WriteEndDocument();
            }
        }
    }
}
