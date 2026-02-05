namespace ConceptRoutesService.Dtos
{
    public class RouteResponseDto
    {
        public double RouteDistance { get; set; }

        public int RouteHours { get; set; }

        public int RouteMinutes { get; set; }

        public bool IsOutLimit { get; set; }
    }
}
