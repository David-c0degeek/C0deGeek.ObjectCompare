namespace C0deGeek.ObjectCompare.Tests.Models;

public class CircularObject
{
    public int Id { get; set; }
    public CircularObject? Reference { get; set; }
}