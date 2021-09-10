using System.Collections.Generic;

namespace WebCrawler
{
    public class WebCrawlerResult
    {
        public List<string> ErrorMessages { get; }
        public bool Result { get; }
        public WebCrawlerResult(bool result, List<string> errormessages)
        {
            Result = result;
            ErrorMessages = errormessages;
        }
    }
}
