using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace SCPP_WinUI_CS.Helpers
{
    public class HttpRetry
    {
        public static async Task<HttpResponseMessage> ExecuteWithRetry(Func<Task<HttpResponseMessage>> operation, int maxRetries = 3, int retryDelayMilliseconds = 1000)
        {
            HttpResponseMessage response = null;
            int retryCount = 0;

            while (retryCount < maxRetries)
            {
                try
                {
                    response = await operation();

                    if (response.IsSuccessStatusCode)
                    {
                        // Request was successful, break out of the loop
                        break;
                    }
                    else
                    {
                        // Handle non-successful response (optional)
                        FileLogger.AppendToFile("HttpRetry, " + response.RequestMessage.RequestUri + " API respondio: status "
                        + response.StatusCode + " " + response.ReasonPhrase);
                    }
                }
                catch (Exception ex)
                {
                    // Handle exception (optional)
                }

                // Increment retry count
                retryCount++;

                // Wait before retrying
                await Task.Delay(retryDelayMilliseconds);
            }

            return response;
        }
    }
}
