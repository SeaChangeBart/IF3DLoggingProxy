﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34014
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Logging3DproxyApp.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("8090")]
        public short ListenPort {
            get {
                return ((short)(this["ListenPort"]));
            }
            set {
                this["ListenPort"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("STBTS")]
        public string Resource1 {
            get {
                return ((string)(this["Resource1"]));
            }
            set {
                this["Resource1"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://10.9.201.213:8090/nordbro/3d")]
        public string EndPoint1 {
            get {
                return ((string)(this["EndPoint1"]));
            }
            set {
                this["EndPoint1"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("HLS")]
        public string Resource2 {
            get {
                return ((string)(this["Resource2"]));
            }
            set {
                this["Resource2"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("http://10.9.202.228:8090/nordbro/3dhls")]
        public string EndPoint2 {
            get {
                return ((string)(this["EndPoint2"]));
            }
            set {
                this["EndPoint2"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("d:\\Logs\\Proxy3D")]
        public string LogPath {
            get {
                return ((string)(this["LogPath"]));
            }
            set {
                this["LogPath"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("10")]
        public int TimeoutInSeconds {
            get {
                return ((int)(this["TimeoutInSeconds"]));
            }
            set {
                this["TimeoutInSeconds"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("503")]
        public int StatusCodeOnTimeout {
            get {
                return ((int)(this["StatusCodeOnTimeout"]));
            }
            set {
                this["StatusCodeOnTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("503")]
        public int StatusCodeOnPutPostTimeout {
            get {
                return ((int)(this["StatusCodeOnPutPostTimeout"]));
            }
            set {
                this["StatusCodeOnPutPostTimeout"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("503")]
        public int StatusCodeOnDeleteTimeout {
            get {
                return ((int)(this["StatusCodeOnDeleteTimeout"]));
            }
            set {
                this["StatusCodeOnDeleteTimeout"] = value;
            }
        }
    }
}
