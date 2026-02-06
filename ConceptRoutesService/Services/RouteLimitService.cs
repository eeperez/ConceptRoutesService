using ConceptRoutesService.Dtos;
using NetTopologySuite.Geometries;

namespace ConceptRoutesService.Services
{
    public class RouteLimitService : IRouteLimitService
    {
        private readonly GeometryFactory geometryFactory;

        public RouteLimitService(GeometryFactory geometryFactory)
        {
            this.geometryFactory = geometryFactory;
        }

        public Polygon GetLimitPolygon()
        {
            Coordinate[] puntosPeriferico = new[] {
                new Coordinate(-89.65124254997441, 21.043595171377078),
                 new Coordinate(-89.66381688418458, 21.01324333863505),
                 new Coordinate(-89.66579278674719, 21.00758195965939),
                 new Coordinate(-89.67429151117238, 20.993786992456663),
                 new Coordinate(-89.68037487826011, 20.984582363535893),
                 new Coordinate(-89.68387873823478, 20.979554137497445),
                 new Coordinate(-89.68932239952127, 20.976383770129615),
                 new Coordinate(-89.69098672699361, 20.966481657938658),
                 new Coordinate(-89.7025218312982, 20.95063083725401),
                 new Coordinate(-89.67413347609642, 20.899635032042326),
                 new Coordinate(-89.66943369052446, 20.895335848755153),
                 new Coordinate(-89.66621013949981, 20.893676928355305),
                 new Coordinate(-89.66253955043274, 20.893170543577853),
                 new Coordinate(-89.65868315295178, 20.8921532170732),
                 new Coordinate(-89.65451936298132, 20.89303536515115),
                 new Coordinate(-89.62294765798404, 20.909035048311367),
                 new Coordinate(-89.61820057404265, 20.912846120012247),
                 new Coordinate(-89.57180447344359, 20.947723583322954),
                 new Coordinate(-89.56899526299304, 20.950695184197187),
                 new Coordinate(-89.56812540028567, 20.953684862335862),
                 new Coordinate(-89.56082337111872, 21.003380321364745),
                 new Coordinate(-89.56244388501187, 21.012592648605334),
                 new Coordinate(-89.56459566023314, 21.019196181934248),
                 new Coordinate(-89.56778308450376, 21.023492298408314),
                 new Coordinate(-89.5725861057106, 21.026917774447213),
                 new Coordinate(-89.57431804299272, 21.02992477486592),
                 new Coordinate(-89.58084400775859, 21.031936833397467),
                 new Coordinate(-89.59358840815162, 21.03619682547715),
                 new Coordinate(-89.60767506206471, 21.04119992767818),
                 new Coordinate(-89.62171622775864, 21.048443974920502),
                 new Coordinate(-89.63095801710323, 21.048314697017744),
                 new Coordinate(-89.6482516594657, 21.046245879597876),
                 new Coordinate(-89.65124254997441, 21.043595171377078)
            };

            var limitPolygon = geometryFactory.CreatePolygon(puntosPeriferico);

            return limitPolygon;
        }

        public bool IsOutLimit(MultiRouteRequestDto request, Polygon limitPolygon)
        {
            bool isOutLimit = false;
            var factory = geometryFactory;

            isOutLimit = request.Waypoints.Any(p => !limitPolygon.Contains(factory.CreatePoint(new Coordinate(p.Longitude, p.Latitude))));

            return isOutLimit;
        }
    }
}
