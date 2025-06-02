namespace RetakeApbd.Models;

public class AddClientWithRentalDTO
{
    public NewClientDTO Client { get; set; }
    public int CarID { get; set; }
    public DateTime DateFrom { get; set; }
    public DateTime DateTo { get; set; }
}

public class NewClientDTO
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Address { get; set; }
}