using Microsoft.Extensions.Logging;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebCrawlerTest
{
    public class Tests
    {
        private ILogger _logger;
        [SetUp]
        public void Setup()
        {
            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole()
                .SetMinimumLevel(LogLevel.Trace);
            });
            _logger = loggerFactory.CreateLogger<Tests>();
        }

        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 4, 1)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 3, 0.9)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 2, 0.8)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 1, 0.7)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 0, 1)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 4)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 3)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 2)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 1)]
        //[TestCase(true, "https://localhost:44331/", "https://localhost:44331/", 0)]
        public async Task TestLocalSite(bool expectedResult, string urlOne, string urlTwo, int level, double percentageEquals = 0.9)
        {
            var defaultRequestHeaders = new Dictionary<string, IEnumerable<string>>();
            Uri newUri = new Uri(urlOne);
            Uri oldUri = new Uri(urlTwo);

            var webComp = new WebCrawler.WebCrawler(_logger);
            Assert.AreEqual(expectedResult, (await webComp.StartCrawlerAsync(newUri, oldUri, defaultRequestHeaders, level, percentageEquals)).Result);
        }
        //[TestCase(false, "http://xn--klinikforortopdkirurgi-p6b.dk/", "https://dev-klinik-for-ortopaedkirurgi.s1.umbraco.io/", 1, 1, true)]
        [TestCase(true, "https://relyonnutec.com", "https://relyonnutec.com", 0, 0.9, false)]
        [TestCase(false, "http://xn--klinikforortopdkirurgi-p6b.dk/", "http://xn--klinikforortopdkirurgi-p6b.dk/ansatte", 0, 0.9, false)]
        public async Task TestExternalSite(bool expectedResult, string urlOne, string urlTwo, int level, double percentageEquals, bool continueIfNoMatch)
        {
            var defaultRequestHeaders = new Dictionary<string, IEnumerable<string>>();
            Uri newUri = new Uri(urlOne);
            Uri oldUri = new Uri(urlTwo);
            var webComp = new WebCrawler.WebCrawler(_logger);
            Assert.AreEqual(expectedResult, (await webComp.StartCrawlerAsync(newUri, oldUri, defaultRequestHeaders, level, percentageEquals, continueIfNoMatch)).Result);
        }
    }
}