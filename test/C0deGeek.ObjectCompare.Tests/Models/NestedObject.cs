namespace C0deGeek.ObjectCompare.Tests.Models;

public class NestedObject
{
    public int Value { get; set; }
    public NestedObject? Next { get; set; }
    public byte[] Data { get; set; } = [];
}

