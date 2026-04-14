namespace FinancialApi.Api.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FinancialApi.Infrastructure.Persistence;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public ProductsController(ApplicationDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _db.Products
            .OrderBy(p => p.Category)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                id = p.Id,
                name = p.Name,
                price = p.Price,
                category = p.Category
            })
            .ToListAsync();

        return Ok(products);
    }
}
