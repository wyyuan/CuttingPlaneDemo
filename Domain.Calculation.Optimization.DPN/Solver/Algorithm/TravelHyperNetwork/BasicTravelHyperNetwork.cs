using DataStructures.Graphs;
using Domain.Base.Schedule.Timetable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    /// <summary>
    /// 基本的出行网络
    /// </summary>
    public class BasicTravelHyperNetwork : DirectedWeightedSparseGraph<TravelHyperNode>
    {
        protected DPNProblemContext _ctx;
        protected DiscreteTimeAdapter _adapter;
        //记录link与Service Segment的对应关系
        protected Dictionary<IEdge<TravelHyperNode>, IServiceSegment> _segDic
            = new Dictionary<IEdge<TravelHyperNode>, IServiceSegment>();
        //记录reservation link与Train的对应关系
        protected Dictionary<IEdge<TravelHyperNode>, ITrainTrip> _linkTrainDict
            = new Dictionary<IEdge<TravelHyperNode>, ITrainTrip>();

        // 分种类的索引
        protected List<IEdge<TravelHyperNode>> _reservationLinks = new List<IEdge<TravelHyperNode>>();
        protected List<IEdge<TravelHyperNode>> _intrainLinks = new List<IEdge<TravelHyperNode>>();
        protected List<IEdge<TravelHyperNode>> _waitingLinks = new List<IEdge<TravelHyperNode>>();
        protected List<IEdge<TravelHyperNode>> _finishLinks = new List<IEdge<TravelHyperNode>>();

        public BasicTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter) : base()
        {
            _ctx = ctx;
            _adapter = adapter;
        }

        public Dictionary<IEdge<TravelHyperNode>, ITrainTrip> LinkTrainDict => _linkTrainDict;
        public Dictionary<IEdge<TravelHyperNode>, IServiceSegment> LinkSegmentDict => _segDic;

        public void Build()
        {
            BuildIntrainLinks();
            BuildWaitingLinks();
            BuildReservationLinks();
            BuildFinishLinks();
        }

        protected virtual void BuildFinishLinks()
        {
            //Build finishing links
            foreach (var price in _ctx.PriceLevelList)
            {
                foreach (var sta in _ctx.Wor.Net.StationCollection)
                {
                    var arrSegs = _ctx.Wor.RailwayTimeTable.Trains.SelectMany(i => i.ServiceSegments.Where(s => s.ArrStation == sta));
                    foreach (var seg in arrSegs)
                    {
                        TravelHyperNode arrNode = new TravelHyperNode()
                        {
                            Time = _adapter.Horizon + seg.ArrTime.Hour * 60 + seg.ArrTime.Minute,
                            Station = seg.ArrStation,
                            Price = price
                        };

                        TravelHyperNode endNode = new TravelHyperNode()
                        {
                            Time = _adapter.Horizon + 1440,
                            Station = seg.ArrStation,
                            Price = 0
                        };

                        if (!base.HasVertex(endNode))
                        {
                            base.AddVertex(endNode);
                        }

                        if(!base.AddEdge(arrNode, endNode, GetFinishLinkCost())) throw new Exception("已添加相同Edge");
                        _finishLinks.Add(base.GetEdge(arrNode, endNode));
                    }
                }
            }
        }
        protected virtual void BuildReservationLinks()
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

                                if(!base.AddEdge(depNode, arrNode, GetReservationLinkCost(depNode, arrNode))) throw new Exception("已添加相同Edge"); // 一次预订花费一分钟
                                _reservationLinks.Add(base.GetEdge(depNode, arrNode));
                                _linkTrainDict.Add(base.GetEdge(depNode, arrNode), item.Train);
                            }
                        }
                    }
                }
            }
        }
        protected virtual void BuildWaitingLinks()
        {
            //Build waiting links
            foreach (var station in _ctx.Wor.Net.StationCollection)
            {
                for (int i = 0; i < _adapter.Horizon -1; i++)
                {
                    TravelHyperNode depNode = new TravelHyperNode()
                    {
                        Time = i,
                        Station = station,
                        Price = 0
                    };

                    if (!base.HasVertex(depNode))
                    {
                        base.AddVertex(depNode);
                    }

                    TravelHyperNode arrNode = new TravelHyperNode()
                    {
                        Time = i + 1,
                        Station = station,
                        Price = 0
                    };

                    if (!base.HasVertex(arrNode))
                    {
                        base.AddVertex(arrNode);
                    }
                    if(!base.AddEdge(depNode, arrNode, GetWaitingLinkCost())) throw new Exception("已添加相同Edge");
                    _waitingLinks.Add(base.GetEdge(depNode, arrNode));
                    //adapter.Resolution * ctx.WaitingCost); // One minute each link
                }
            }
        }
        protected virtual void BuildIntrainLinks()
        {
            //Build In-train links
            foreach (var train in _ctx.Wor.RailwayTimeTable.Trains)
            {
                foreach (var price in _ctx.PriceLevelList)
                {
                    //Build In-train links in Section
                    foreach (var seg in train.ServiceSegments)
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

                        if(!base.AddEdge(depNode, arrNode, GetIntrainSectionLinkCost(train, seg, price))) throw new Exception("已添加相同Edge");
                        _intrainLinks.Add(base.GetEdge(depNode, arrNode));
                        _segDic.Add(base.GetEdge(depNode, arrNode), seg);

                    }//Build In-train links in stop station
                    foreach (var stop in train.StopStaions.Skip(1).Take(train.StopStaions.Count() - 2))
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

                        if(!base.AddEdge(arrNode, depNode, GetIntrainStpLinkCost(train, stop, price))) throw new Exception("已添加相同Edge"); ;
                        _intrainLinks.Add(base.GetEdge(arrNode, depNode));
                        //((decimal)(stop.DepTime - stop.ArrTime).TotalMinutes * ctx.Vot));
                    }
                }
            }
        }

        protected virtual decimal GetIntrainSectionLinkCost(ITrainTrip train, IServiceSegment seg, decimal price)
        {
            return _ctx.BasicPriceDic[seg] * price + (decimal)(seg.ArrTime - seg.DepTime).TotalMinutes * _ctx.Vot;
        }
        protected virtual decimal GetIntrainStpLinkCost(ITrainTrip train, IStopStation stop, decimal price)
        {
            return (decimal)(stop.DepTime - stop.ArrTime).TotalMinutes * _ctx.Vot;
        }
        protected virtual decimal GetWaitingLinkCost()
        {
            return _adapter.Resolution * _ctx.WaitingVot;
        }
        protected virtual decimal GetReservationLinkCost(TravelHyperNode depNode, TravelHyperNode arrNode)
        {
            return DpnAlgorithm.ASmallCost;
        }
        protected virtual decimal GetFinishLinkCost()
        {
            return DpnAlgorithm.ASmallCost;
        }

        /// <summary>
        /// 获取路径的cost
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public decimal GetPathCost(TravelPath path)
        {
            decimal lValue = 0;
            for (int i = 0; i < path.Nodepath.Count() - 1; i++)
            {
                lValue += base.GetEdgeWeight(path.Nodepath[i], path.Nodepath[i + 1]);
            }
            return Math.Round(lValue,2);
        }
        /// <summary>
        /// 判断路径是否可行
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool IsPathFeasible(TravelPath path)
        {
            bool res = true;
            for (int i = 0; i < path.Nodepath.Count() - 1; i++)
            {
                res = res && base.HasEdge(path.Nodepath[i], path.Nodepath[i + 1]);
            }
            return res;
        }

        public ITrainTrip GetTrainByReservationLink(IEdge<TravelHyperNode> link)
        {
            var l = _linkTrainDict.Keys.FirstOrDefault(i => i.Source == link.Source && i.Destination == link.Destination);
            if(l!=null)
            {
                return _linkTrainDict[l];
            }
            else
            {
                return null;
            }
        }
        public IServiceSegment GetServiceSegmentByReservationLink(IEdge<TravelHyperNode> link)
        {
                return _segDic[link];
        }
    }
}
