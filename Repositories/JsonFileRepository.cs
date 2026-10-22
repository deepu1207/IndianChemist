using System.Security.Cryptography;
using System.Text.Json;
using IndianChemist.Models;
namespace IndianChemist.Repositories;

public class JsonFileRepository
{
    public object Sync { get; } = new(); public Data Data
    {
        get;
    }
    public byte[] Key
    {
        get;
    }
    readonly string path; public Country[] Countries = [new(1, "India"), new(2, "United States")]; public State[] States = [new(1, 1, "Maharashtra"), new(2, 1, "Gujarat"), new(3, 1, "Karnataka"), new(4, 2, "California"), new(5, 2, "New York")]; public City[] Cities = [new(1, 1, "Mumbai"), new(2, 1, "Pune"), new(3, 2, "Ahmedabad"), new(4, 2, "Surat"), new(5, 3, "Bengaluru"), new(6, 3, "Mysuru"), new(7, 4, "Los Angeles"), new(8, 4, "San Francisco"), new(9, 5, "New York City"), new(10, 5, "Buffalo")]; public JsonFileRepository(string dir)
    {
        Directory.CreateDirectory(dir);
        path = Path.Combine(dir, "data.json");
        Data = File.Exists(path) ? JsonSerializer.Deserialize<Data>(File.ReadAllText(path))! : new();
        var kp = Path.Combine(dir, "jwt.key");
        if (!File.Exists(kp))
            File.WriteAllBytes(kp, RandomNumberGenerator.GetBytes(64));
        Key = File.ReadAllBytes(kp);
    }
    public void Save()
    {
        File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(Data));
        File.Move(path + ".tmp", path, true);
    }
}
