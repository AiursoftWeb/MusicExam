using Aiursoft.MusicExam.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Aiursoft.MusicExam.Tests.Services;

[TestClass]
public class NaturalSortComparerTests
{
    private readonly NaturalSortComparer _comparer = new();

    [TestMethod]
    public void TestBasicStrings()
    {
        var list = new List<string> { "b", "a", "c" };
        list.Sort(_comparer);
        Assert.AreEqual("a", list[0]);
        Assert.AreEqual("b", list[1]);
        Assert.AreEqual("c", list[2]);
    }

    [TestMethod]
    public void TestNumericStrings()
    {
        var list = new List<string> { "10", "2", "1" };
        list.Sort(_comparer);
        Assert.AreEqual("1", list[0]);
        Assert.AreEqual("2", list[1]);
        Assert.AreEqual("10", list[2]);
    }

    [TestMethod]
    public void TestMixedStrings()
    {
        var list = new List<string> { "file10.txt", "file2.txt", "file1.txt" };
        list.Sort(_comparer);
        Assert.AreEqual("file1.txt", list[0]);
        Assert.AreEqual("file2.txt", list[1]);
        Assert.AreEqual("file10.txt", list[2]);
    }

    [TestMethod]
    public void TestComplexStrings()
    {
        var list = new List<string> { "模拟卷9", "模拟卷10", "模拟卷1", "模拟卷2", "模拟卷3", "模拟卷4", "模拟卷5", "模拟卷6", "模拟卷7", "模拟卷8", };
        list.Sort(_comparer);
        Assert.AreEqual("模拟卷1", list[0]);
        Assert.AreEqual("模拟卷2", list[1]);
        Assert.AreEqual("模拟卷3", list[2]);
        Assert.AreEqual("模拟卷4", list[3]);
        Assert.AreEqual("模拟卷5", list[4]);
        Assert.AreEqual("模拟卷6", list[5]);
        Assert.AreEqual("模拟卷7", list[6]);
        Assert.AreEqual("模拟卷8", list[7]);
        Assert.AreEqual("模拟卷9", list[8]);
        Assert.AreEqual("模拟卷10", list[9]);
    }

    [TestMethod]
    public void TestStructureWithLevels()
    {
        var list = new List<string> { "Level 10", "Level 9", "Level 8", "Level 7", "Level 6", "Level 5", "Level 4", "Level 3", "Level 2", "Level 1", "Level 11", "Level 12" };
        list.Sort(_comparer);
        Assert.AreEqual("Level 1", list[0]);
        Assert.AreEqual("Level 2", list[1]);
        Assert.AreEqual("Level 3", list[2]);
        Assert.AreEqual("Level 4", list[3]);
        Assert.AreEqual("Level 5", list[4]);
        Assert.AreEqual("Level 6", list[5]);
        Assert.AreEqual("Level 7", list[6]);
        Assert.AreEqual("Level 8", list[7]);
        Assert.AreEqual("Level 9", list[8]);
        Assert.AreEqual("Level 10", list[9]);
        Assert.AreEqual("Level 11", list[10]);
        Assert.AreEqual("Level 12", list[11]);
    }
}
