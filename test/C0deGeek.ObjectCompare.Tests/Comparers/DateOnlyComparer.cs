using C0deGeek.ObjectCompare.Comparison.Base;
using C0deGeek.ObjectCompare.Interfaces;

namespace C0deGeek.ObjectCompare.Tests.Comparers;

public class DateOnlyComparer : ICustomComparer
{
    public bool AreEqual(object obj1, object obj2, ComparisonConfig config)
    {
        if (obj1 is DateTime date1 && obj2 is DateTime date2)
        {
            return date1.Date == date2.Date;
        }
        return false;
    }
}