using Aiursoft.MusicExam.Models;

namespace Aiursoft.MusicExam.Configuration;

public class SettingsMap
{
    public const string AllowUserAdjustNickname = "Allow_User_Adjust_Nickname";
    public const string ProjectName = "Project_Name";

    public class FakeLocalizer
    {
        public string this[string name] => name;
    }

    private static readonly FakeLocalizer Localizer = new();

    public static readonly List<GlobalSettingDefinition> Definitions = new()
    {
        new GlobalSettingDefinition
        {
            Key = AllowUserAdjustNickname,
            Name = Localizer["Allow User Adjust Nickname"],
            Description = Localizer["Allow users to adjust their nickname in the profile management page."],
            Type = SettingType.Bool,
            DefaultValue = "True"
        },
        new GlobalSettingDefinition
        {
            Key = ProjectName,
            Name = Localizer["Project Name"],
            Description = Localizer["The name of the project displayed throughout the application."],
            Type = SettingType.Text,
            DefaultValue = "Music Exam"
        }
    };
}
