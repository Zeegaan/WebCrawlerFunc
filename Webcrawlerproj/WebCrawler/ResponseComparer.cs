using HtmlAgilityPack;
using System.Linq;

namespace WebCrawler
{
    public class ResponseComparer
    {
        private PercentageCalculator _calc;
        public ResponseComparer()
        {
            _calc = new PercentageCalculator();
        }
        public bool CompareHtmlDocs(HtmlDocument docOne, HtmlDocument docTwo)
        {
            if (docOne.DocumentNode.InnerHtml == docTwo.DocumentNode.InnerHtml)
                return true;
            return false;
        }
        public bool CompareByteArrays(byte[] byteArrayOne, byte[] byteArrayTwo)
        {
            if (!byteArrayOne.SequenceEqual(byteArrayTwo))
            {
                return false;
            }
            return true;
        }
        public double CompareHtmlPercentage(string responseOne, string responseTwo)
        {
            if (string.IsNullOrEmpty(responseOne) || string.IsNullOrEmpty(responseTwo))
            {
                if (responseOne != responseTwo) return 0;
                return 1;
            }
            return _calc.CalculateSimilarity(responseOne, responseTwo);
        }
    }
}
