﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Frame.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.8.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10, 10")]
        public global::System.Drawing.Point WindowLocation {
            get {
                return ((global::System.Drawing.Point)(this["WindowLocation"]));
            }
            set {
                this["WindowLocation"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("525, 350")]
        public global::System.Drawing.Size WindowSize {
            get {
                return ((global::System.Drawing.Size)(this["WindowSize"]));
            }
            set {
                this["WindowSize"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("0")]
        public int WindowState {
            get {
                return ((int)(this["WindowState"]));
            }
            set {
                this["WindowState"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string ImageEditor {
            get {
                return ((string)(this["ImageEditor"]));
            }
            set {
                this["ImageEditor"] = value;
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n<ArrayOfString xmlns:xsi=\"http://www.w3." +
            "org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\r\n  <s" +
            "tring>art</string>\r\n  <string>avs</string>\r\n  <string>bmp</string>\r\n  <string>cu" +
            "t</string>\r\n  <string>dcm</string>\r\n  <string>dcx</string>\r\n  <string>dib</strin" +
            "g>\r\n  <string>dpx</string>\r\n  <string>emf</string>\r\n  <string>epdf</string>\r\n  <" +
            "string>fax</string>\r\n  <string>fits</string>\r\n  <string>gif</string>\r\n  <string>" +
            "ico</string>\r\n  <string>mat</string>\r\n  <string>miff</string>\r\n  <string>mono</s" +
            "tring>\r\n  <string>mpc</string>\r\n  <string>mtv</string>\r\n  <string>otb</string>\r\n" +
            "  <string>p7</string>\r\n  <string>palm</string>\r\n  <string>pbm</string>\r\n  <strin" +
            "g>pcd</string>\r\n  <string>pcds</string>\r\n  <string>pcx</string>\r\n  <string>pdb</" +
            "string>\r\n  <string>pfa</string>\r\n  <string>pfb</string>\r\n  <string>pgm</string>\r" +
            "\n  <string>picon</string>\r\n  <string>pict</string>\r\n  <string>pix</string>\r\n  <s" +
            "tring>png</string>\r\n  <string>pnm</string>\r\n  <string>ppm</string>\r\n  <string>ps" +
            "d</string>\r\n  <string>ptif</string>\r\n  <string>pwp</string>\r\n  <string>rla</stri" +
            "ng>\r\n  <string>rle</string>\r\n  <string>sct</string>\r\n  <string>sfw</string>\r\n  <" +
            "string>sgi</string>\r\n  <string>sun</string>\r\n  <string>tga</string>\r\n  <string>t" +
            "if</string>\r\n  <string>tiff</string>\r\n  <string>tim</string>\r\n  <string>vicar</s" +
            "tring>\r\n  <string>viff</string>\r\n  <string>wbmp</string>\r\n  <string>wpg</string>" +
            "\r\n  <string>xbm</string>\r\n  <string>xcf</string>\r\n  <string>xpm</string>\r\n  <str" +
            "ing>dds</string>\r\n  <string>jpg</string>\r\n  <string>jpeg</string>\r\n</ArrayOfStri" +
            "ng>")]
        public global::System.Collections.Specialized.StringCollection SupportedExtensions {
            get {
                return ((global::System.Collections.Specialized.StringCollection)(this["SupportedExtensions"]));
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ReplaceImageOnDrop {
            get {
                return ((bool)(this["ReplaceImageOnDrop"]));
            }
            set {
                this["ReplaceImageOnDrop"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool ImageFullZoom {
            get {
                return ((bool)(this["ImageFullZoom"]));
            }
            set {
                this["ImageFullZoom"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool SplitChannelsBorder {
            get {
                return ((bool)(this["SplitChannelsBorder"]));
            }
            set {
                this["SplitChannelsBorder"] = value;
            }
        }
    }
}
