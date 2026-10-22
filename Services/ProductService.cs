using System.ComponentModel.DataAnnotations;
using IndianChemist.Models;
using IndianChemist.Repositories;
namespace IndianChemist.Services;

public class ProductService(JsonFileRepository db)
{
    public Product[] GetAll()
    {
        lock (db.Sync)
            return db.Data.Products.ToArray();
    }
    public Product? Get(int id)
    {
        lock (db.Sync)
            return db.Data.Products.FirstOrDefault(p => p.Id == id);
    }
    public Product Create(Product product)
    {
        lock (db.Sync)
        {
            product.Id = db.Data.NextId++;
            db.Data.Products.Add(product);
            db.Save();
            return product;
        }
    }
    public bool Update(int id, Product product)
    {
        lock (db.Sync)
        {
            var index = db.Data.Products.FindIndex(p => p.Id == id);
            if (index < 0)
                return false;
            product.Id = id;
            db.Data.Products[index] = product;
            db.Save();
            return true;
        }
    }
    public bool Delete(int id)
    {
        lock (db.Sync)
        {
            if (db.Data.Products.RemoveAll(p => p.Id == id) == 0)
                return false;
            db.Save();
            return true;
        }
    }
    public string? Validate(Product p)
    {
        if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 150)
            return "Name is required (maximum 150 characters).";
        if (string.IsNullOrWhiteSpace(p.Email) || !new EmailAddressAttribute().IsValid(p.Email))
            return "A valid email is required.";
        if (string.IsNullOrWhiteSpace(p.Description) || p.Description.Length > 2000)
            return "Description is required (maximum 2000 characters).";
        if (p.Price <= 0 || p.Price > 999999999)
            return "Price must be greater than zero and no more than 999999999.";
        if (p.Quantity <= 0)
            return "Quantity must be a positive integer.";
        if (!new[] { "Male", "Female", "Other" }.Contains(p.Sex))
            return "Select Male, Female, or Other.";
        if (!db.Countries.Any(c => c.Id == p.CountryId) || !db.States.Any(s => s.Id == p.StateId && s.CountryId == p.CountryId) || !db.Cities.Any(c => c.Id == p.CityId && c.StateId == p.StateId))
            return "Select a valid country, state, and city combination.";
        return null;
    }
}
