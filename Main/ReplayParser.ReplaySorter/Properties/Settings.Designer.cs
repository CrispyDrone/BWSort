﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ReplayParser.ReplaySorter.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.9.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string LOGDIRECTORY {
            get {
                return ((string)(this["LOGDIRECTORY"]));
            }
            set {
                this["LOGDIRECTORY"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int MAXUNDOLEVEL {
            get {
                return ((int)(this["MAXUNDOLEVEL"]));
            }
            set {
                this["MAXUNDOLEVEL"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool CHECKFORUPDATES {
            get {
                return ((bool)(this["CHECKFORUPDATES"]));
            }
            set {
                this["CHECKFORUPDATES"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool REMEMBERPARSINGDIRECTORY {
            get {
                return ((bool)(this["REMEMBERPARSINGDIRECTORY"]));
            }
            set {
                this["REMEMBERPARSINGDIRECTORY"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string LASTPARSINGDIRECTORY {
            get {
                return ((string)(this["LASTPARSINGDIRECTORY"]));
            }
            set {
                this["LASTPARSINGDIRECTORY"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool LOADREPLAYSONSTARTUP {
            get {
                return ((bool)(this["LOADREPLAYSONSTARTUP"]));
            }
            set {
                this["LOADREPLAYSONSTARTUP"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool PARSESUBDIRECTORIES {
            get {
                return ((bool)(this["PARSESUBDIRECTORIES"]));
            }
            set {
                this["PARSESUBDIRECTORIES"] = value;
            }
        }
    }
}
