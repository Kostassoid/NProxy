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

namespace NProxy.Core.Test.Types
{
    internal interface IGeneric<in TValue>
    {
        void Method(TValue value);
    }

    internal interface IGenericParameter<in TFirst>
    {
        void Method<TSecond>(TFirst first, TSecond second);
    }

    internal interface IGenericEvent<out TValue>
    {
        event Action<TValue> Event;
    }

    internal interface IGenericProperty<TValue>
    {
        TValue Property { get; set; }
    }

    internal interface IGenericMethod<out TValue>
    {
        TValue Method();
    }
}