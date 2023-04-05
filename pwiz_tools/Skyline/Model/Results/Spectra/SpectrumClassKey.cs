﻿/*
 * Original author: Nicholas Shulman <nicksh .at. u.washington.edu>,
 *                  MacCoss Lab, Department of Genome Sciences, UW
 *
 * Copyright 2023 University of Washington - Seattle, WA
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
using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.Spectra;
using pwiz.Common.SystemUtil;

namespace pwiz.Skyline.Model.Results.Spectra
{
    public class SpectrumClassKey : Immutable
    {
        public SpectrumClassKey(ImmutableList<SpectrumClassColumn> columns, SpectrumMetadata spectrumMetadata)
            : this(columns, columns.Select(col => col.GetValue(spectrumMetadata)))
        {
        }

        public SpectrumClassKey(ImmutableList<SpectrumClassColumn> columns, IEnumerable<object> values)
        {
            Columns = columns;
            Values = ImmutableList.ValueOf(values);
        }

        public ImmutableList<SpectrumClassColumn> Columns { get; private set; }
        public ImmutableList<object> Values { get; private set; }

        protected bool Equals(SpectrumClassKey other)
        {
            return Equals(Columns, other.Columns) && Equals(Values, other.Values);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((SpectrumClassKey) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Columns != null ? Columns.GetHashCode() : 0) * 397) ^ (Values != null ? Values.GetHashCode() : 0);
            }
        }
    }
}
