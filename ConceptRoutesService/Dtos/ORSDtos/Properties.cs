namespace ConceptRoutesService.Dtos.ORSDtos
{
    public class Properties
    {
        public Properties()
        {
            Segments = new List<Segment>();
        }

        public List<Segment> Segments { get; set; }
    }
}
