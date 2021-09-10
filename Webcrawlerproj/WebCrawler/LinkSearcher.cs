using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WebCrawler
{
    public class LinkSearcher
    {
        private Uri _hostUrl;
        private HashSet<string> _checkedUris;
        private Queue<string> _currentLinksToVisit;
        private Queue<string> _nextLinksToVisit;
        public Queue<string> CurrentLinksToVisit { get { return _currentLinksToVisit; } }
        public Queue<string> NextLinksToVisit { get { return _nextLinksToVisit; } }
        public HashSet<string> CheckedUris { get { return _checkedUris; } }
        private ILogger _logger;

        public LinkSearcher(Uri hostUri, HashSet<string> checkedUris, Queue<string> linksToVisit, ILogger logger)
        {
            _hostUrl = hostUri;
            _checkedUris = checkedUris;
            _currentLinksToVisit = new Queue<string>();
            if (hostUri.AbsoluteUri == hostUri.GetLeftPart(UriPartial.Authority) || hostUri.AbsoluteUri == hostUri.GetLeftPart(UriPartial.Authority) + "/")
            {
                _checkedUris.Add("/");
            }
            _currentLinksToVisit = linksToVisit;
            _nextLinksToVisit = new Queue<string>();
            _logger = logger;
        }
        public void ScrapeUris(string url, HtmlDocument doc)
        {
            _checkedUris.Add(url);

            FindTagsAndPropertiesInHtmlDoc(doc, "a", "href");
            FindTagsAndPropertiesInHtmlDoc(doc, "script", "src");
            FindTagsAndPropertiesInHtmlDoc(doc, "link", "href");
            FindTagsAndPropertiesInHtmlDoc(doc, "img", "src");
        }
        private void FindTagsAndPropertiesInHtmlDoc(HtmlDocument doc, string tag, string prop)
        {
            //Find all elements with given tag, so we can loop through them
            var links = doc.DocumentNode.Descendants(tag).ToList();
            foreach (var link in links)
            {
                var url = link.ChildAttributes(prop).FirstOrDefault()?.Value;
                if (String.IsNullOrEmpty(url) || url.StartsWith("#")) continue;
                //Check if absolute link so we can check if its our site or not
                if (url.StartsWith("https") || url.StartsWith("http"))
                {
                    if (url.StartsWith(_hostUrl.GetLeftPart(UriPartial.Authority)))
                    {
                        url = url.Remove(0, _hostUrl.GetLeftPart(UriPartial.Authority).Length);
                        AddToVisited(url);
                    }
                    else
                    {
                        continue;
                    }
                }
                else if (!url.StartsWith("/"))
                {
                    url = "/" + url;
                    AddToVisited(url);
                }
                else
                {
                    AddToVisited(url);
                }

            }
        }
        private void AddToVisited(string url)
        {
            if (!_checkedUris.Contains(url))
            {
                if (!_currentLinksToVisit.Contains(url))
                {
                    _checkedUris.Add(url);
                    _nextLinksToVisit.Enqueue(url);
                    _logger.LogDebug($"Added link to visit queue: {url}", url);
                }
            }
        }
        public string GetNextUrl()
        {
            return _currentLinksToVisit.Dequeue();
        }
    }
}
