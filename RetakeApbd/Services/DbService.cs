using Microsoft.Data.SqlClient;
using RetakeApbd.Exceptions;
using RetakeApbd.Models;

namespace RetakeApbd.Services
{
    public class DbService : IDbService
    {
        private readonly string _connectionString;

        public DbService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        }

        public async Task<CustomerDTO?> GetCustomerByIdAsync(int id)
        {
            var query = @"
                SELECT 
                    client.ID, client.FirstName, client.LastName, client.Address, 
                    car.VIN, color.Name AS Color, model.Name AS Model,
                    cr.DateFrom, cr.DateTo, cr.TotalPrice
                FROM clients AS client
                LEFT JOIN car_rentals AS cr ON cr.ClientID = client.ID
                LEFT JOIN cars AS car ON cr.CarID = car.ID
                LEFT JOIN colors AS color ON car.ColorID = color.ID
                LEFT JOIN models AS model ON car.ModelID = model.ID
                WHERE client.ID = @id";

            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@id", id);
            await connection.OpenAsync();

            await using var reader = await command.ExecuteReaderAsync();

            CustomerDTO? customer = null;
            var rentals = new List<RentalDTO>();

            while (await reader.ReadAsync())
            {
                if (customer is null)
                {
                    customer = new CustomerDTO
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("ID")),
                        FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                        LastName = reader.GetString(reader.GetOrdinal("LastName")),
                        Address = reader.GetString(reader.GetOrdinal("Address")),
                        Rentals = rentals
                    };
                }

                if (!reader.IsDBNull(reader.GetOrdinal("VIN")))
                {
                    rentals.Add(new RentalDTO
                    {
                        Vin = reader.GetString(reader.GetOrdinal("VIN")),
                        Color = reader.GetString(reader.GetOrdinal("Color")),
                        Model = reader.GetString(reader.GetOrdinal("Model")),
                        DateFrom = reader.GetDateTime(reader.GetOrdinal("DateFrom")),
                        DateTo = reader.GetDateTime(reader.GetOrdinal("DateTo")),
                        TotalPrice = reader.GetInt32(reader.GetOrdinal("TotalPrice"))
                    });
                }
            }

            return customer;
        }

        public async Task<bool> CarExistsAsync(int carId)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using var command = new SqlCommand("SELECT 1 FROM cars WHERE ID = @carId", connection);
            command.Parameters.AddWithValue("@carId", carId);
            await connection.OpenAsync();
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }

        public async Task AddClientWithRentalAsync(AddClientWithRentalDTO dto)
        {
            await using var connection = new SqlConnection(_connectionString);
            await using SqlCommand command = new SqlCommand();
            
            command.Connection = connection;
            await connection.OpenAsync();
            await using var transaction = await connection.BeginTransactionAsync();
            command.Transaction = transaction as SqlTransaction;

            try
            {
                var carCmd = new SqlCommand("SELECT PricePerDay FROM cars WHERE ID = @carId", connection, (SqlTransaction)transaction);
                carCmd.Parameters.AddWithValue("@carId", dto.CarID);
                var priceObj = await carCmd.ExecuteScalarAsync();
                if (priceObj == null)
                {
                    throw new NotFoundException();
                }
                var pricePerDay = (int)priceObj;
                
                var clientCmd = new SqlCommand(
                    "INSERT INTO clients (FirstName, LastName, Address) OUTPUT INSERTED.ID VALUES (@fn, @ln, @addr)",
                    connection, (SqlTransaction)transaction);
                clientCmd.Parameters.AddWithValue("@fn", dto.Client.FirstName);
                clientCmd.Parameters.AddWithValue("@ln", dto.Client.LastName);
                clientCmd.Parameters.AddWithValue("@addr", dto.Client.Address);
                var clientId = (int)(await clientCmd.ExecuteScalarAsync());

                var days = (dto.DateTo - dto.DateFrom).Days + 1;
                var totalPrice = days * pricePerDay;
                
                var rentalCmd = new SqlCommand(
                    @"INSERT INTO car_rentals (ClientID, CarID, DateFrom, DateTo, TotalPrice) 
                      VALUES (@clientId, @carId, @from, @to, @total)", connection, (SqlTransaction)transaction);
                rentalCmd.Parameters.AddWithValue("@clientId", clientId);
                rentalCmd.Parameters.AddWithValue("@carId", dto.CarID);
                rentalCmd.Parameters.AddWithValue("@from", dto.DateFrom);
                rentalCmd.Parameters.AddWithValue("@to", dto.DateTo);
                rentalCmd.Parameters.AddWithValue("@total", totalPrice);

                await rentalCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
    }
}
