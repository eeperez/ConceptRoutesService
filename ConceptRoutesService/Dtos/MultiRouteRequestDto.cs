namespace ConceptRoutesService.Dtos
{
    public class MultiRouteRequestDto
    {
        // Una lista de puntos donde cada uno tiene Lat y Lon
        public List<CoordinateDto> Waypoints { get; set; }
    }
}
