
using Aiursoft.MusicExam.Models.HomeViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;


namespace Aiursoft.MusicExam.Controllers;

[LimitPerMin]
public class HomeController : Controller
{
    public IActionResult Index()
    {
        return this.SimpleView(new IndexViewModel());
    }
}
