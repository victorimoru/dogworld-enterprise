using DogWorld.Api.Models;
using DogWorld.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace DogWorld.Api.Controllers;

[ApiController]
[Route("api/dogs")]
public class DogsController(DogService service) : ControllerBase
{
    [HttpGet("{id:int}")]
    public async Task<ActionResult<DogWorld.Contracts.DogDetails>> GetDogDetails(
        int id, [FromServices] DogDetailsService details, CancellationToken cancellationToken)
    {
        var dog = await details.GetByIdAsync(id, cancellationToken);
        return dog is null ? NotFound() : Ok(dog);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<Dog>>> GetAvailableDogs(
        [FromServices] DogPageService pages, CancellationToken cancellationToken,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null, [FromQuery] string? breed = null)
    {
        if (page.HasValue || pageSize.HasValue || breed is not null)
        {
            var requestedPage = page ?? 1;
            var requestedSize = pageSize ?? 12;

            if (requestedPage < 1 || requestedSize is < 1 or > 100 || (long)(requestedPage - 1) * requestedSize > int.MaxValue)
                return BadRequest(new ProblemDetails { Status = 400, Title = "Invalid pagination", Detail = "Use a positive page and pageSize between 1 and 100 with an offset no greater than 2147483647." });

            var result = await pages.GetPageAsync(requestedPage, requestedSize, breed, cancellationToken);
            Response.Headers["X-Total-Count"] = result.TotalCount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            return Ok(result.Dogs);
        }

        var dogs = await service.GetAvailableDogsAsync(cancellationToken);
        return Ok(dogs);
    }
}
