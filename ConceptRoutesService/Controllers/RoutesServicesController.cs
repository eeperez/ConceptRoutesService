using Azure;
using Azure.Core;
using Azure.Core.GeoJson;
using Azure.Maps.Routing;
using Azure.Maps.Routing.Models;
using AzureMapsToolkit.Traffic;
using ConceptRoutesService.Dtos;
using ConceptRoutesService.Dtos.ORSDtos;
using ConceptRoutesService.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;

namespace ConceptRoutesService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoutesServicesController : ControllerBase
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly MapsRoutingClient azureClient;
        private readonly RouteLimitService routeLimitService;

        public RoutesServicesController(IHttpClientFactory httpClientFactory, MapsRoutingClient azureClient)
        {
            this.httpClientFactory = httpClientFactory;
            this.azureClient = azureClient;
            routeLimitService = new RouteLimitService();
        }

        [HttpGet("dataroute")]
        public async Task<RouteResponseDto> GetDataRoute([FromQuery]RouteRequestDto routeRequest)
        { 
            RouteResponseDto routeResponseDto = new RouteResponseDto();

            //string suscriptionKey = "QrWwW2blyz6v8C577UTBiysZn-fSgVTc2FxZDyoqymk";

            //AzureKeyCredential azureKeyCredential = new AzureKeyCredential(suscriptionKey);
            //MapsRoutingClient client = new MapsRoutingClient(azureKeyCredential);

            var origen = new GeoPosition(routeRequest.SourceLongitude, routeRequest.SourceLatitude);
            var destino = new GeoPosition(routeRequest.DestinationLongitude, routeRequest.DestinationLatitude);

            // 2. Crear el objeto RouteDirectionQuery pasando la lista de coordenadas
            var query = new RouteDirectionQuery(new[] { origen, destino });
            
            try
            {
                // 3. Configurar las opciones de la ruta
                RouteDirectionOptions options = new RouteDirectionOptions
                {
                    RouteType = RouteType.Fastest,
                    TravelMode = TravelMode.Car,
                    UseTrafficData = true // Considerar tráfico real para el tiempo
                };
                query.RouteDirectionOptions = options;
                //RouteDirectionQuery routeDirectionQuery = new RouteDirectionQuery();
                //routeDirectionQuery.RouteDirectionOptions = options;

                // 4. Realizar la petición
                Response<RouteDirections> response = await azureClient.GetDirectionsAsync(query);

                // 5. Extraer resultados del primer resumen de ruta
                var summary = response.Value.Routes[0].Summary;

                TimeSpan? routeTime = summary.TravelTimeDuration;

                routeResponseDto.RouteDistance = (double)(summary.LengthInMeters??0) / 1000.0;
                routeResponseDto.RouteHours = routeTime?.Hours ?? 0;
                routeResponseDto.RouteMinutes = routeTime?.Minutes ?? 0;
            }
            catch 
            { 

            }

            return routeResponseDto;
        }

        [HttpGet("dataroute-osm")]
        public async Task<ActionResult<RouteResponseDto>> GetDataRouteOSM([FromQuery] RouteRequestDto routeRequest)
        {
            var httpClient = httpClientFactory.CreateClient("ORS");
            string apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6ImFhMTgzNDU0NTMxNjRhZTRiZjg2OWIzMDA3NzEzOWEyIiwiaCI6Im11cm11cjY0In0="; // Idealmente sacada de appsettings.json

            // ORS usa un POST para direcciones detalladas o un GET simple
            // El formato es: /v2/directions/{profile}?api_key={key}&start={lon,lat}&end={lon,lat}
            string profile = "driving-car";
            string url = $"v2/directions/{profile}?api_key={apiKey}" +
                         $"&start={routeRequest.SourceLongitude},{routeRequest.SourceLatitude}" +
                         $"&end={routeRequest.DestinationLongitude},{routeRequest.DestinationLatitude}";

            try
            {
                var response = await httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode, "Error al conectar con OpenRouteService");

                var content = await response.Content.ReadFromJsonAsync<OrsResponseDto>();

                // ORS devuelve la distancia en metros y la duración en segundos
                var segment = content.Features[0].Properties.Segments[0];
                double totalSeconds = segment.Duration;
                TimeSpan time = TimeSpan.FromSeconds(totalSeconds);

                return Ok(new RouteResponseDto
                {
                    RouteDistance = segment.Distance / 1000.0, // Convertir a KM
                    RouteHours = time.Hours + (time.Days * 24), // Incluir días si es un viaje largo
                    RouteMinutes = time.Minutes
                });
            }
            catch (Exception ex)
            {
                return BadRequest($"Error: {ex.Message}");
            }
        }

        [HttpPost("azure/multi-route")]
        public async Task<ActionResult<RouteResponseDto>> GetAzureMultiRoute([FromBody] MultiRouteRequestDto request)
        {
            if (request.Waypoints.Count < 2) return BadRequest("Se requieren al menos 2 puntos.");

            // Convertir nuestros DTOs a GeoPosition (Longitud, Latitud)
            var points = request.Waypoints
                .Select(p => new GeoPosition(p.Longitude, p.Latitude))
                .ToList();

            var query = new RouteDirectionQuery(points)
            {
                RouteDirectionOptions = new RouteDirectionOptions
                {
                    RouteType = RouteType.Fastest,
                    TravelMode = TravelMode.Car,
                    UseTrafficData = true
                }
            };

            var response = await azureClient.GetDirectionsAsync(query);

            // El 'Summary' de la ruta principal ya contiene el TOTAL de todos los tramos (legs)
            var totalSummary = response.Value.Routes[0].Summary;

            return Ok(new RouteResponseDto
            {
                RouteDistance = (totalSummary.LengthInMeters ?? 0) / 1000.0,
                RouteHours = totalSummary.TravelTimeDuration?.Hours ?? 0,
                RouteMinutes = totalSummary.TravelTimeDuration?.Minutes ?? 0
            });
        }

        [HttpPost("osm/multi-route")]
        public async Task<ActionResult<RouteResponseDto>> GetOsmMultiRoute([FromBody] MultiRouteRequestDto request)
        {
            var httpClient = httpClientFactory.CreateClient("ORS");
            string apiKey = "eyJvcmciOiI1YjNjZTM1OTc4NTExMTAwMDFjZjYyNDgiLCJpZCI6ImFhMTgzNDU0NTMxNjRhZTRiZjg2OWIzMDA3NzEzOWEyIiwiaCI6Im11cm11cjY0In0=";

            // ORS espera un JSON con el formato: {"coordinates": [[lon,lat], [lon,lat], ...]}
            var body = new
            {
                coordinates = request.Waypoints.Select(p => new[] { p.Longitude, p.Latitude }).ToArray()
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, "v2/directions/driving-car")
            {
                Content = JsonContent.Create(body)
            };
            //httpRequest.Headers.Add("Authorization", apiKey);
            httpRequest.Headers.TryAddWithoutValidation("Authorization", apiKey);
            //httpRequest.Headers.Add("X-Api-Key", apiKey);
            httpRequest.Headers.Add("Accept", "application/json");

            var response = await httpClient.SendAsync(httpRequest);
            if (!response.IsSuccessStatusCode) 
                return StatusCode((int)response.StatusCode);

            var content = await response.Content.ReadFromJsonAsync<OrsMultiResponseDto>();

            // El primer feature contiene la ruta completa. 
            // Los 'Segments' representan los tramos entre puntos, pero el 'Summary' general tiene el total.
            var totalDistance = content?.Routes.FirstOrDefault()?.Summary.Distance ?? 0;
            var totalDurationSeconds = content?.Routes.FirstOrDefault()?.Summary.Duration ?? 0;

            TimeSpan time = TimeSpan.FromSeconds(totalDurationSeconds);

            //Obtiene el poligono de merida y calcula si esta fuera del periferico
            var meridaPolygon = routeLimitService.GetLimitPolygon();
            bool isRouteOut = routeLimitService.IsOutLimit(request, meridaPolygon);

            return Ok(new RouteResponseDto
            {
                RouteDistance = (totalDistance / 1000.0),
                RouteHours = (int)time.TotalHours,
                RouteMinutes = time.Minutes,
                IsOutLimit = isRouteOut
            });
        }

        [HttpGet("getcoordinates")]
        public async Task<ActionResult<CoordinateDto>> GetCoordinatesFromGoogleUrl([FromQuery] string shortUrl)
        {
            string longUrl = string.Empty;
            CoordinateDto coordinateResponse = new CoordinateDto();

            using var client = httpClientFactory.CreateClient("URLExpander");

            // 1. Enviamos una petición para expandir la URL corta
            var response = await client.GetAsync(shortUrl);
            if (response != null && response.IsSuccessStatusCode)
            {
                longUrl = response.RequestMessage?.RequestUri?.ToString()?? string.Empty;
            }

            // 2. Buscamos el patrón @latitud,longitud en la URL final
            // Ejemplo: .../place/Yucatán/@20.9482618,-89.6432415,15z/...
            //@"(?<lat>-?\d+\.\d+),(?<lng>-?\d+\.\d+)"
            //var match = Regex.Match(longUrl, @"@(-?\d+\.\d+),(-?\d+\.\d+)");
            var match = Regex.Match(longUrl, @"(?<lat>-?\d+\.\d+),(?<lng>-?\d+\.\d+)");

            if (match.Success)
            {
                coordinateResponse.Latitude = double.Parse(match.Groups[1].Value);
                coordinateResponse.Longitude = double.Parse(match.Groups[2].Value);              
            }

            return Ok(coordinateResponse);
        }
    }
}
