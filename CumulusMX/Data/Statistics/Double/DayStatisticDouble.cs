﻿using System;
using Newtonsoft.Json;

namespace CumulusMX.Data.Statistics.Double
{
    [JsonObject(MemberSerialization.OptIn)]
    internal class DayStatisticDouble
    {
        [JsonProperty]
        public double HighestMinimum { get; private set; }
        [JsonProperty]
        public double LowestMaximum { get; private set; }
        [JsonProperty]
        public DateTime LowestMaximumDay { get; private set; }
        [JsonProperty]
        public DateTime HighestMinimumDay { get; private set; }

        public double HighestTotal => _total.Maximum;
        public double LowestTotal => _total.Minimum;
        public DateTime HighestTotalDay => _total.MaximumTime;
        public DateTime LowestTotalDay => _total.MinimumTime;

        [JsonProperty]
        private readonly MaxMinAverageDouble _range;
        [JsonProperty]
        private readonly MaxMinAverageDouble _total;
        [JsonProperty]
        private int _count;

        public double HighestRange => _range.Maximum;
        public double LowestRange => _range.Minimum;
        public DateTime HighestRangeDay => _range.MaximumTime;
        public DateTime LowestRangeDay => _range.MinimumTime;

        public DayStatisticDouble()
        {
            _range = new MaxMinAverageDouble();
            _total = new MaxMinAverageDouble();
            Reset();
        }

        public void Add(MaxMinAverageDouble dayStatistics)
        {
            DateTime day = dayStatistics.MaximumTime.Date;
            _range.AddValue(day, dayStatistics.Maximum- dayStatistics.Minimum);
            _total.AddValue(day,dayStatistics.Total);
            _count++;

            if (dayStatistics.Maximum < LowestMaximum || _count == 0)
            {
                LowestMaximum = dayStatistics.Maximum;
                LowestMaximumDay = day;
            }
            if (dayStatistics.Minimum > HighestMinimum || _count == 0)
            {
                HighestMinimum = dayStatistics.Minimum;
                HighestMinimumDay = day;
            }
        }

        public void Reset()
        {
            LowestMaximum = 0.0;
            HighestMinimum = 0.0;
            _count = 0;
            LowestMaximumDay = DateTime.Today;
            HighestMinimumDay = DateTime.Today;
            _total.Reset();
            _range.Reset();
        }
    }
}