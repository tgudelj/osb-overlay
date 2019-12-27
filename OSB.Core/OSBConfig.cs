using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSB {
    /// <summary>
    /// OSB settings
    /// </summary>
    public class OSBConfig {
        /// <summary>
        /// Default config file name
        /// </summary>
        static string DEFAULT_CONFIG_FILE = "config.json";
        public static string DEFAULT_APP_SUBFOLDER = "OSB";

        /// <summary>
        /// Constructor
        /// </summary>
        public OSBConfig()
        {
            Buttons = new List<OSBButton>();
            VJDeviceId = 1;
            MaskBitmap = null;
            X = 0;
            Y = 0;
            Width = 1024;
            Height = 768;
         }

        #region Properties
        /// <summary>
        /// VJOY device id to use. OSB will assign a VJOY button to each OSB button.
        /// Max number of buttons supported by VJoy at present is 128
        /// </summary>
        [JsonProperty("VJDeviceId")]
        public uint VJDeviceId { get; set; }

        /// <summary>
        /// Bitmap to use as overlay (image file name), determines the form shape.
        /// Areas in magenta color RGB(255,0,255) will be transparent
        /// </summary>
        [JsonProperty("maskBitmap")]
        public string MaskBitmap { get; set; }

        /// <summary>
        /// Form initial position x coordinate (pixels)
        /// </summary>
        [JsonProperty("x")]
        public int X { get; set; }

        /// <summary>
        /// Form initial position y coordinate (pixels)
        /// </summary>
        [JsonProperty("y")]
        public int Y { get; set; }

        /// <summary>
        /// Form width (pixels)
        /// </summary>
        [JsonProperty("width")]
        public int Width { get; set; }

        /// <summary>
        /// Form height (pixels)
        /// </summary>
        [JsonProperty("height")]
        public int Height { get; set; }

        /// <summary>
        /// Defines the appearance and position of the reload configuration button
        /// </summary>
        [JsonProperty("reloadButton")]
        public OSBButton ReloadButton { get; set; }

        /// <summary>
        /// Defines the appearance and position of the exit button
        /// </summary>
        [JsonProperty("exitButton")]
        public OSBButton ExitButton { get; set; }

        /// <summary>
        /// Defines the appearance and position of the load config button
        /// </summary>
        [JsonProperty("loadButton")]
        public OSBButton LoadButton { get; set; }

        /// <summary>
        /// List of On-screen-button definitions
        /// </summary>
        [JsonProperty("buttons")]
        public List<OSBButton> Buttons { get; set; }

        /// <summary>
        /// Directory path of the config file
        /// </summary>
        [JsonIgnore]
        public string ConfigDir {
            get; set;
        }

        /// <summary>
        /// Config file path
        /// </summary>
        [JsonIgnore]
        public string ConfigFilePath {
            get; set;
        }

        #endregion

        /// <summary>
        /// Reads the application configuration and returns the settings object
        /// </summary>
        /// <returns></returns>
        public static OSBConfig Get() {
            return Get(DEFAULT_CONFIG_FILE);
        }

        /// <summary>
        /// Reads the application configuration and returns the settings object
        /// </summary>
        /// <param name="configFile">Config file to use</param>
        /// <returns></returns>
        public static OSBConfig Get(string configFile)
        {
            if (File.Exists(configFile))
            {
                return ParseConfig(configFile);
            }
            else {
                configFile = Path.Combine(GetApplicationFolderPath(), configFile);
            }

            if (File.Exists(configFile))
            {
                return ParseConfig(configFile);
            }
            else
            {
                if (File.Exists(Path.Combine(GetApplicationFolderPath(), DEFAULT_CONFIG_FILE)))
                {
                    return ParseConfig(Path.Combine(GetApplicationFolderPath(), DEFAULT_CONFIG_FILE));
                }
                else {
                    OSBConfig defaultSettings = new OSBConfig();
                    Save(defaultSettings, DEFAULT_CONFIG_FILE);
                    return defaultSettings;
                }
            }
        }

        /// <summary>
        /// Parses the specified config file
        /// </summary>
        /// <param name="configFile">Config file path</param>
        /// <returns>Configuration</returns>
        static OSBConfig ParseConfig(string configFile) {
            string settingsJson = File.ReadAllText(configFile);
            OSBConfig config = JsonConvert.DeserializeObject<OSBConfig>(settingsJson);
            config.ConfigDir = Path.GetDirectoryName(configFile);
            config.ConfigFilePath = configFile;
            return config;
        }

        /// <summary>
        /// Persists user settings to file in My Documents\OSB folder
        /// </summary>
        /// <param name="settings"></param>
        public static void Save(OSBConfig settings) {
            if (!string.IsNullOrEmpty(settings.ConfigFilePath))
            {
                Save(settings, settings.ConfigFilePath);
            }
            else {
                Save(settings, DEFAULT_CONFIG_FILE);
            }
        }

        /// <summary>
        /// Persists user settings to file in My Documents\OSB folder
        /// </summary>
        /// <param name="settings">Settings object to save</param>
        /// <param name="fileName">file name where settings will be saved</param>
        public static void Save(OSBConfig settings, string fileName)
        {
            string settingsFilePath = Path.Combine(GetApplicationFolderPath(), fileName);
            File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(settings));
        }

        /// <summary>
        /// Gets the folder path where we keep application settings
        /// </summary>
        /// <returns></returns>
        public static string GetApplicationFolderPath() {
            string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), DEFAULT_APP_SUBFOLDER);
            //Ensure that the folder exists
            Directory.CreateDirectory(path);
            return path;
        }


    }
}
