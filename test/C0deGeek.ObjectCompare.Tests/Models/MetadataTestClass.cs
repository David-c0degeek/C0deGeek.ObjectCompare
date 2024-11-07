namespace C0deGeek.ObjectCompare.Tests.Models;

public class MetadataTestClass(string privateValue = "")
{
    private readonly string _privateField = privateValue;

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string IgnoredProperty { get; set; } = "";
}