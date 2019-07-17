using DataStructures.Graphs;
using Domain.Base.Schedule.Timetable;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    /// <summary>
    /// 用于模拟旅客到达生成可行解的网络
    /// </summary>
    public class SimNetwork : ControlledTravelHyperNetwork
    {
        private Dictionary<IServiceSegment, List<IEdge<TravelHyperNode>>> _segEdgeDict
            = new Dictionary<IServiceSegment, List<IEdge<TravelHyperNode>>>(); //Segment与edge的映射关系
        private Dictionary<IServiceSegment, int> m_segUsageDict = new Dictionary<IServiceSegment, int>();//当前segment的使用数量
        private Dictionary<IServiceSegment, int> _segCapDict = new Dictionary<IServiceSegment, int>();//segment的容量

        //Reservation link 是否存在  Dictionary<IServiceSegment, int> serviceDic
        public SimNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter,
           Dictionary<IEdge<TravelHyperNode>, bool> y) : base(ctx, adapter, y) { }

        public Dictionary<IServiceSegment, int> ServiceDic { get => m_segUsageDict; }

        /// <summary>
        /// 设置segment的使用数量
        /// </summary>
        /// <param name="seg"></param>
        public void AddUsage(IServiceSegment seg, int num)
        {
            if (m_segUsageDict[seg] + num > _segCapDict[seg])
            {
                throw new System.Exception("正在使用不可用ServiceSegment!");
            }
            else
            {
                m_segUsageDict[seg] += num;
                if (m_segUsageDict[seg] == _segCapDict[seg])
                {
                    foreach (var edge in _segEdgeDict[seg])
                    {
                        if (HasEdge(edge.Source, edge.Destination))
                        {
                            RemoveEdge(edge.Source, edge.Destination);
                        }
                    }
                }
            }
        }
        public void AddUsage(Dictionary<IServiceSegment, int> dict)
        {
            foreach (var pair in dict)
            {
                AddUsage(pair.Key, pair.Value);
            }
        }
        protected override void BuildIntrainLinks()
        {
            //Build In-train links
            foreach (var train in _ctx.Wor.RailwayTimeTable.Trains)
            {
                //Build In-train links in Section
                foreach (var seg in train.ServiceSegments)
                {
                    m_segUsageDict.Add(seg, 0);
                    _segCapDict.Add(seg, train.Carriage.Chairs.Count());
                    _segEdgeDict.Add(seg, new List<IEdge<TravelHyperNode>>());
                    foreach (var price in _ctx.PriceLevelList)
                    {
                        TravelHyperNode depNode = new TravelHyperNode()
                        {
                            Time = _adapter.Horizon + seg.DepTime.Hour * 60 + seg.DepTime.Minute,
                            Station = seg.DepStation,
                            Price = price
                        };

                        if (!base.HasVertex(depNode))
                        {
                            base.AddVertex(depNode);
                        }

                        TravelHyperNode arrNode = new TravelHyperNode()
                        {
                            Time = _adapter.Horizon + seg.ArrTime.Hour * 60 + seg.ArrTime.Minute,
                            Station = seg.ArrStation,
                            Price = price
                        };

                        if (!base.HasVertex(arrNode))
                        {
                            base.AddVertex(arrNode);
                        }

                        base.AddEdge(depNode, arrNode, GetIntrainSectionLinkCost(train, seg, price));
                        _segEdgeDict[seg].Add(base.GetEdge(depNode, arrNode));
                        _segDic.Add(base.GetEdge(depNode, arrNode), seg);
                    }
                }//Build In-train links in stop station
                foreach (var stop in train.StopStaions.Skip(1).Take(train.StopStaions.Count() - 2))
                {
                    foreach (var price in _ctx.PriceLevelList)
                    {
                        TravelHyperNode arrNode = new TravelHyperNode()
                        {
                            Time = _adapter.Horizon + stop.ArrTime.Hour * 60 + stop.ArrTime.Minute,
                            Station = stop.Station,
                            Price = price
                        };
                        TravelHyperNode depNode = new TravelHyperNode()
                        {
                            Time = _adapter.Horizon + stop.DepTime.Hour * 60 + stop.DepTime.Minute,
                            Station = stop.Station,
                            Price = price
                        };
                        base.AddEdge(arrNode, depNode, GetIntrainStpLinkCost(train, stop, price));
                    }
                }
            }
        }
    }
}
