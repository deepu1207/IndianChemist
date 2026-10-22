namespace IndianChemist.Models;

public class Product
{
    public int Id
    { get; set; }
    public int CountryId
    { get; set; }
    public int StateId
    { get; set; }
    public int CityId
    { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Description { get; set; } = "";
    public decimal Price
    { get; set; }
    public int Quantity
    { get; set; }
    public string Sex { get; set; } = "";
}
