using Aiursoft.MusicExam.Entities;
using Aiursoft.MusicExam.Models.HomeViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Aiursoft.MusicExam.Controllers;

[LimitPerMin]
public class HomeController : Controller
{
    private readonly TemplateDbContext _dbContext;

    public HomeController(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
    {
        var schools = await _dbContext
            .Schools
            .Include(s => s.Papers)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.Id)
            .ToListAsync();

        var model = new IndexViewModel
        {
            Schools = schools
        };
        return this.StackView(model);
    }
}
