using Aiursoft.MusicExam.Authorization;
using Aiursoft.MusicExam.Configuration;
using Aiursoft.MusicExam.Models;
using Aiursoft.MusicExam.Models.GlobalSettingsViewModels;
using Aiursoft.MusicExam.Services;
using Aiursoft.MusicExam.Services.FileStorage;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aiursoft.MusicExam.Controllers;

[Authorize(Policy = AppPermissionNames.CanManageGlobalSettings)]
[LimitPerMin]
public class GlobalSettingsController(
    GlobalSettingsService settingsService,
    StorageService storageService) : Controller
{
    [RenderInNavBar(
        NavGroupName = "Administration",
        NavGroupOrder = 9999,
        CascadedLinksGroupName = "System",
        CascadedLinksIcon = "settings",
        CascadedLinksOrder = 9999,
        LinkText = "Global Settings",
        LinkOrder = 1)]
    public async Task<IActionResult> Index()
    {
        var model = new IndexViewModel();
        foreach (var definition in SettingsMap.Definitions)
        {
            model.Settings.Add(new SettingViewModel
            {
                Key = definition.Key,
                Name = definition.Name,
                Description = definition.Description,
                Type = definition.Type,
                DefaultValue = definition.DefaultValue,
                ChoiceOptions = definition.ChoiceOptions,
                Value = await settingsService.GetSettingValueAsync(definition.Key),
                IsOverriddenByConfig = settingsService.IsOverriddenByConfig(definition.Key)
            });
        }
        return this.StackView(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return RedirectToAction(nameof(Index));
        }

        try
        {
            var definition = SettingsMap.Definitions.First(d => d.Key == model.Key);
            if (definition.Type == SettingType.File)
            {
                if (model.FileValue is { Length: > 0 })
                {
                    var savedFilePath = await storageService.Save(Path.Combine("logos", model.FileValue.FileName), model.FileValue);
                    await settingsService.UpdateSettingAsync(model.Key, savedFilePath);
                }
                else if (model.Value != null)
                {
                    await settingsService.UpdateSettingAsync(model.Key, model.Value);
                }
            }
            else
            {
                await settingsService.UpdateSettingAsync(model.Key, model.Value ?? string.Empty);
            }
        }
        catch (InvalidOperationException e)
        {
            ModelState.AddModelError(string.Empty, e.Message);
            return RedirectToAction(nameof(Index));
        }

        return RedirectToAction(nameof(Index));
    }
}
