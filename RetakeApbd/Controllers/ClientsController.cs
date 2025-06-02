using Microsoft.AspNetCore.Mvc;
using RetakeApbd.Models;
using RetakeApbd.Services;
using RetakeApbd.Exceptions;

namespace RetakeApbd.Controllers
{
    [Route("api/clients")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IDbService _dbService;
        public ClientsController(IDbService dbService)
        {
            _dbService = dbService;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCustomer(int id)
        {
            var customer = await _dbService.GetCustomerByIdAsync(id);
            if (customer == null)
                return NotFound("Client not found.");
            return Ok(customer);
        }

        [HttpPost]
        public async Task<IActionResult> AddClientWithRental([FromBody] AddClientWithRentalDTO dto)
        {
            try
            {
                await _dbService.AddClientWithRentalAsync(dto);
                return Created("", "Client and rental added.");
            }
            catch (NotFoundException)
            {
                return NotFound("Car not found.");
            }
            catch
            {
                return StatusCode(500, "Server error");
            }
        }
    }
}