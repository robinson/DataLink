using System.Linq;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace DataLink.Ads.Client.Finder.Common
{
    public class RouteInfo
    {
        
        public string RouteName = NetworkInformation.GetHostNames().FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName ?? "???"; // Just a name of the new route
        public AdsNetId LocalAmsNetId;
        public bool IsTemporaryRoute= false;
        public string Login = "Administrator";
        public string Password = "1";
        public string Localhost = NetworkInformation.GetHostNames().FirstOrDefault(name => name.Type == HostNameType.DomainName)?.DisplayName ?? "???"; // IP or machine name
    }
}
