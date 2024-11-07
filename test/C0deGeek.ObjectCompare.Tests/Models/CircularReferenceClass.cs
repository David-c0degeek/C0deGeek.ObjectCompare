namespace C0deGeek.ObjectCompare.Tests.Models;

public class CircularReferenceClass
{
    public int Id { get; set; }
    public CircularReferenceClass? Reference { get; set; }
}