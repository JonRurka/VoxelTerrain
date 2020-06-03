using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using IniParser;
using IniParser.Parser;
using IniParser.Model;

namespace UnityGameServer
{
    public class AppSettings
    {
        public static class BaseDefaults
        {
            public static int TcpPort { get { return 11010; } }
            public static int UdpPort { get { return 11011; } }
            public static int TimeoutSeconds { get { return 300; } }
        }

        public static int TcpPort { get; private set; }
        public static int UdpPort { get; private set; }
        public static int TimeoutSeconds { get; private set; }

        private IniData _config;

        public IniData Config { get { return _config; } }

        public AppSettings(string file)
        {
            FileIniDataParser parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";
            if (File.Exists(file))
            {
                _config = parser.ReadFile(file);
            }
            else
                Logger.LogWarning("Config file not found!");
            LoadSettings();
        }

        public virtual void LoadSettings()
        {
            //<Setting> = TryGetValue("section", "setting", <Default Setting>);

            TcpPort = TryGetValue("General", "TcpPort", BaseDefaults.TcpPort);
            UdpPort = TryGetValue("General", "UdpPort", BaseDefaults.UdpPort);
            TimeoutSeconds = TryGetValue("General", "TimeoutSeconds", BaseDefaults.TimeoutSeconds);
        }

        public T GetSettingValue<T>(string section, string setting)
        {
            try
            {
                Type typeT = typeof(T);
                if (_config != null)
                {
                    if (_config.Sections.ContainsSection(section))
                    {
                        if (_config[section].ContainsKey(setting))
                        {
                            string value = _config[section][setting];
                            return (T)Convert.ChangeType(value, typeT);
                        }
                        else
                            Logger.LogError("Setting \"{0}\" does not exist in section \"{1}\".", setting, section);
                    }
                    else
                        Logger.LogError("Section \"{0}\" does not exist.", section);
                }
            }
            catch (Exception e)
            {
                Logger.Log("Failed loading {0}.{1}: {2}", section, setting, e.Message);
            }
            return default(T);
        }

        public bool ContainsSection(string section)
        {
            return _config != null && _config.Sections.ContainsSection(section);
        }

        public bool ContainsSetting(string section, string setting)
        {
            return _config != null && _config.Sections.ContainsSection(section) && _config[section].ContainsKey(setting);
        }

        public T TryGetValue<T>(string section, string setting, T defaultValue)
        {
            if (_config == null)
                return defaultValue;
            return ContainsSetting(section, setting) ? GetSettingValue<T>(section, setting) : defaultValue;
        }
    }
}
