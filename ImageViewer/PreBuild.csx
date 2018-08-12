using Microsoft.CodeAnalysis;
using System.IO;
using Microsoft.Build.Evaluation;
using System;
using System.Reflection;

var installerScript = File.ReadAllText("../Setup.iss");
var installerScriptLines = File.ReadAllLines("../Setup.iss");
foreach(var line in installerScriptLines)
{
    if(line.Contains("AppVersion="))
    {
        var oldVersionNumber = line.Split('=')[1];
        installerScript.Replace($"AppVersion={oldVersionNumber}", $"AppVersion={"1.6.5.0"}");
    }
    else if (line.Contains("AppVerName="))
    {
        installerScript.Replace(line, $"AppVerName=Frame {"1.6.5.0"}");
    }
}
File.WriteAllText("../Setup.iss", installerScript);

foreach (Document document in Project.Analysis.Documents)
{
    if (document.Name == "About.xaml.cs")
    {
        var xamlPath = Path.Combine(Path.GetDirectoryName(document.FilePath), "About.xaml");
        var csprojPath = Path.Combine(Path.GetDirectoryName(document.FilePath), "Frame.csproj");
        var text = File.ReadAllText(xamlPath);
        var lines = File.ReadAllLines(xamlPath);
        var csprojText = File.ReadAllLines(csprojPath);

        foreach (var line in csprojText)
        {
            if(line.Contains("AutoUpdater.NET, "))
            {
                var oldVersionNumber = string.Empty;
                foreach(var xamlLine in lines)
                {
                    if (xamlLine.Contains("Autoupdater.NET.Official("))
                    {
                        oldVersionNumber = xamlLine.Split('(')[1].Split(')')[0];
                    }
                }
                text = text.Replace(string.Format("Autoupdater.NET.Official({0}) by RBSoft", oldVersionNumber), string.Format("Autoupdater.NET.Official({0}) by RBSoft", line.Split('=')[2].Split(',')[0]));
            }
            else if (line.Contains("Magick.NET-Q16-x64, "))
            {
                var oldVersionNumber = string.Empty;
                foreach (var xamlLine in lines)
                {
                    if (xamlLine.Contains("Magick.NET("))
                    {
                        oldVersionNumber = xamlLine.Split('(')[1].Split(')')[0];
                    }
                }
                text = text.Replace(string.Format("Magick.NET({0}) by Dirk Lemstra", oldVersionNumber), string.Format("Magick.NET({0}) by Dirk Lemstra", line.Split('=')[2].Split(',')[0]));
            }
            else if (line.Contains("Xceed.Wpf.Toolkit, "))
            {
                var oldVersionNumber = string.Empty;
                foreach (var xamlLine in lines)
                {
                    if (xamlLine.Contains("Extended.Wpf.Toolkit("))
                    {
                        oldVersionNumber = xamlLine.Split('(')[1].Split(')')[0];
                    }
                }
                text = text.Replace(string.Format("Extended.Wpf.Toolkit({0}) by Xceed", oldVersionNumber), string.Format("Extended.Wpf.Toolkit({0}) by Xceed", line.Split('=')[2].Split(',')[0]));
            }
    }
        File.WriteAllText(xamlPath, text);
    }
}