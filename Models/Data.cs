namespace IndianChemist.Models;

public class Data
{
    public List<Account> Accounts { get; set; } = [];
    public List<Product> Products { get; set; } = [];
    public int NextId { get; set; } = 1;
}
