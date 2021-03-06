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
using System.Threading;
using System.Threading.Tasks;
using NProxy.Core.Internal.Caching;
using NUnit.Framework;

namespace NProxy.Core.Test.Internal.Caching
{
    [TestFixture]
    public sealed class LockOnWriteCacheTestFixture
    {
        [Test]
        public void GetOrAddWithoutCacheHitTest()
        {
            // Arrange
            var invocationCount = 0;
            Func<int, string> valueFactory = k =>
                {
                    invocationCount++;
                    return Convert.ToString(k);
                };
            var cache = new LockOnWriteCache<int, string>();

            // Act
            var value = cache.GetOrAdd(1, valueFactory);

            // Assert
            Assert.That(value, Is.EqualTo("1"));
            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void GetOrAddWithCacheHitTest()
        {
            // Arrange
            // Arrange
            var invocationCount = 0;
            Func<int, string> valueFactory = k =>
                {
                    invocationCount++;
                    return Convert.ToString(k);
                };
            var cache = new LockOnWriteCache<int, string>();

            // Act
            cache.GetOrAdd(1, valueFactory);

            var value = cache.GetOrAdd(1, valueFactory);

            // Assert
            Assert.That(value, Is.EqualTo("1"));
            Assert.That(invocationCount, Is.EqualTo(1));
        }

        [Test]
        public void GetOrAddWithoutCacheHitAndConcurrencyTest()
        {
            // Arrange
            var monitor = new Object();
            var invocationCount = 0;
            Func<int, string> valueFactory = k =>
                {
                    lock (monitor)
                    {
                        Monitor.Wait(monitor);
                    }

                    invocationCount++;
                    return Convert.ToString(k);
                };
            var cache = new LockOnWriteCache<int, string>();

            // Act
            var firstTask = Task.Factory.StartNew(() => cache.GetOrAdd(1, valueFactory));
            var secondTask = Task.Factory.StartNew(() => cache.GetOrAdd(1, valueFactory));

            firstTask.Wait(500);
            secondTask.Wait(500);

            Assert.That(firstTask.IsCompleted, Is.False);
            Assert.That(secondTask.IsCompleted, Is.False);

            lock (monitor)
            {
                Monitor.Pulse(monitor);
            }

            Task.WaitAll(firstTask, secondTask);

            // Assert
            Assert.That(firstTask.Result, Is.EqualTo("1"));
            Assert.That(secondTask.Result, Is.EqualTo("1"));
            Assert.That(invocationCount, Is.EqualTo(1));
        }
    }
}