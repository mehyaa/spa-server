using System;
using System.Collections.Generic;

namespace Spa.Server
{
    public class SpaOptions
    {
        public string RootPath { get; set; } = "wwwroot";
        public string DefaultPage { get; set; } = "index.html";
        public int CacheDurationInMinutes { get; set; } = 7 * 24 * 60;
        public IList<string> PopulateEnvironmentVariablesInFiles { get; set; } = Array.Empty<string>();
    }
}
