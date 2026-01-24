using Aiursoft.MusicExam.Configuration;
using Aiursoft.MusicExam.Services;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.MusicExam.Views.Shared.Components.MarketingFooter;

public class MarketingFooter(GlobalSettingsService globalSettingsService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(MarketingFooterViewModel? model = null)
    {
        model ??= new MarketingFooterViewModel();
        model.ProjectName = await globalSettingsService.GetSettingValueAsync(SettingsMap.ProjectName);
        model.BrandName = await globalSettingsService.GetSettingValueAsync(SettingsMap.BrandName);
        model.BrandHomeUrl = await globalSettingsService.GetSettingValueAsync(SettingsMap.BrandHomeUrl);
        model.Icp = await globalSettingsService.GetSettingValueAsync(SettingsMap.Icp);
        return View(model);
    }
}
