﻿using System;
using System.Collections.Generic;
#if DEBUG
using System.Diagnostics;
#endif
using System.Linq;
using pwiz.Common.Collections;
using pwiz.Common.SystemUtil;

namespace pwiz.Common.DataBinding
{
    public class PivotedProperties
    {
        public PivotedProperties(ItemProperties itemProperties) : this(itemProperties, ImmutableList.Empty<SeriesGroup>())
        {
        }
        private PivotedProperties(ItemProperties itemProperties, IEnumerable<SeriesGroup> seriesGroups)
        {
            ItemProperties = itemProperties;
            SeriesGroups = ImmutableList.ValueOf(seriesGroups);
        }

        public ItemProperties ItemProperties { get; private set; }

        public ImmutableList<SeriesGroup> SeriesGroups { get; private set; }

        public class SeriesGroup
        {
            public SeriesGroup(IEnumerable<object> pivotKeys, IEnumerable<object> pivotCaptions, IEnumerable<Series> series)
            {
                PivotKeys = ImmutableList.ValueOf(pivotKeys);
                PivotCaptions = ImmutableList.ValueOf(PivotCaptions);
                SeriesList = ImmutableList.ValueOf(series);
            }
            public ImmutableList<object> PivotKeys { get; }
            public ImmutableList<IColumnCaption> PivotCaptions { get; }
            public ImmutableList<Series> SeriesList { get; private set; }
            public SeriesGroup ReorderPivotKeys(IList<int> newOrder)
            {
                return new SeriesGroup(newOrder.Select(i=>PivotKeys[i]),
                    newOrder.Select(i=>PivotCaptions[i]),
                    SeriesList.Select(series=>series.ReorderProperties(newOrder)));
            }

            public SeriesGroup RenumberProperties(IList<int> newNumbering)
            {
                return new SeriesGroup(PivotKeys, PivotCaptions, SeriesList.Select(series=>series.RenumberProperties(newNumbering)));
            }
        }

        public class Series : Immutable
        {
            public Series(object seriesId, IColumnCaption seriesCaption, IEnumerable<int> propertyIndexes, Type propertyType)
            {
                SeriesId = seriesId;
                SeriesCaption = seriesCaption;
                PropertyIndexes = ImmutableList.ValueOf(propertyIndexes);
                PropertyType = propertyType;
            }
            public object SeriesId { get; }
            public IColumnCaption SeriesCaption { get; }
            public ImmutableList<int> PropertyIndexes { get; }
            public Type PropertyType { get; }

            public Series ReorderProperties(IList<int> newOrder)
            {
                return new Series(SeriesId, SeriesCaption, newOrder.Select(i => PropertyIndexes[i]), PropertyType);
            }

            public Series RenumberProperties(IList<int> newNumbering)
            {
                return new Series(SeriesId, SeriesCaption, PropertyIndexes.Select(i=>newNumbering[i]), PropertyType);
            }
        }

        public IEnumerable<SeriesGroup> CreateSeriesGroups()
        {
            // Create a lookup from SeriesId to properties in that series
            var propertiesBySeriesId = Enumerable.Range(0, ItemProperties.Count)
                .Select(i => Tuple.Create(i, ItemProperties[i].PivotedColumnId))
                .Where(tuple => null != tuple.Item2)
                .ToLookup(tuple => Tuple.Create(tuple.Item2.SeriesId, ItemProperties[tuple.Item1].PropertyType));
            var seriesList = new List<Tuple<Series, ImmutableList<object>, ImmutableList<IColumnCaption>>>();
            foreach (var seriesTuples in propertiesBySeriesId)
            {
                if (seriesTuples.Count() <= 1)
                {
                    continue;
                }

                var firstProperty = ItemProperties[seriesTuples.First().Item1];
                var firstPivotColumnId = seriesTuples.First().Item2;
                var series = new Series(firstPivotColumnId.SeriesId, firstPivotColumnId.SeriesCaption,
                    seriesTuples.Select(tuple => tuple.Item1),
                    firstProperty.PropertyType);

                seriesList.Add(Tuple.Create(series,
                    ImmutableList.ValueOf(seriesTuples.Select(tuple => tuple.Item2.PivotKey)),
                    ImmutableList.ValueOf(seriesTuples.Select(tuple => tuple.Item2.PivotKeyCaption))));
            }

            return seriesList.ToLookup(tuple => tuple.Item2).Select(grouping =>
                new SeriesGroup(grouping.First().Item2, grouping.First().Item3, grouping.Select(tuple => tuple.Item1)));
        }

