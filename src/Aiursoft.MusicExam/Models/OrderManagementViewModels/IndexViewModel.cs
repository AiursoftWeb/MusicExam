using Aiursoft.UiStack.Layout;

namespace Aiursoft.MusicExam.Models.OrderManagementViewModels;

public class IndexViewModel : UiStackLayoutViewModel
{
    public List<SchoolNode> Schools { get; set; } = new();

    public IndexViewModel()
    {
        PageTitle = "Manage Display Order";
    }
}

public class SchoolNode
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int DisplayOrder { get; set; }
    public List<LevelNode> Levels { get; set; } = new();
}

public class LevelNode
{
    public required string Name { get; set; }
    public List<CategoryNode> Categories { get; set; } = new();
}

public class CategoryNode
{
    public required string Name { get; set; }
    public List<PaperNode> Papers { get; set; } = new();
}

public class PaperNode
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public int DisplayOrder { get; set; }
}
