using Domain.Base.Network.RailwayBasicNetwork;
using System;
using System.Diagnostics;

namespace Domain.Calculation.Optimization.DPN
{
    [DebuggerDisplay("{this.ToString()}")]
    public struct TravelHyperNode : IEquatable<TravelHyperNode>, IComparable<TravelHyperNode>
    {
        public int Time;
        public IRailwayStation Station;
        public decimal Price;

        public int CompareTo(TravelHyperNode other)
        {
            if(this.Time == other.Time && this.Station == other.Station
                && this.Price == other.Price)
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }
        public bool Equals(TravelHyperNode other)
        {
            return this.Time == other.Time
                && this.Station == other.Station
                && this.Price == other.Price;
        }
        public override string ToString()
        {
            return $"{Time}_{Station.StaName}_{Price}";
        }

        public static bool operator ==(TravelHyperNode p1, TravelHyperNode p2)
        {
            return p1.Equals(p2);
        }
        public static bool operator !=(TravelHyperNode p1, TravelHyperNode p2)
        {
            return !p1.Equals(p2);
        }
    }
}
