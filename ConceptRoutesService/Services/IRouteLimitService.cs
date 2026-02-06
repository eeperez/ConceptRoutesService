using ConceptRoutesService.Dtos;
using NetTopologySuite.Geometries;

namespace ConceptRoutesService.Services
{
    public interface IRouteLimitService
    {
        Polygon GetLimitPolygon();
        bool IsOutLimit(MultiRouteRequestDto request, Polygon limitPolygon);
    }
}