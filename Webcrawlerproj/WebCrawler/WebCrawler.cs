using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WebCrawler;
using Microsoft.Extensions.Logging;

namespace WebCrawler
{
    public class WebCrawler
    {
        private ResponseComparer _comparer;
        private LinkSearcher _linkSearcher;
        private List<string> _errorMessages;
        private ILogger _logger;
        private bool _runPercentageChecks;
        private bool _continueIfNoMatch;
        private bool _endResult;
        public WebCrawler(ILogger logger)
        {
            _comparer = new ResponseComparer();
            _errorMessages = new List<string>();
            _logger = logger;
        }
        public async Task<WebCrawlerResult> StartCrawlerAsync(Uri domainOld, Uri domainNew, IDictionary<string, IEnumerable<string>> defaultRequestHeaders = null, int level = 0, double percentageEquals = 0.9, bool continueIfNoMatch = false)
        {
            _endResult = true;
            _continueIfNoMatch = continueIfNoMatch;
            ValidateAndUpdatePercentageEquals(percentageEquals);
            var checkedUris = new HashSet<string>();
            var nextLinksToVisit = new Queue<string>();
            //Initialize the link searcher and CompareResponse so we have links to visit in EnsureResponseIdentical
            _linkSearcher = new LinkSearcher(domainOld, checkedUris, nextLinksToVisit, _logger);
            if (!await CompareResponse(domainOld, domainNew, level, percentageEquals, defaultRequestHeaders) && _continueIfNoMatch == false) return new WebCrawlerResult(false, _errorMessages);
            //Compare initial URIs this also loads all links on the sites in linksearcher
            if (!await EnsureResponseIdentical(domainOld, domainNew, level, checkedUris, _linkSearcher.NextLinksToVisit, percentageEquals, defaultRequestHeaders) && _continueIfNoMatch == false) return new WebCrawlerResult(false, _errorMessages);
            return new WebCrawlerResult(_endResult, _errorMessages);
        }
        private void ValidateAndUpdatePercentageEquals(double percentageEquals)
        {
            if (percentageEquals < 0 || percentageEquals > 1)
            {
                percentageEquals = 1;
            }
            if (percentageEquals == 1)
            {
                _runPercentageChecks = false;
            }
            else
            {
                _runPercentageChecks = true;
            }
        }
        private async Task<HttpResponseMessage> GetResponse(string url, IDictionary<string, IEnumerable<string>> defaultRequestHeaders)
        {
            var httpClient = new HttpClient();
            //check if string is empty so we can make an AuthenticationHeader if it isnt, this will break if string is empty
            if (defaultRequestHeaders is not null || defaultRequestHeaders.Count != 0)
            {
                foreach (var header in defaultRequestHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }

            return await httpClient.GetAsync(url);
        }
        private async Task<bool> EnsureResponseIdentical(Uri domainOld, Uri domainNew, int currentLevel, HashSet<string> checkedUris, Queue<string> linksToVisit, double percentageEquals, IDictionary<string, IEnumerable<string>> defaultRequestHeaders)
        {
            if (currentLevel < 0) return true;
            _linkSearcher = new LinkSearcher(domainOld, checkedUris, linksToVisit, _logger);
            //Loop through all the links and scour pages for links
            while (_linkSearcher.CurrentLinksToVisit.Count > 0)
            {
                var localPath = _linkSearcher.GetNextUrl();
                var oldLinkPath = new Uri(domainOld.GetLeftPart(UriPartial.Authority) + localPath);
                var newLinkPath = new Uri(domainNew.GetLeftPart(UriPartial.Authority) + localPath);
                if (!await CompareResponse(oldLinkPath, newLinkPath, currentLevel - 1, percentageEquals, defaultRequestHeaders) && _continueIfNoMatch == false) return false;
            }

            if (!await EnsureResponseIdentical(domainOld, domainNew, currentLevel - 1, checkedUris, _linkSearcher.NextLinksToVisit, percentageEquals, defaultRequestHeaders) && _continueIfNoMatch == false) return false;
            return true;
        }
        private HtmlDocument CreateHtmlDoc(string htmlString)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(htmlString);
            return htmlDocument;
        }
        private async Task<bool> CompareResponse(Uri domainOld, Uri domainNew, int currentLevel, double percentageEquals, IDictionary<string, IEnumerable<string>> defaultRequestHeaders)
        {
            var responseOld = await GetResponse(domainOld.AbsoluteUri, defaultRequestHeaders);
            _logger.LogDebug($"Visited site: {domainOld.AbsoluteUri}");
            var responseNew = await GetResponse(domainNew.AbsoluteUri, defaultRequestHeaders);
            _logger.LogDebug($"Visited site: {domainNew.AbsoluteUri}");
            //Check if status code are equal else we can return false instantly for performance
            if (responseOld.StatusCode != responseNew.StatusCode)
            {
                _errorMessages.Add($"Error occured: {domainOld.AbsoluteUri} gave Status Code: {responseOld.StatusCode} & {domainNew.AbsoluteUri} gave Status Code: {responseNew.StatusCode}. They were expected to match.");
                return false;
            }
            if (responseOld.Content.Headers.ContentType.MediaType != responseNew.Content.Headers.ContentType.MediaType)
            {
                _errorMessages.Add($"{domainOld.AbsoluteUri} returned {responseOld.Content.Headers.ContentType.MediaType} but {domainNew.AbsoluteUri} returned {responseNew.Content.Headers.ContentType.MediaType}. They were expected to match");
                return false;
            }
            if (IsResponseHtml(responseOld))
            {
                var resultOld = await responseOld.Content.ReadAsStringAsync();
                var resultNew = await responseNew.Content.ReadAsStringAsync();
                var docOld = CreateHtmlDoc(resultOld);
                var docNew = CreateHtmlDoc(resultNew);
                if (!_comparer.CompareHtmlDocs(docOld, docNew) && _runPercentageChecks == true)
                {
                    _logger.LogTrace($"Comparing {docOld.DocumentNode.InnerHtml} with {docNew.DocumentNode.InnerHtml}");
                    //Remove forms and compare again, we do this so we dont have to run the taxing CompareHtmlPercentage unless necessary
                    RemoveForms(docOld);
                    RemoveForms(docNew);
                    if (!_comparer.CompareHtmlDocs(docOld, docNew))
                    {
                        _logger.LogTrace($"Removed forms from {docOld.DocumentNode.InnerHtml} and {docNew.DocumentNode.InnerHtml} but they are still different");
                        var result = _comparer.CompareHtmlPercentage(resultOld, resultNew);
                        if (result < percentageEquals)
                        {
                            _errorMessages.Add($"{domainOld.AbsoluteUri} and {domainNew.AbsoluteUri} only matched {result * 100}%, and therefore failed.");
                            if (_continueIfNoMatch != true)
                                return false;
                            else _endResult = false;
                        }
                        _logger.LogInformation("Website: {domainOld} and {domainNew} were {result}% equals", domainOld.AbsoluteUri, domainNew.AbsoluteUri, result * 100);
                    }

                }
                else if (!_comparer.CompareHtmlDocs(docOld, docNew) && _runPercentageChecks == false && _continueIfNoMatch == false)
                {
                    _errorMessages.Add($"{domainOld.AbsoluteUri} & {domainNew.AbsoluteUri} did not match");
                    if (_continueIfNoMatch != true)
                        return false;
                    else _endResult = false;
                }
                else if (!_comparer.CompareHtmlDocs(docOld, docNew) && _continueIfNoMatch == true) 
                {
                    _errorMessages.Add($"{domainOld.AbsoluteUri} & {domainNew.AbsoluteUri} did not match");
                    _logger.LogError($"{domainOld.AbsoluteUri} & {domainNew.AbsoluteUri} did not match");
                    _endResult = false;
                }
                //scrape doc for links
                if (currentLevel > 0) _linkSearcher.ScrapeUris(domainOld.AbsoluteUri, docOld);
            }
            else
            {
                var byteArrayOld = await responseOld.Content.ReadAsByteArrayAsync();
                var byteArrayNew = await responseNew.Content.ReadAsByteArrayAsync();
                if (!_comparer.CompareByteArrays(byteArrayOld, byteArrayNew))
                {
                    _errorMessages.Add($"{domainNew.AbsoluteUri} were compared with {domainOld.AbsoluteUri} as byte arrays and did not match.");
                    if (_continueIfNoMatch != true)
                        return false;
                    else _endResult = false;
                }
            }
            return true;
        }
        private bool IsResponseHtml(HttpResponseMessage response)
        {
            if (response.Content.Headers.ContentType.MediaType == "text/html")
            {
                return true;
            }
            return false;
        }
        private void RemoveForms(HtmlDocument doc)
        {
            doc.DocumentNode.Descendants()
                        .Where(n => n.Name == "form" || (n.Name == "input" && n.Attributes.Contains("name") && n.Attributes["name"].Value == "ufrpt"))
                        .ToList()
                        .ForEach(n => n.Remove());
        }
    }
}
