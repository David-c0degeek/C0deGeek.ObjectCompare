using C0deGeek.ObjectCompare.Tests.Models;

namespace C0deGeek.ObjectCompare.Tests.Extensions;

public static class TestObjectHelper
{
    public static NestedObject CreateNestedObject(int depth)
    {
        var root = new NestedObject { Value = 1 };
        var current = root;
        for (var i = 0; i < depth; i++)
        {
            current.Next = new NestedObject { Value = i + 2 };
            current = current.Next;
        }
        return root;
    }
    
    public static ComplexObject CreateLargeObject()
    {
        var obj = new ComplexObject();
        for (var i = 0; i < 1000; i++)
        {
            obj.Items.Add(new SimpleObject 
            { 
                Id = i, 
                Name = $"Item {i}",
                Data = new byte[1000]
            });
        }
        return obj;
    }
    
    public static ComplexObject CreateComplexObject(int itemCount)
    {
        var obj = new ComplexObject();
        
        for (var i = 0; i < itemCount; i++)
        {
            obj.Items.Add(new SimpleObject
            {
                Id = i,
                Name = $"Item {i}",
                Data = new byte[1000]
            });
        }

        return obj;
    }
}