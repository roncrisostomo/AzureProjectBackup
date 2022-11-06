namespace AzureProjectBackup;

using System;
using System.Net.Http.Headers;
using System.Text;

public static class RequestSender
{
    public static HttpResponseMessage SendRequest(string azureDevOpsOrganizationUrl, string credentials, string uri, string? jsonstr = null)
    {
        HttpResponseMessage response;
        using (var client = new HttpClient())
        {
            client.BaseAddress = new Uri(azureDevOpsOrganizationUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("User-Agent", "ManagedClientConsoleAppSample");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            if (jsonstr != null)
            {
                HttpContent body = new StringContent(jsonstr, Encoding.UTF8, "application/json");
                response = client.PostAsync(uri, body).Result;
            }
            else
            {
                response = client.GetAsync(uri).Result;
            }
        }
        return response;
    }
}
