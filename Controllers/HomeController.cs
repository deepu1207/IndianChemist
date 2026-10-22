using IndianChemist.Models;
using Microsoft.AspNetCore.Mvc;
namespace IndianChemist.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View(new WorkspaceViewModel());
}
