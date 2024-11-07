using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace C0deGeek.ObjectCompare.Dynamic;

internal sealed class CustomGetMemberBinder : GetMemberBinder
{
    public CustomGetMemberBinder(string name) : base(name, ignoreCase: true)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
    }

    public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject? errorSuggestion)
    {
        ArgumentNullException.ThrowIfNull(target);

        var expression = CreateMemberAccessExpression(target);
        var restrictions = target.Restrictions.Merge(
            BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType));

        return new DynamicMetaObject(expression, restrictions);
    }

    private Expression CreateMemberAccessExpression(DynamicMetaObject target)
    {
        return target.LimitType == typeof(IDictionary<string, object>) 
            ? CreateDictionaryAccess(target) 
            : CreateReflectionAccess(target);
    }

    private BlockExpression CreateDictionaryAccess(DynamicMetaObject target)
    {
        var dictionaryCast = Expression.Convert(target.Expression, typeof(IDictionary<string, object>));
        var nameConstant = Expression.Constant(Name);
        var tryGetValue = typeof(IDictionary<string, object>).GetMethod("TryGetValue")!;
        var valueVar = Expression.Variable(typeof(object));

        return Expression.Block(
            [valueVar],
            Expression.Condition(
                Expression.Call(dictionaryCast, tryGetValue, nameConstant, valueVar),
                valueVar,
                Expression.Constant(null, typeof(object))
            )
        );
    }

    private Expression CreateReflectionAccess(DynamicMetaObject target)
    {
        var typeConstant = Expression.Constant(target.LimitType);
        var nameConstant = Expression.Constant(Name);
        const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        var flagsConstant = Expression.Constant(bindingFlags);

        // Property access
        var propertyAccess = CreatePropertyAccess(target, typeConstant, nameConstant, flagsConstant);
        
        // Field access
        var fieldAccess = CreateFieldAccess(target, typeConstant, nameConstant, flagsConstant);

        // Combine property and field access
        return Expression.Condition(
            Expression.NotEqual(propertyAccess.Info, Expression.Constant(null)),
            propertyAccess.Value,
            fieldAccess.Value
        );
    }

    private static (Expression Info, Expression Value) CreatePropertyAccess(
        DynamicMetaObject target,
        ConstantExpression typeConstant,
        ConstantExpression nameConstant,
        ConstantExpression flagsConstant)
    {
        var getPropertyMethod = typeof(Type).GetMethod("GetProperty", [typeof(string), typeof(BindingFlags)])!;
        var propertyInfo = Expression.Call(typeConstant, getPropertyMethod, nameConstant, flagsConstant);
        
        var getValue = typeof(PropertyInfo).GetMethod("GetValue", [typeof(object)])!;
        var propertyValue = Expression.Condition(
            Expression.NotEqual(propertyInfo, Expression.Constant(null)),
            Expression.Call(
                propertyInfo,
                getValue,
                target.Expression
            ),
            Expression.Constant(null, typeof(object))
        );

        return (propertyInfo, propertyValue);
    }

    private static (Expression Info, Expression Value) CreateFieldAccess(
        DynamicMetaObject target,
        ConstantExpression typeConstant,
        ConstantExpression nameConstant,
        ConstantExpression flagsConstant)
    {
        var getFieldMethod = typeof(Type).GetMethod("GetField", [typeof(string), typeof(BindingFlags)])!;
        var fieldInfo = Expression.Call(typeConstant, getFieldMethod, nameConstant, flagsConstant);
        
        var getValue = typeof(FieldInfo).GetMethod("GetValue", [typeof(object)])!;
        var fieldValue = Expression.Condition(
            Expression.NotEqual(fieldInfo, Expression.Constant(null)),
            Expression.Call(
                fieldInfo,
                getValue,
                target.Expression
            ),
            Expression.Constant(null, typeof(object))
        );

        return (fieldInfo, fieldValue);
    }
}