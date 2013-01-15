﻿//
// NProxy is a library for the .NET framework to create lightweight dynamic proxies.
// Copyright © Martin Tamme
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program. If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Linq;
using System.Reflection;
using NProxy.Core.Internal.Common;

namespace NProxy.Core.Internal.Reflection
{
    /// <summary>
    /// Provides type extensions.
    /// </summary>
    internal static class TypeExtensions
    {
        /// <summary>
        /// Returns a value indicating weather the specified type is a delegate.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A value indicating weather the specified type is a delegate.</returns>
        public static bool IsDelegate(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return typeof (Delegate).IsAssignableFrom(type);
        }

        /// <summary>
        /// Returns a value indicating weather the specified type is void.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>A value indicating weather the specified type is void.</returns>
        public static bool IsVoid(this Type type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return (type == typeof (void));
        }

        /// <summary>
        /// Returns the constructor information that reflects the constructor that matches the specified criterias.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="bindingFlags">The binding flags.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns>The constructor information.</returns>
        public static ConstructorInfo GetConstructor(this Type type, BindingFlags bindingFlags, params Type[] parameterTypes)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (parameterTypes == null)
                throw new ArgumentNullException("parameterTypes");

            var constructorInfo = type.GetConstructor(bindingFlags, null, parameterTypes, null);

            if (constructorInfo == null)
                throw new MissingMethodException(String.Format("Constructor on type '{0}' not found", type));

            return constructorInfo;
        }

        /// <summary>
        /// Returns the method information that reflects the method that matches the specified criterias.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="bindingFlags">The binding flags.</param>
        /// <param name="parameterTypes">The parameter types.</param>
        /// <returns>The method information.</returns>
        public static MethodInfo GetMethod(this Type type, string methodName, BindingFlags bindingFlags, params Type[] parameterTypes)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (methodName == null)
                throw new ArgumentNullException("methodName");

            if (parameterTypes == null)
                throw new ArgumentNullException("parameterTypes");

            var methodInfo = type.GetMethod(methodName, bindingFlags, null, parameterTypes, null);

            if (methodInfo == null)
                throw new MissingMethodException(String.Format("Method '{0}' on type '{1}' not found", methodName, type));

            return methodInfo;
        }

        /// <summary>
        /// Maps a type to the specified generic types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="genericTypes">The generic types.</param>
        /// <returns>The mapped type.</returns>
        public static Type MapGenericType(this Type type, Type[] genericTypes)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (genericTypes == null)
                throw new ArgumentNullException("genericTypes");

            // Skip types without generic parameters.
            if (!type.IsGenericParameter && !type.ContainsGenericParameters)
                return type;

            // Handle generic parameter.
            if (type.IsGenericParameter)
                return genericTypes[type.GenericParameterPosition];

            // Handle generic array parameters.
            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var newElementType = elementType.MapGenericType(genericTypes);
                var rank = type.GetArrayRank();

                if (rank > 1)
                    return newElementType.MakeArrayType(rank);

                return newElementType.MakeArrayType();
            }

            // Handle generic by reference parameters.
            if (type.IsByRef)
            {
                var elementType = type.GetElementType();
                var newElementType = elementType.MapGenericType(genericTypes);

                return newElementType.MakeByRefType();
            }

            // Handle generic pointer parameters.
            if (type.IsPointer)
            {
                var elementType = type.GetElementType();
                var newElementType = elementType.MapGenericType(genericTypes);

                return newElementType.MakePointerType();
            }

            // Handle generic type parameters.
            var genericTypeDefinition = type.IsGenericTypeDefinition ? type : type.GetGenericTypeDefinition();
            var genericArguments = type.MapGenericArguments(genericTypes);

            return genericTypeDefinition.MakeGenericType(genericArguments);
        }

        /// <summary>
        /// Maps the generic arguments to the specified generic types.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="genericTypes">The generic types.</param>
        /// <returns>The mapped generic arguments.</returns>
        private static Type[] MapGenericArguments(this Type type, Type[] genericTypes)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var genericArguments = type.GetGenericArguments();

            return Array.ConvertAll(genericArguments, t => t.MapGenericType(genericTypes));
        }

        /// <summary>
        /// Visits all interfaces of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="visitor">The visitor.</param>
        public static void VisitInterfaces(this Type type, IVisitor<Type> visitor)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (visitor == null)
                throw new ArgumentNullException("visitor");

            if (type.IsInterface)
                visitor.Visit(type);

            var interfaceTypes = type.GetInterfaces();

            interfaceTypes.Visit(t => t.VisitInterfaces(visitor));
        }

        /// <summary>
        /// Visits all constructors of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="visitor">The visitor.</param>
        public static void VisitConstructors(this Type type, IVisitor<ConstructorInfo> visitor)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            var constructorInfos = type.GetConstructors(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            constructorInfos.Visit(visitor);
        }

        /// <summary>
        /// Visits all events of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="visitor">The visitor.</param>
        public static void VisitEvents(this Type type, IVisitor<EventInfo> visitor)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // Only visit instance events.
            var eventInfos = type.GetEvents(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            eventInfos.Visit(visitor);
        }

        /// <summary>
        /// Visits all properties of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="visitor">The visitor.</param>
        public static void VisitProperties(this Type type, IVisitor<PropertyInfo> visitor)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // Only visit instance properties.
            var propertyInfos = type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            propertyInfos.Visit(visitor);
        }

        /// <summary>
        /// Visits all methods of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="visitor">The visitor.</param>
        public static void VisitMethods(this Type type, IVisitor<MethodInfo> visitor)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            // Only visit non-accessor instance methods.
            var methodInfos = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
                                  .Where(m => !m.IsSpecialName);

            methodInfos.Visit(visitor);
        }
    }
}