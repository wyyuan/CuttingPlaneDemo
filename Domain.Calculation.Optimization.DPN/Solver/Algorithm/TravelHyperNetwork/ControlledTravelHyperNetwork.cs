using DataStructures.Graphs;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    public class ControlledTravelHyperNetwork : BasicTravelHyperNetwork
    {
        protected Dictionary<IEdge<TravelHyperNode>, bool> _y;

        public ControlledTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            Dictionary<IEdge<TravelHyperNode>, bool> y) : base(ctx, adapter)
        {
            _y = y;
        }

        protected override void BuildReservationLinks()
        {
            //Build Reservation links
            foreach (var sta in _ctx.Wor.Net.StationCollection)
            {
                //获取车次出发点
                var items = _ctx.Wor.RailwayTimeTable.Trains.SelectMany(i => new[] { new { Train = i, Segs = i.ServiceSegments.Where(s => s.DepStation == sta) } });
                foreach (var item in items)
                {
                    foreach (var seg in item.Segs)
                    {
                        foreach (var price in _ctx.PriceLevelList)
                        {
                            TravelHyperNode arrNode = new TravelHyperNode()
                            {
                                Time = _adapter.Horizon + seg.DepTime.Hour * 60 + seg.DepTime.Minute,
                                Station = seg.DepStation,
                                Price = price
                            };

                            for (int i = 0; i < _adapter.Horizon; i++)
                            {
                                TravelHyperNode depNode = new TravelHyperNode()
                                {
                                    Time = i,
                                    Station = sta,
                                    Price = 0
                                };

                                var edge = _y.Keys.FirstOrDefault(link => link.Source == depNode && link.Destination == arrNode);

                                if (edge != null && _y[edge])
                                {
                                    base.AddEdge(depNode, arrNode, GetReservationLinkCost(depNode, arrNode)); // 一次预订花费一分钟
                                    _linkTrainDict.Add(base.GetEdge(depNode, arrNode), item.Train);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