        public PivotedProperties ChangeSeriesGroups(IEnumerable<SeriesGroup> newGroups)
        {
            return new PivotedProperties(ItemProperties, newGroups);
        }

        public PivotedProperties ReorderPivots(IList<IList<int>> newPivotOrders)
        {
            if (newPivotOrders.Count != SeriesGroups.Count)
            {
                throw new ArgumentException();
            }

            var newGroups = new List<SeriesGroup>();
            for (int iGroup = 0; iGroup < newPivotOrders.Count; iGroup++)
            {
                var pivotOrder = newPivotOrders[iGroup];
                newGroups.Add(SeriesGroups[iGroup].ReorderPivotKeys(pivotOrder));
            }
            return new PivotedProperties(ItemProperties, newGroups);
        }

        /// <summary>
        /// Reorder the ItemProperties collection so that the ungrouped properties come first,
        /// followed by the grouped properties.
        /// If a group contains multiple series, the properties from those series are interleaved
        /// with each other.
        /// </summary>
        /// <returns></returns>
        public PivotedProperties ReorderItemProperties()
        {
            var groupedPropertyIndexes = SeriesGroups
                .SelectMany(group => group.SeriesList.SelectMany(series => series.PropertyIndexes)).ToHashSet();
            var newOrder = new List<int>();
            newOrder.AddRange(Enumerable.Range(0, ItemProperties.Count).Where(i=>!groupedPropertyIndexes.Contains(i)));
            newOrder.AddRange(SeriesGroups.SelectMany(group =>
                Enumerable.Range(0, group.PivotKeys.Count)
                    .SelectMany(i => group.SeriesList.Select(series => series.PropertyIndexes[i]))));

            var newNumbering = new int[newOrder.Count];
            for (int i = 0; i < newOrder.Count; i++)
            {
                newNumbering[newOrder[i]] = i;
            }

            var newItemProperties = new ItemProperties(newOrder.Select(i => ItemProperties[i]));
            var result = new PivotedProperties(newItemProperties, SeriesGroups.Select(group=>group.RenumberProperties(newNumbering)));
#if DEBUG
            Debug.Assert(ItemProperties.ToHashSet().SetEquals(result.ItemProperties.ToHashSet()));
            Debug.Assert(SeriesGroups.Count == result.SeriesGroups.Count);
            for (int iGroup = 0; iGroup < SeriesGroups.Count; iGroup++)
            {
                Debug.Assert(SeriesGroups[iGroup].SeriesList.Count == result.SeriesGroups[iGroup].SeriesList.Count);
                Debug.Assert(SeriesGroups[iGroup].PivotKeys.SequenceEqual(result.SeriesGroups[iGroup].PivotKeys));
                for (int iSeries = 0; iSeries < SeriesGroups[iGroup].SeriesList.Count; iSeries++)
                {
                    var resultSeries = result.SeriesGroups[iGroup].SeriesList[iSeries];
                    Debug.Assert(resultSeries.PropertyIndexes.OrderBy(i => i).SequenceEqual(resultSeries.PropertyIndexes));

                    var series = SeriesGroups[iGroup].SeriesList[iSeries];
                    Debug.Assert(series.PropertyIndexes.Select(i => ItemProperties[i])
                        .SequenceEqual(resultSeries.PropertyIndexes.Select(i => result.ItemProperties[i])));
                }
            }
#endif
            return result;
        }

        public Tuple<SeriesGroup, Series> FindSeriesForProperty(DataPropertyDescriptor dataPropertyDescriptor)
        {
            var pivotColumnId = dataPropertyDescriptor.PivotedColumnId;
            if (pivotColumnId == null)
            {
                return null;
            }

            foreach (var group in SeriesGroups)
            {
                var result = group.SeriesList.FirstOrDefault(series => Equals(series.SeriesId, pivotColumnId.SeriesId));
                if (result != null)
                {
                    return Tuple.Create(group, result);
                }
            }

            return null;
        }
    }
}