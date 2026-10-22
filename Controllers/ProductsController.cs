using IndianChemist.Models;
using IndianChemist.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace IndianChemist.Controllers;

[ApiController, Authorize]
[Route("api/products")]
public class ProductsController(ProductService products) : ControllerBase
{
    [HttpGet]
    public IActionResult Index() => Ok(products.GetAll());
    [HttpGet("{id:int}")]
    public IActionResult Details(int id) => products.Get(id) is { } product ? Ok(product) : NotFound();
    [HttpPost, Authorize(Roles = "Admin")]
    public IActionResult Create([FromBody] Product product)
    {
        if (products.Validate(product) is { } error)
            return BadRequest(new
            {
                message = error
            });
        var created = products.Create(product);
        return CreatedAtAction(nameof(Details), new
        {
            id = created.Id
        }, created);
    }
    [HttpPut("{id:int}"), Authorize(Roles = "Admin")]
    public IActionResult Edit(int id, [FromBody] Product product)
    {
        if (products.Validate(product) is { } error)
            return BadRequest(new
            {
                message = error
            });
        return products.Update(id, product) ? Ok(product) : NotFound();
    }
    [HttpDelete("{id:int}"), Authorize(Roles = "Admin")]
    public IActionResult Delete(int id) => products.Delete(id) ? NoContent() : NotFound();
}
