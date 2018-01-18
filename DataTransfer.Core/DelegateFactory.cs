﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace DataTransfer.Core
{
    internal delegate object LateBoundMethod(object target, object[] arguments);
    internal delegate object LateBoundPropertyGet(object target); 
    internal delegate object LateBoundFieldGet(object target);
    internal delegate void LateBoundFieldSet(object target, object value);
    internal delegate void LateBoundPropertySet(object target, object value);
    internal delegate void LateBoundValueTypeFieldSet(ref object target, object value);
    internal delegate void LateBoundValueTypePropertySet(ref object target, object value);
    internal delegate object LateBoundCtor();
    internal delegate object LateBoundParamsCtor(params object[] parameters);
    /// <summary>
    /// 委托工厂
    /// </summary>
    internal static class DelegateFactory
    {
        private static readonly ConcurrentDictionary<Type, LateBoundCtor> _ctorCache = new ConcurrentDictionary<Type, LateBoundCtor>();
        internal static LateBoundMethod CreateGet(MethodInfo method)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");
            ParameterExpression argumentsParameter = Expression.Parameter(typeof(object[]), "arguments");

            MethodCallExpression call;
            if (!method.IsDefined(typeof(ExtensionAttribute), false))
            {
                // instance member method
                call = Expression.Call(
                    Expression.Convert(instanceParameter, method.DeclaringType),
                    method,
                    CreateParameterExpressions(method, instanceParameter, argumentsParameter));
            }
            else
            {
                // static extension method
                call = Expression.Call(
                    method,
                    CreateParameterExpressions(method, instanceParameter, argumentsParameter));
            }

            Expression<LateBoundMethod> lambda = Expression.Lambda<LateBoundMethod>(
                Expression.Convert(call, typeof(object)),
                instanceParameter,
                argumentsParameter);

            return lambda.Compile();
        }
        internal static LateBoundPropertyGet CreateGet(PropertyInfo property)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");

            MemberExpression member = Expression.Property(Expression.Convert(instanceParameter, property.DeclaringType), property);

            Expression<LateBoundPropertyGet> lambda = Expression.Lambda<LateBoundPropertyGet>(
                Expression.Convert(member, typeof(object)),
                instanceParameter
                );

            return lambda.Compile();
        }
        internal static LateBoundFieldGet CreateGet(FieldInfo field)
        {
            ParameterExpression instanceParameter = Expression.Parameter(typeof(object), "target");

            MemberExpression member = Expression.Field(Expression.Convert(instanceParameter, field.DeclaringType), field);

            Expression<LateBoundFieldGet> lambda = Expression.Lambda<LateBoundFieldGet>(
                Expression.Convert(member, typeof(object)),
                instanceParameter
                );

            return lambda.Compile();
        }
        internal static LateBoundFieldSet CreateSet(FieldInfo field)
        {
            var sourceType = field.DeclaringType;

            var method = CreateDynamicMethod(field, sourceType);

            var gen = method.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0); // Load input to stack
            gen.Emit(OpCodes.Castclass, sourceType); // Cast to source type
            gen.Emit(OpCodes.Ldarg_1); // Load value to stack
            gen.Emit(OpCodes.Unbox_Any, field.FieldType); // Unbox the value to its proper value type
            gen.Emit(OpCodes.Stfld, field); // Set the value to the input field
            gen.Emit(OpCodes.Ret);

            var callback = (LateBoundFieldSet)method.CreateDelegate(typeof(LateBoundFieldSet));

            return callback;
        }
        internal static LateBoundPropertySet CreateSet(PropertyInfo property)
        {
            var sourceType = property.DeclaringType;
            var setter = property.GetSetMethod(true);
            var method = CreateDynamicMethod(property, sourceType);
            var gen = method.GetILGenerator();

            gen.Emit(OpCodes.Ldarg_0); // Load input to stack
            gen.Emit(OpCodes.Castclass, sourceType); // Cast to source type
            gen.Emit(OpCodes.Ldarg_1); // Load value to stack
            gen.Emit(OpCodes.Unbox_Any, property.PropertyType); // Unbox the value to its proper value type
            gen.Emit(OpCodes.Callvirt, setter); // Call the setter method
            gen.Emit(OpCodes.Ret);

            var result = (LateBoundPropertySet)method.CreateDelegate(typeof(LateBoundPropertySet));

            return result;
        }
        internal static LateBoundValueTypePropertySet CreateValueTypeSet(PropertyInfo property)
        {
            var sourceType = property.DeclaringType;
            var setter = property.GetSetMethod(true);
            var method = CreateValueTypeDynamicMethod(property, sourceType);
            var gen = method.GetILGenerator();

            method.InitLocals = true;
            gen.Emit(OpCodes.Ldarg_0); // Load input to stack
            gen.Emit(OpCodes.Ldind_Ref);
            gen.Emit(OpCodes.Unbox_Any, sourceType); // Unbox the source to its correct type
            gen.Emit(OpCodes.Stloc_0); // Store the unboxed input on the stack
            gen.Emit(OpCodes.Ldloca_S, 0);
            gen.Emit(OpCodes.Ldarg_1); // Load value to stack
            gen.Emit(OpCodes.Castclass, property.PropertyType); // Unbox the value to its proper value type
            gen.Emit(OpCodes.Call, setter); // Call the setter method
            gen.Emit(OpCodes.Ret);

            var result = (LateBoundValueTypePropertySet)method.CreateDelegate(typeof(LateBoundValueTypePropertySet));

            return result;
        }
        internal static LateBoundCtor CreateCtor(Type type)
        {
            LateBoundCtor ctor = _ctorCache.GetOrAdd(type, t =>
            {
                var ctorExpression = Expression.Lambda<LateBoundCtor>(Expression.Convert(Expression.New(type), typeof(object)));

                return ctorExpression.Compile();
            });

            return ctor;
        }
        private static DynamicMethod CreateValueTypeDynamicMethod(MemberInfo member, Type sourceType)
        {
            if (sourceType.IsInterface)
                return new DynamicMethod("Set" + member.Name, null, new[] { typeof(object).MakeByRefType(), typeof(object) }, sourceType.Assembly.ManifestModule, true);
            return new DynamicMethod("Set" + member.Name, null, new[] { typeof(object).MakeByRefType(), typeof(object) }, sourceType, true);
        }
        private static DynamicMethod CreateDynamicMethod(MemberInfo member, Type sourceType)
        {
            if (sourceType.IsInterface)
                return new DynamicMethod("Set" + member.Name, null, new[] { typeof(object), typeof(object) }, sourceType.Assembly.ManifestModule, true);

            return new DynamicMethod("Set" + member.Name, null, new[] { typeof(object), typeof(object) }, sourceType, true);
        }
        private static Expression[] CreateParameterExpressions(MethodInfo method, Expression instanceParameter, Expression argumentsParameter)
        {
            var expressions = new List<UnaryExpression>();
            var realMethodParameters = method.GetParameters();
            if (method.IsDefined(typeof(ExtensionAttribute), false))
            {
                Type extendedType = method.GetParameters()[0].ParameterType;
                expressions.Add(Expression.Convert(instanceParameter, extendedType));
                realMethodParameters = realMethodParameters.Skip(1).ToArray();
            }

            expressions.AddRange(realMethodParameters.Select((parameter, index) =>
                Expression.Convert(
                    Expression.ArrayIndex(argumentsParameter, Expression.Constant(index)),
                    parameter.ParameterType)));

            return expressions.ToArray();
        }
    }
}