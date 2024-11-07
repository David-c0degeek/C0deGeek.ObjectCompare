namespace C0deGeek.ObjectCompare.Tests.Models;

public class MetadataTestClass
{
    private readonly string _privateField;

    public MetadataTestClass(string privateValue = "")
    {
        _privateField = privateValue;
    }

    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string IgnoredProperty { get; set; } = "";
}