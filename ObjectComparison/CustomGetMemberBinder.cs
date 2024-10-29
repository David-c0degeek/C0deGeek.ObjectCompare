using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;

namespace ObjectComparison;

internal class CustomGetMemberBinder(string name) : GetMemberBinder(name, true)
{
    public override DynamicMetaObject FallbackGetMember(DynamicMetaObject target, DynamicMetaObject errorSuggestion)
    {
        // Create the expression that will get the member value
        Expression getExpression;

        if (target.LimitType == typeof(IDictionary<string, object>))
        {
            // For ExpandoObject (implements IDictionary<string, object>)
            var dictionaryCast = Expression.Convert(target.Expression, typeof(IDictionary<string, object>));
            var nameConstant = Expression.Constant(Name);
            var tryGetValue = typeof(IDictionary<string, object>).GetMethod("TryGetValue");
            var valueVar = Expression.Variable(typeof(object));

            getExpression = Expression.Block(
                [valueVar],
                Expression.Condition(
                    Expression.Call(dictionaryCast, tryGetValue, nameConstant, valueVar),
                    valueVar,
                    Expression.Constant(null)
                )
            );
        }
        else
        {
            // For other dynamic objects, try to get property or field value using reflection
            var typeConstant = Expression.Constant(target.LimitType);
            var nameConstant = Expression.Constant(Name);
            var bindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            var flagsConstant = Expression.Constant(bindingFlags);

            // Try property first
            var getPropertyMethod =
                typeof(Type).GetMethod("GetProperty", [typeof(string), typeof(BindingFlags)]);
            var propertyInfo = Expression.Call(typeConstant, getPropertyMethod, nameConstant, flagsConstant);
            var propertyValue = Expression.Condition(
                Expression.NotEqual(propertyInfo, Expression.Constant(null)),
                Expression.Call(
                    propertyInfo,
                    typeof(PropertyInfo).GetMethod("GetValue", [typeof(object)]),
                    target.Expression
                ),
                Expression.Constant(null)
            );

            // Try field if property not found
            var getFieldMethod = typeof(Type).GetMethod("GetField", [typeof(string), typeof(BindingFlags)]);
            var fieldInfo = Expression.Call(typeConstant, getFieldMethod, nameConstant, flagsConstant);
            var fieldValue = Expression.Condition(
                Expression.NotEqual(fieldInfo, Expression.Constant(null)),
                Expression.Call(
                    fieldInfo,
                    typeof(FieldInfo).GetMethod("GetValue", [typeof(object)]),
                    target.Expression
                ),
                Expression.Constant(null)
            );

            // Combine property and field checks
            getExpression = Expression.Condition(
                Expression.NotEqual(propertyInfo, Expression.Constant(null)),
                propertyValue,
                fieldValue
            );
        }

        // Return a new DynamicMetaObject with the member access expression
        return new DynamicMetaObject(
            getExpression,
            target.Restrictions.Merge(BindingRestrictions.GetTypeRestriction(target.Expression, target.LimitType))
        );
    }
}