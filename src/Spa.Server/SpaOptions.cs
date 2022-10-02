using System.Collections.Generic;

namespace Spa.Server
{
    public class SpaOptions
    {
        public string UrlBasePath { get; set; }
        public string RootPath { get; set; }
        public string DefaultPage { get; set; }
        public int CacheDurationInMinutes { get; set; }
        public IList<string> PopulateEnvironmentVariablesInFiles { get; set; }
    }
}
