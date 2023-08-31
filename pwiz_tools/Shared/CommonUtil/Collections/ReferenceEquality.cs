/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2018 University of Washington - Seattle, WA
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace pwiz.Common.Collections
{
    /// <summary>
    /// Equality comparer which tests object identity
    /// </summary>
    public class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceEqualityComparer INSTANCE = new ReferenceEqualityComparer();

        private ReferenceEqualityComparer()
        {
        }
        bool IEqualityComparer<object>.Equals(object x, object y)
        {
            return ReferenceEquals(x, y);
        }

        int IEqualityComparer<object>.GetHashCode(object obj)
        {
            return RuntimeHelpers.GetHashCode(obj);
        }
    }

    /// <summary>
    /// Wrapper around objects which uses reference equality to implement Equals and GetHashCode.
    /// </summary>
    public readonly struct ReferenceValue<T> : IEquatable<ReferenceValue<T>> where T : class
    {
        public ReferenceValue(T value)
        {
            Value = value;
        }
        public T Value { get; }

        bool IEquatable<ReferenceValue<T>>.Equals(ReferenceValue<T> other)
        {
            return ReferenceEquals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            return obj is ReferenceValue<T> other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value == null ? 0 : RuntimeHelpers.GetHashCode(Value);
        }

        public static implicit operator T(ReferenceValue<T> value)
        {
            return value.Value;
        }

        public static implicit operator ReferenceValue<T>([CanBeNull] T value)
        {
            return new ReferenceValue<T>(value);
        }
    }

    public static class ReferenceValue
    {
        public static ReferenceValue<T> Of<T>(T value) where T : class
        {
            return new ReferenceValue<T>(value);
        }
    }
}
