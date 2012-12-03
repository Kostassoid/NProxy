﻿//
// NProxy is a library for the .NET framework to create lightweight dynamic proxies.
// Copyright © 2012 Martin Tamme
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
using NProxy.Core.Internal.Reflection;
using NProxy.Core.Internal.Common;

namespace NProxy.Core.Interceptors
{
    /// <summary>
    /// Represents an interceptor invocation handler.
    /// </summary>
    internal sealed class InterceptorInvocationHandler : IInvocationHandler
    {
        /// <summary>
        /// The default interceptors.
        /// </summary>
        private readonly IInterceptor[] _defaultInterceptors;

        /// <summary>
        /// The interceptors.
        /// </summary>
        private readonly Dictionary<long, IInterceptor[]> _interceptors;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterceptorInvocationHandler"/> class.
        /// </summary>
        /// <param name="defaultInterceptors">The default interceptor.</param>
        public InterceptorInvocationHandler(params IInterceptor[] defaultInterceptors)
        {
            if (defaultInterceptors == null)
                throw new ArgumentNullException("defaultInterceptors");

            _defaultInterceptors = defaultInterceptors;

            _interceptors = new Dictionary<long, IInterceptor[]>();
        }

        /// <summary>
        /// Applies all interceptors for the specified type.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="interceptors">The interceptors.</param>
        public void ApplyInterceptors(Type type, IEnumerable<IInterceptor> interceptors)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            if (interceptors == null)
                throw new ArgumentNullException("interceptors");

            // Apply type interception behaviors.
            var typeInterceptors = ApplyInterceptionBehaviors(type, interceptors);

            // Apply event interception behaviors.
            var eventVisitor = Visitor.Create<EventInfo>(e => ApplyInterceptors(e, typeInterceptors));

            type.VisitEvents(eventVisitor);

            // Apply property interception behaviors.
            var propertyVisitor = Visitor.Create<PropertyInfo>(p => ApplyInterceptors(p, typeInterceptors));

            type.VisitProperties(propertyVisitor);

            // Apply method interception behaviors.
            var methodVisitor = Visitor.Create<MethodInfo>(m => ApplyInterceptors(m, typeInterceptors));

            type.VisitMethods(methodVisitor);
        }

        /// <summary>
        /// Applies all interceptors for the specified event.
        /// </summary>
        /// <param name="eventInfo">The event information.</param>
        /// <param name="interceptors">The interceptors.</param>
        private void ApplyInterceptors(EventInfo eventInfo, IEnumerable<IInterceptor> interceptors)
        {
            var eventInterceptors = ApplyInterceptionBehaviors(eventInfo, interceptors);

            foreach (var methodInfo in eventInfo.GetAccessorMethods())
            {
                ApplyInterceptors(methodInfo, eventInterceptors);
            }
        }

        /// <summary>
        /// Applies all interceptors for the specified property.
        /// </summary>
        /// <param name="propertyInfo">The property information.</param>
        /// <param name="interceptors">The interceptors.</param>
        private void ApplyInterceptors(PropertyInfo propertyInfo, IEnumerable<IInterceptor> interceptors)
        {
            var propertyInterceptors = ApplyInterceptionBehaviors(propertyInfo, interceptors);

            foreach (var methodInfo in propertyInfo.GetAccessorMethods())
            {
                ApplyInterceptors(methodInfo, propertyInterceptors);
            }
        }

        /// <summary>
        /// Applies all interceptors for the specified method.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="interceptors">The interceptors.</param>
        private void ApplyInterceptors(MethodInfo methodInfo, IEnumerable<IInterceptor> interceptors)
        {
            var methodInterceptors = ApplyInterceptionBehaviors(methodInfo, interceptors);

            SetInterceptors(methodInfo, methodInterceptors);
        }

        /// <summary>
        /// Applies the interception behaviors for the specified member.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="interceptors">The interceptors.</param>
        /// <returns>The member interceptors.</returns>
        private static IList<IInterceptor> ApplyInterceptionBehaviors(MemberInfo memberInfo, IEnumerable<IInterceptor> interceptors)
        {
            var interceptionBehaviors = memberInfo.GetCustomAttributes<IInterceptionBehavior>();
            var memberInterceptors = new List<IInterceptor>(interceptors);

            foreach (var interceptionBehavior in interceptionBehaviors)
            {
                interceptionBehavior.Validate(memberInfo);
                interceptionBehavior.Apply(memberInfo, memberInterceptors);
            }

            return memberInterceptors;
        }

        /// <summary>
        /// Sets the interceptors for the specified method.
        /// </summary>
        /// <param name="methodInfo">The method information.</param>
        /// <param name="interceptors">The interceptors.</param>
        private void SetInterceptors(MethodInfo methodInfo, IList<IInterceptor> interceptors)
        {
            if (interceptors.Count == 0)
                return;

            var methodToken = methodInfo.GetToken();

            _interceptors.Add(methodToken, interceptors.Concat(_defaultInterceptors).ToArray());
        }

        /// <summary>
        /// Returns all interceptors for the specified member.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <returns>The interceptors.</returns>
        private IInterceptor[] GetInterceptors(MemberInfo memberInfo)
        {
            var methodToken = memberInfo.GetToken();
            IInterceptor[] interceptors;

            return _interceptors.TryGetValue(methodToken, out interceptors) ? interceptors : _defaultInterceptors;
        }

        #region IInvocationHandler Members

        /// <inheritdoc/>
        public object Invoke(object proxy, MethodInfo methodInfo, object[] parameters)
        {
            var interceptors = GetInterceptors(methodInfo);
            var invocationContext = new InvocationContext(proxy, methodInfo, parameters, interceptors);

            return invocationContext.Proceed();
        }

        #endregion
    }
}
