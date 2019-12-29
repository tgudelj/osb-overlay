using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OSB.Core
{
    //Info on latest version available for download
    public class UpdateInfo
    {
        public UpdateInfo()
        {
            majorVersion = -1;
            minorVersion = -1;
            description = "";
        }

        /// <summary>
        /// Major version
        /// </summary>
        public int majorVersion { get; set; }
        /// <summary>
        /// Minor version
        /// </summary>
        public int minorVersion { get; set; }
        /// <summary>
        /// Version description
        /// </summary>
        public string description { get; set; }
        /// <summary>
        /// Download URL
        /// </summary>
        public string downloadUrl { get; set; }
        /// <summary>
        /// True if update is available
        /// </summary>
        [JsonIgnore()]
        public bool UpdateAvailable {
            get {
                Version version = Assembly.GetEntryAssembly().GetName().Version;
                if (version.Major < majorVersion)
                {
                    return true;
                }
                else
                {
                    if (version.Major == majorVersion && version.Minor < minorVersion)
                    {
                        return true;
                    }
                }
                return false;
            }
        }
    }
}
