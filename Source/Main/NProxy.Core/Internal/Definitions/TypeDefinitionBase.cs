//
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NProxy.Core.Internal.Common;
using NProxy.Core.Internal.Reflection;

namespace NProxy.Core.Internal.Definitions
{
    /// <summary>
    /// Represents the type definition base class.
    /// </summary>
    internal abstract class TypeDefinitionBase : ITypeDefinition, ITypeActivator
    {
        /// <summary>
        /// The declaring type.
        /// </summary>
        private readonly Type _declaringType;

        /// <summary>
        /// The custom attribute informations.
        /// </summary>
        private readonly List<AttributeInfo> _customAttributeInfos;

        /// <summary>
        /// The declaring interface types.
        /// </summary>
        private readonly Lazy<ISet<Type>> _declaringInterfaceTypes;

        /// <summary>
        /// The additional interface types.
        /// </summary>
        private readonly HashSet<Type> _additionalInterfaceTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="TypeDefinitionBase"/> class.
        /// </summary>
        /// <param name="declaringType">The declaring type.</param>
        protected TypeDefinitionBase(Type declaringType)
        {
            if (declaringType == null)
                throw new ArgumentNullException("declaringType");

            _declaringType = declaringType;

            _customAttributeInfos = new List<AttributeInfo>();
            _declaringInterfaceTypes = new Lazy<ISet<Type>>(() => GetInterfaces(declaringType), false);
            _additionalInterfaceTypes = new HashSet<Type>();
        }

        /// <summary>
        /// Returns all interfaces of the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>The interface types.</returns>
        private static ISet<Type> GetInterfaces(Type type)
        {
            var interfacesTypes = new HashSet<Type>();
            var visitor = Visitor.Create<Type>(t => interfacesTypes.Add(t));

            type.VisitInterfaces(visitor);

            return interfacesTypes;
        }

        /// <summary>
        /// Returns the declaring interface types.
        /// </summary>
        protected IEnumerable<Type> DeclaringInterfaceTypes
        {
            get { return _declaringInterfaceTypes.Value; }
        }

        /// <summary>
        /// Returns the additional interface types.
        /// </summary>
        protected IEnumerable<Type> AdditionalInterfaceTypes
        {
            get { return _additionalInterfaceTypes; }
        }

        /// <summary>
        /// Adds the specified attribute information.
        /// </summary>
        /// <param name="attributeInfo">The attribute information.</param>
        public void AddCustomAttribute(AttributeInfo attributeInfo)
        {
            if (attributeInfo == null)
                throw new ArgumentNullException("attributeInfo");

            _customAttributeInfos.Add(attributeInfo);
        }

        /// <summary>
        /// Adds the specified interface type.
        /// </summary>
        /// <param name="interfaceType">The interface type.</param>
        public void AddInterface(Type interfaceType)
        {
            if (interfaceType == null)
                throw new ArgumentNullException("interfaceType");

            if (!interfaceType.IsInterface)
                throw new ArgumentException(String.Format("Type '{0}' is not an interface type", interfaceType), "interfaceType");

            if (interfaceType.IsGenericTypeDefinition)
                throw new ArgumentException("Interface type must not be a generic type definition", "interfaceType");

            var addInterfaceVisitor = Visitor.Create<Type>(t => _additionalInterfaceTypes.Add(t))
                .Where(t => !_declaringInterfaceTypes.Value.Contains(t));

            interfaceType.VisitInterfaces(addInterfaceVisitor);
        }

        #region ITypeActivator Members

        /// <inheritdoc/>
        public abstract object CreateInstance(Type type, object[] arguments);

        #endregion

        #region ITypeDefinition Members

        /// <inheritdoc/>
        public Type DeclaringType
        {
            get { return _declaringType; }
        }

        /// <inheritdoc/>
        public abstract Type ParentType { get; }

        /// <inheritdoc/>
        public IEnumerable<AttributeInfo> CustomAttributes
        {
            get { return _customAttributeInfos; }
        }

        /// <inheritdoc/>
        public abstract void VisitInterfaces(IVisitor<Type> visitor);

        /// <inheritdoc/>
        public void VisitConstructors(IVisitor<ConstructorInfo> visitor)
        {
            ParentType.VisitConstructors(visitor);
        }

        /// <inheritdoc/>
        public abstract void VisitEvents(IVisitor<EventInfo> visitor);

        /// <inheritdoc/>
        public abstract void VisitProperties(IVisitor<PropertyInfo> visitor);

        /// <inheritdoc/>
        public abstract void VisitMethods(IVisitor<MethodInfo> visitor);

        #endregion

        #region Object Members

        /// <inheritdoc/>
        public override bool Equals(object obj)
        {
            var typeDescriptor = obj as TypeDefinitionBase;

            if (typeDescriptor == null)
                return false;

            if (typeDescriptor.DeclaringType != DeclaringType)
                return false;

            if (typeDescriptor.ParentType != ParentType)
                return false;

            if (!typeDescriptor.AdditionalInterfaceTypes.SequenceEqual(AdditionalInterfaceTypes))
                return false;

            return typeDescriptor.CustomAttributes.SequenceEqual(CustomAttributes);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return DeclaringType.GetHashCode();
        }

        #endregion
    }
}
