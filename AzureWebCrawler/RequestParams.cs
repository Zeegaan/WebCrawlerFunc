using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureWebCrawler
{
    public class RequestParams
    {
        public string OldUrl { get; set; }
        public string NewUrl { get; set; }
        public int Level { get; set; }
        public double PercentageEquals { get; set; }
        public bool ContinueIfNoMatch { get; set; }
        public Dictionary<string, IEnumerable<string>> DefaultRequestHeaders { get; set; }
    }
}
