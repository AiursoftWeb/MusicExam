using Aiursoft.MusicExam.Entities;
using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.HomeViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public required IEnumerable<School> Schools { get; init; }
}