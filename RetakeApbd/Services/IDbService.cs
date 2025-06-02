using RetakeApbd.Models;

namespace RetakeApbd.Services;

public interface IDbService
{
    Task<CustomerDTO> GetCustomerByIdAsync(int id);
    Task AddClientWithRentalAsync(AddClientWithRentalDTO addClientWithRental);
    Task<bool> CarExistsAsync(int id);
}