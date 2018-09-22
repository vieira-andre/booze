using System.Device.Location;

namespace Booze.Classes
{
    public class Push
    {
        public string content { get; set; }
        public GeoCoordinate geoCoordinate { get; set; }

        public Push(string content, GeoCoordinate geoCoordinate)
        {
            this.content = content;
            this.geoCoordinate = geoCoordinate;
        }
    }
}
