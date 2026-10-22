using IndianChemist.Models;
using IndianChemist.Repositories;
namespace IndianChemist.Services;

public class LocationService(JsonFileRepository repository)
{
    public IEnumerable<Country> Countries() => repository.Countries;
    public IEnumerable<State> States(int countryId) => repository.States.Where(s => s.CountryId == countryId);
    public IEnumerable<City> Cities(int stateId) => repository.Cities.Where(c => c.StateId == stateId);
}
