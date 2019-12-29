using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OSB.Core {
    public class UpdateChecker {
        public async void Check() {
            WebClient client = new WebClient();
            try
            {
                string updateInfoString = await client.DownloadStringTaskAsync("https://matricapp.com/update.json");
                UpdateInfo info = JsonConvert.DeserializeObject<UpdateInfo>(updateInfoString);
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                //Compare update info and current version
                if (version.Major < info.update.majorVersion)
                {
                    info.updateAvailable = true;
                }
                else
                {
                    if (version.Major <= info.update.majorVersion && version.Minor < info.update.minorVersion)
                    {
                        info.updateAvailable = true;
                    }
                }
                if (callback != null)
                {
                    await callback.ExecuteAsync(new object[] { info });
                }
            }
            catch
            {
                if (callback != null)
                {
                    await callback.ExecuteAsync(new object[] { null });
                }
            }
        }

    }
}
