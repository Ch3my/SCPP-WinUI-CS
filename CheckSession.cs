using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace SCPP_WinUI_CS
{
    class CheckSession
    {
        async public static Task<bool> CheckValid()
        {
            try
            {
                HttpResponseMessage response = await App.httpClient.GetAsync(
                   $"/check-session?sessionHash={App.sessionHash}"
                   );

                JsonObject checkSession = JsonSerializer.Deserialize<JsonObject>(response.Content.ReadAsStringAsync().Result);

                if (checkSession.ContainsKey("hasErrors"))
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                FileLogger.AppendToFile(ex.Message);
                return false;
            }

            return true;
        }
    }
}
