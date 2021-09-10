using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Linq;

namespace AzureWebCrawler
{
    public static class WebcrawlerFunc
    {
        [Function("WebCrawler")]
        public async static Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req,
            FunctionContext executionContext)
        {

            var logger = executionContext.GetLogger("WebCrawler");
            logger.LogInformation("C# HTTP trigger function processed a request.");

            var crawler = new WebCrawler.WebCrawler(logger);
            //Get requestbody
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            var data = JsonConvert.DeserializeObject<RequestParams>(requestBody);
            if(data is null)
            {
                var error = "No data was recieved in the body :(";
                return CreateErrorResponse(req, error);
                
            }
            if (String.IsNullOrEmpty(data.OldUrl) || String.IsNullOrEmpty(data.NewUrl))
            {
                var error = "One or both of the urls sent was empty";
                return CreateErrorResponse(req, error);
            }
            var result = await crawler.StartCrawlerAsync(new Uri(data.OldUrl), new Uri(data.NewUrl), data.DefaultRequestHeaders, data.Level, data.PercentageEquals == 0 ? 0.9 : data.PercentageEquals, data.ContinueIfNoMatch);
            if (!result.Result)
            {
                string errors = "";
                result.ErrorMessages.ForEach(x => errors += x + "\n");
                return CreateErrorResponse(req, errors, HttpStatusCode.OK);
            }
            return CreateResponse(req, result.Result.ToString());
        }
        private static HttpResponseData CreateResponse(HttpRequestData req, string name = "", IDictionary<string, IEnumerable<string>> defaultRequestHeaders = null)
        {
            var response = req.CreateResponse(HttpStatusCode.NoContent);
            response.WriteString($"Did the sites match? True/False: {name}");
            return response;
        }
        private static HttpResponseData CreateErrorResponse(HttpRequestData req, string error, HttpStatusCode status = HttpStatusCode.BadRequest)
        {
            var response = req.CreateResponse(status);
            response.WriteString(error);
            return response;
        }
    }
}
