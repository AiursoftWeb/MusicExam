using Aiursoft.MusicExam.Configuration;
using Aiursoft.MusicExam.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.MusicExam.Views.Shared.Components.MarketingNavbar;

public class MarketingNavbar(GlobalSettingsService globalSettingsService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingNavbarViewModel? model = null)
    {
        model ??= new MarketingNavbarViewModel();
        model.ProjectName = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectName);
        return View(model);
    }
}
