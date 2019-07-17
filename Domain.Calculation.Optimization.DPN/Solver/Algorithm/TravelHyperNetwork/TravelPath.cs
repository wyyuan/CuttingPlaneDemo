using DataStructures.Graphs;
using Domain.Base.Network.RailwayBasicNetwork;
using Domain.Base.Schedule.Timetable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    /// <summary>
    /// 表示一条出行路径
    /// </summary>
    public struct TravelPath
    {
        //时间_车站_价格
        private List<TravelHyperNode> _nodepath;

        public TravelPath(DirectedWeightedSparseGraph<TravelHyperNode> graph, IEnumerable<TravelHyperNode> nodes)
        {
            StartStation = null;
            TotalTime = -1;
            ReservationTime = -1;
            Price = -1;
            ReservationArc = null;

            _nodepath = nodes.ToList();
            for (int i = 0; i < _nodepath.Count - 1; i++)
            {
                if (_nodepath[i].Price == 0 && _nodepath[i+1].Price != 0)
                {
                    StartStation = _nodepath[i].Station;
                    ReservationTime = Convert.ToInt32(_nodepath[i].Time);
                    Price = _nodepath[i+1].Price;
                    ReservationArc = graph.GetEdge(_nodepath[i], _nodepath[i+1]);
                }
            }
        }

        public List<TravelHyperNode> Nodepath { get => _nodepath; }
        /// <summary>
        /// 出发车站ID
        /// </summary>
        public IRailwayStation StartStation { get; }
        /// <summary>
        /// 到达的时间
        /// </summary>
        public int TotalTime { get; }
        /// <summary>
        /// 开始订票的时间
        /// </summary>
        public int ReservationTime { get; }
        /// <summary>
        /// 购票时的价格等级
        /// </summary>
        public decimal Price { get; }
        /// <summary>
        /// 订票弧
        /// </summary>
        public IEdge<TravelHyperNode> ReservationArc { get; }
        /// <summary>
        /// 等车时间
        /// </summary>
        /// <Marker>(注意，这里是相对时间，需要乘以当前的resolution才是绝对时间)</Marker>
        /// <returns></returns>
        public int GetWaitTime()
        {
            return Nodepath.Where(i => i.Price == 0 ).Count()-2;//去除收尾两个节点
        }
        /// <summary>
        /// 获取采用的服务集合
        /// </summary>
        /// <param name="SegDic"></param>
        /// <returns></returns>
        internal IEnumerable<IServiceSegment> GetSegments(BasicTravelHyperNetwork network)//获取经过的Service Segment集合
        {
            if (Nodepath != null)
            {
                for (int i = 0; i < Nodepath.Count - 1; i++)
                {
                    if (network.HasEdge(Nodepath[i], Nodepath[i + 1]))
                    {
                        var edge = network.GetEdge(Nodepath[i], Nodepath[i + 1]);
                        if (network.LinkSegmentDict.TryGetValue(edge, out IServiceSegment seg))
                        {
                            yield return seg;
                        }
                    }
                }
            }
        }
    }
}
