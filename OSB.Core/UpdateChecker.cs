using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OSB.Core
{
    public static class UpdateChecker
    {
        public static int CHECK_TIMEOUT = 5000;
        /// <summary>
        /// Try to get the info on the latest available version
        /// </summary>
        /// <returns>UpdateInfo</returns>
        public static async Task<UpdateInfo> Check()
        {
            WebClient webClient = new WebClient();
            try
            {
                string updateInfoString = await webClient.DownloadStringTaskAsync(new Uri("https://matricapp.com/osb-update.json"));
                return JsonConvert.DeserializeObject<UpdateInfo>(updateInfoString);
            }
            catch (Exception ex)
            {
                UpdateInfo info = new UpdateInfo
                {
                    description = $@"Could not get update info {ex.Message}"
                };
                return info;
            }

        }
    }
}
