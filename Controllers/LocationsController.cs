using IndianChemist.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace IndianChemist.Controllers;

[ApiController, Authorize]
[Route("api")]
public class LocationsController(LocationService locations) : ControllerBase
{
    [HttpGet("countries")]
    public IActionResult Countries() => Ok(locations.Countries());
    [HttpGet("countries/{id:int}/states")]
    public IActionResult States(int id) => Ok(locations.States(id));
    [HttpGet("states/{id:int}/cities")]
    public IActionResult Cities(int id) => Ok(locations.Cities(id));
}
