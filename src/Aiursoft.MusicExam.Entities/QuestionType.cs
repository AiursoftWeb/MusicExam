namespace Aiursoft.MusicExam.Entities;

/// <summary>
/// 定义了题目的类型。
/// </summary>
public enum QuestionType
{
    /// <summary>
    /// 选择题，可能有单选或多选。
    /// </summary>
    MultipleChoice = 0,

    /// <summary>
    /// 视唱题，仅展示题目内容，无标准答案。
    /// </summary>
    SightSinging = 1,

    /// <summary>
    /// 单选题，必须且只能选择一个正确选项。
    /// </summary>
    SingleChoice = 2
}
