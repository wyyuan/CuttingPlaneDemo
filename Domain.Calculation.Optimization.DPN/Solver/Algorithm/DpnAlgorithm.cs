using DataStructures.Graphs;
using Domain.Base.Demand.Reservation;
using Domain.Base.Network.RailwayBasicNetwork;
using Domain.Base.Schedule.Timetable;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    public static class DpnAlgorithm
    {
        public static decimal ASmallCost = 0.000001m;//为不考虑weight的arc增加一个极小的weight

        //给定w的网络
        public static DirectedWeightedSparseGraph<string> BuildSolutionGraph(
            DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            Dictionary<WeightedEdge<string>, bool> y)
        {
            int transferThreshold = ctx.TransferThreshold;
            List<decimal> priceLevelList = ctx.PriceLevelList;

            DirectedWeightedSparseGraph<string> graph = new DirectedWeightedSparseGraph<string>();

            //Build In-train links
            foreach (var train in ctx.Wor.RailwayTimeTable.Trains)
            {
                foreach (var price in priceLevelList)
                {
                    //Build In-train links in Section
                    foreach (var seg in train.ServiceSegments)
                    {
                        string depNode = $"{adapter.Horizon + seg.DepTime.Hour * 60 + seg.DepTime.Minute}" +
                            $"_{seg.DepStation.RailwayStationID}_{price}";//价格等级默认是1
                        if (!graph.HasVertex(depNode))
                        {
                            graph.AddVertex(depNode);
                        }
                        string arrNode = $"{adapter.Horizon + seg.ArrTime.Hour * 60 + seg.ArrTime.Minute}" +
                            $"_{seg.ArrStation.RailwayStationID}_{price}";//价格等级默认是1
                        if (!graph.HasVertex(arrNode))
                        {
                            graph.AddVertex(arrNode);
                        }
                        graph.AddEdge(depNode, arrNode,
                            (ctx.BasicPriceDic[seg] * price + (decimal)(seg.ArrTime - seg.DepTime).TotalMinutes * ctx.Vot));

                    }//Build In-train links in stop station
                    foreach (var stop in train.StopStaions.Skip(1).Take(train.StopStaions.Count() - 2))
                    {
                        string arrNode = $"{adapter.Horizon + stop.ArrTime.Hour * 60 + stop.ArrTime.Minute}" +
                            $"_{stop.Station.RailwayStationID}_{price}";//价格等级默认是1
                        string depNode = $"{adapter.Horizon + stop.DepTime.Hour * 60 + stop.DepTime.Minute}" +
                            $"_{stop.Station.RailwayStationID}_{price}";//价格等级默认是1
                        graph.AddEdge(arrNode, depNode,
                             ((decimal)(stop.DepTime - stop.ArrTime).TotalMinutes * ctx.Vot));
                    }
                }
            }

            //Build waiting links
            foreach (var ms in ctx.Wor.Mar as IEnumerable<IRailwayMarketSegment>)
            {
                for (int i = 0; i < adapter.Horizon; i++)
                {
                    if (!graph.HasVertex($"{i}_{ms.OriSta.RailwayStationID}_0"))
                    {
                        graph.AddVertex($"{i}_{ms.OriSta.RailwayStationID}_0");
                    }

                    if (!graph.HasVertex($"{i + 1}_{ms.OriSta.RailwayStationID}_0"))
                    {
                        graph.AddVertex($"{i + 1}_{ms.OriSta.RailwayStationID}_0");
                    }
                    graph.AddEdge($"{i}_{ms.OriSta.RailwayStationID}_0",
                        $"{i + 1}_{ms.OriSta.RailwayStationID}_0",
                        adapter.Resolution * ctx.WaitingVot); // One minute each link
                }
            }

            //Build Reservation links
            foreach (var sta in ctx.Wor.Net.StationCollection)
            {
                //获取车次出发点
                var depSegs = ctx.Wor.RailwayTimeTable.Trains.SelectMany(i => i.ServiceSegments.Where(s => s.DepStation == sta));
                foreach (var seg in depSegs)
                {
                    foreach (var price in priceLevelList)
                    {
                        string depNode = $"{adapter.Horizon + seg.DepTime.Hour * 60 + seg.DepTime.Minute}_{seg.DepStation.RailwayStationID}_{price}";
                        for (int i = 0; i < adapter.Horizon; i++)
                        {
                            var edge = y.Keys.FirstOrDefault(link => link.Source == $"{i}_{sta.RailwayStationID.ToString()}_0"
                                                             && link.Destination == depNode);

                            if (edge != null && y[edge])
                                graph.AddEdge($"{i}_{sta.RailwayStationID.ToString()}_0", depNode, ASmallCost); // 一次预订花费一分钟
                        }
                    }
                }
            }

            //Build finishing links
            foreach (var price in priceLevelList)
            {
                foreach (var sta in ctx.Wor.Net.StationCollection)
                {
                    var arrSegs = ctx.Wor.RailwayTimeTable.Trains.SelectMany(i => i.ServiceSegments.Where(s => s.ArrStation == sta));
                    foreach (var seg in arrSegs)
                    {
                        string arrNode = $"{adapter.Horizon + seg.ArrTime.Hour * 60 + seg.ArrTime.Minute}_{seg.ArrStation.RailwayStationID}_{price}";//价格等级
                        string endNode = $"End_{sta.RailwayStationID.ToString()}_0";
                        if (!graph.HasVertex(endNode))
                        {
                            graph.AddVertex(endNode);
                        }
                        graph.AddEdge(arrNode, endNode, ASmallCost);
                    }
                }
            }

            //Build quit links
            /*
            foreach (var ms in ctx.Wor.Mar as IEnumerable<IRailwayMarketSegment>)
            {
                graph.AddEdge($"{adapter.Horizon}_{ms.OriSta.RailwayStationID}_0",
                    $"End_{ms.DesSta.RailwayStationID}_0", 999m);
            }
            */
            return graph;
        }

        internal static DirectedWeightedSparseGraph<string> BuildSolutionGraph(
            DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            Dictionary<WeightedEdge<string>, bool> y,
            Dictionary<IServiceSegment, int> serviceDic)
        {
            int transferThreshold = ctx.TransferThreshold;
            List<decimal> priceLevelList = ctx.PriceLevelList;

            DirectedWeightedSparseGraph<string> graph = new DirectedWeightedSparseGraph<string>();

            //Build In-train links
            foreach (var train in ctx.Wor.RailwayTimeTable.Trains)
            {
                foreach (var price in priceLevelList)
                {
                    //Build In-train links in Section
                    foreach (var seg in train.ServiceSegments)
                    {
                        if (serviceDic[seg] > train.Carriage.Chairs.Count()) continue;

                        string depNode = $"{adapter.Horizon + seg.DepTime.Hour * 60 + seg.DepTime.Minute}" +
                            $"_{seg.DepStation.RailwayStationID}_{price}";//价格等级默认是1
                        if (!graph.HasVertex(depNode))
                        {
                            graph.AddVertex(depNode);
                        }
                        string arrNode = $"{adapter.Horizon + seg.ArrTime.Hour * 60 + seg.ArrTime.Minute}" +
                            $"_{seg.ArrStation.RailwayStationID}_{price}";//价格等级默认是1
                        if (!graph.HasVertex(arrNode))
                        {
                            graph.AddVertex(arrNode);
                        }
                        graph.AddEdge(depNode, arrNode,
                            (ctx.BasicPriceDic[seg] * price + (decimal)(seg.ArrTime - seg.DepTime).TotalMinutes * ctx.Vot));

                    }//Build In-train links in stop station
                    foreach (var stop in train.StopStaions.Skip(1).Take(train.StopStaions.Count() - 2))
                    {
                        string arrNode = $"{adapter.Horizon + stop.ArrTime.Hour * 60 + stop.ArrTime.Minute}" +
                            $"_{stop.Station.RailwayStationID}_{price}";//价格等级默认是1
                        string depNode = $"{adapter.Horizon + stop.DepTime.Hour * 60 + stop.DepTime.Minute}" +
                            $"_{stop.Station.RailwayStationID}_{price}";//价格等级默认是1
                        graph.AddEdge(arrNode, depNode,
                             ((decimal)(stop.DepTime - stop.ArrTime).TotalMinutes * ctx.Vot));
                    }
                }
            }

            //Build waiting links
            foreach (var ms in ctx.Wor.Mar as IEnumerable<IRailwayMarketSegment>)
            {
                for (int i = 0; i < adapter.Horizon; i++)
                {
                    if (!graph.HasVertex($"{i}_{ms.OriSta.RailwayStationID}_0"))
                    {
                        graph.AddVertex($"{i}_{ms.OriSta.RailwayStationID}_0");
                    }

                    if (!graph.HasVertex($"{i + 1}_{ms.OriSta.RailwayStationID}_0"))
                    {
                        graph.AddVertex($"{i + 1}_{ms.OriSta.RailwayStationID}_0");
                    }
                    graph.AddEdge($"{i}_{ms.OriSta.RailwayStationID}_0", $"{i + 1}_{ms.OriSta.RailwayStationID}_0",
                        adapter.Resolution * ctx.WaitingVot); // One minute each link
                }
            }

            //Build Reservation links
            foreach (var sta in ctx.Wor.Net.StationCollection)
            {
                //获取车次出发点
                var depSegs = ctx.Wor.RailwayTimeTable.Trains.SelectMany(i => i.ServiceSegments.Where(s => s.DepStation == sta));
                foreach (var seg in depSegs)
                {
                    foreach (var price in priceLevelList)
                    {
                        string depNode = $"{adapter.Horizon + seg.DepTime.Hour * 60 + seg.DepTime.Minute}_{seg.DepStation.RailwayStationID}_{price}";
                        for (int i = 0; i < adapter.Horizon; i++)
                        {
                            var edge = y.Keys.FirstOrDefault(link => link.Source == $"{i}_{sta.RailwayStationID.ToString()}_0"
                                                             && link.Destination == depNode);

                            if (edge != null && y[edge])
                                graph.AddEdge($"{i}_{sta.RailwayStationID.ToString()}_0", depNode, ASmallCost); // 一次预订花费一分钟
                        }
                    }
                }
            }

            //Build Transfer links
            foreach (var price in priceLevelList)
            {
                foreach (var train in ctx.Wor.RailwayTimeTable.Trains)
                {
                    foreach (var sta in train.StopStaions)
                    {
                        var depSegs = ctx.Wor.RailwayTimeTable.Trains.Where(t => t != train)
                            .SelectMany(i => i.ServiceSegments.Where(s => s.DepStation == sta.Station));
                        string arrNode = $"{adapter.Horizon + sta.ArrTime.Hour * 60 + sta.ArrTime.Minute}_{sta.Station.RailwayStationID}_{price}";//价格等级
                        foreach (var depseg in depSegs.Where(dep =>
                            (dep.DepTime - sta.ArrTime).TotalMinutes >= transferThreshold))
                        {
                            string depNode = $"{adapter.Horizon + depseg.DepTime.Hour * 60 + depseg.DepTime.Minute}_{depseg.DepStation.RailwayStationID}_{price}";//价格等级
                            graph.AddEdge(arrNode, depNode,
                               ((decimal)(depseg.DepTime - sta.ArrTime).TotalMinutes * ctx.Vot));
                        }
                    }
                }
            }

            //Build finishing links
            foreach (var price in priceLevelList)
            {
                foreach (var sta in ctx.Wor.Net.StationCollection)
                {
                    var arrSegs = ctx.Wor.RailwayTimeTable.Trains.SelectMany(i => i.ServiceSegments.Where(s => s.ArrStation == sta));
                    foreach (var seg in arrSegs)
                    {
                        string arrNode = $"{adapter.Horizon + seg.ArrTime.Hour * 60 + seg.ArrTime.Minute}_{seg.ArrStation.RailwayStationID}_{price}";//价格等级
                        string endNode = $"End_{sta.RailwayStationID.ToString()}_0";
                        if (!graph.HasVertex(endNode))
                        {
                            graph.AddVertex(endNode);
                        }
                        graph.AddEdge(arrNode, endNode, ASmallCost);
                    }
                }
            }

            //Build quit links
            /*
            foreach (var ms in ctx.Wor.Mar as IEnumerable<IRailwayMarketSegment>)
            {
                graph.AddEdge($"{adapter.Horizon}_{ms.OriSta.RailwayStationID}_0",
                    $"End_{ms.DesSta.RailwayStationID}_0", 999m);
            }
            */
            return graph;
        }

        //增加了拉格朗日乘子之后的LR-w网络
        public static DirectedWeightedSparseGraph<string> BuildLRwGraph(
            DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            ITrainTrip train,
            IRailwayStation station,
            Dictionary<CustomerArrival, List<TravelPath>> PathDict,
            Dictionary<IEdge<TravelHyperNode>, ITrainTrip> linkTrainDict,
            Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> LM_mu, //拉格朗日乘子 μ TODO:int 改为path ,
            Dictionary<IEdge<TravelHyperNode>, decimal> LM_lambda)
        {
            DirectedWeightedSparseGraph<string> graph = new DirectedWeightedSparseGraph<string>();

            //Build price_transfer links
            decimal[] priceLevelList = ctx.PriceLevelList.ToArray();
            int last = 0;
            int interval = ctx.ControlInterval / adapter.Resolution;
            if (interval >= adapter.Horizon) throw new Exception("控制频率应小于预售期");

            for (int level = 0; level < priceLevelList.Count(); level++)
            {
                for (int time = 0; time + interval < adapter.Horizon; time += interval)
                {
                    decimal cost_part1 = ctx.Pal.Sum(c => PathDict[c].Where(path => path.StartStation == station
                              && priceLevelList[level] == path.Price
                              && linkTrainDict[path.ReservationArc] == train
                              && time <= path.ReservationTime
                              && time + interval > path.ReservationTime).Sum(p => LM_mu[c][p]));

                    decimal cost_part2 = LM_lambda.Where(i => i.Key.Source.Station == station 
                              && i.Key.Destination.Price == priceLevelList[level]
                              && linkTrainDict[i.Key] == train
                              && time <= i.Key.Source.Time
                              && time + interval > i.Key.Source.Time).Sum(i => i.Value);


                    string selfnode = $"{priceLevelList[level]}_{time}";
                    //价格不变
                    string nextnode = $"{priceLevelList[level]}_{time + interval}";

                    if (!graph.HasVertex(selfnode))
                    {
                        graph.AddVertex(selfnode);
                    }
                    if (!graph.HasVertex(nextnode))
                    {
                        graph.AddVertex(nextnode);
                    }
                    if(!graph.AddEdge(selfnode, nextnode, cost_part1- cost_part2)) throw new Exception("存在相同的Edge");

                    //上升一段
                    if (level < priceLevelList.Count() - 1)
                    {
                        string ariseNode = $"{priceLevelList[level + 1]}_{time + interval}";
                        if (!graph.HasVertex(ariseNode))
                        {
                            graph.AddVertex(ariseNode);
                        }
                        if(!graph.AddEdge(selfnode, ariseNode, cost_part1 - cost_part2 + DpnAlgorithm.ASmallCost)) throw new Exception("存在相同的Edge"); 
                    }

                    //下降一段
                    if (level > 0)
                    {
                        string decreaseNode = $"{priceLevelList[level - 1]}_{time + interval}";//价格等级默认是1
                        if (!graph.HasVertex(decreaseNode))
                        {
                            graph.AddVertex(decreaseNode);
                        }
                        if(!graph.AddEdge(selfnode, decreaseNode, cost_part1 - cost_part2 - DpnAlgorithm.ASmallCost)) throw new Exception("存在相同的Edge");
                    }
                    last = time + interval;
                }
            }

            //Build dummy nodes and links.
            string dummystart = $"Start";//价格等级默认是1
            if (!graph.HasVertex(dummystart))
            {
                graph.AddVertex(dummystart);
            }
            for (int level = 0; level < priceLevelList.Count(); level++)
            {
                string selfnode = $"{priceLevelList[level]}_0";//价格等级默认是1
                if(!graph.AddEdge(dummystart, selfnode, (level+1) * ASmallCost)) throw new Exception("存在相同的Edge"); 
            }

            string dummyend = $"End";//价格等级默认是1
            if (!graph.HasVertex(dummyend))
            {
                graph.AddVertex(dummyend);
            }
            for (int level = 0; level < priceLevelList.Count(); level++)
            {
                string selfnode = $"{priceLevelList[level]}_{last}";//价格等级默认是1
                if(!graph.AddEdge(selfnode, dummyend, ASmallCost)) throw new Exception("存在相同的Edge"); 
            }

            var min = graph.Edges.Min(e => e.Weight);
            if (min <= 0)
            {
                foreach (var edge in graph.Edges)
                {
                    edge.Weight += - min + DpnAlgorithm.ASmallCost;
                }
            }
            return graph;
        }

       

        /// <summary>
        /// 输出每一个旅客的路径信息
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="adapter"></param>
        /// <param name="SegDic"></param>
        /// <param name="solution_x"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetTravelPathString(DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            BasicTravelHyperNetwork net,
            Dictionary<CustomerArrival, TravelPath> solution_x,
            Dictionary<CustomerArrival, decimal> PathCost)
        {
            //Print Head
            yield return "Customer ID, Description, Train Name, waiting Time (min), Waiting Time Cost, Travel Time Cost, Ticket Cost, Total Cost";
            //For each passenger, print information 
            foreach(var pair in solution_x)
            {
                var segments = pair.Value.GetSegments(net);
                if(segments!=null || segments.Count()>0)
                {
                    var waitingTimeCost = pair.Value.GetWaitTime() * adapter.Resolution * ctx.WaitingVot;
                    var travelTimeCost = (decimal)(segments.Max(i => i.ArrTime) - segments.Min(i => i.DepTime)).TotalMinutes * ctx.Vot;
                    yield return string.Join(",", pair.Key.QueueOrder,//旅客编号
                      ctx.Wor.Mar[pair.Key.Customer.MarSegID].Description,
                      net.GetTrainByReservationLink(pair.Value.ReservationArc).Name,
                      //string.Join(";", pair.Value.GetSegments(net).Select(i => $"{i.DepStation.StaName}->{i.ArrStation.StaName}")),//使用的服务路径
                      pair.Value.GetWaitTime() * adapter.Resolution,//等待时间
                      waitingTimeCost,//时间成本
                      travelTimeCost,
                      segments.Sum(seg => ctx.BasicPriceDic[seg] * pair.Value.Price),//票价成本
                      Math.Round(PathCost[pair.Key],2));//总成本
                }
                else
                {
                    //TODO:Print the information of a passenger who gives up travelling.

                }
            }
        }

        public static IEnumerable<string> GetPricingPathString(DPNProblemContext ctx,
            DiscreteTimeAdapter adapter,
            Dictionary<ITrainTrip, Dictionary<IRailwayStation, PricePath>> solution_w)
        {
            //Print Head
            yield return "Station ID, Station Name, Train No, Time, Price Level";
            //For each passenger, print information 
            foreach (var pair in solution_w)
            {
                foreach (var subpair in pair.Value)
                {
                    foreach (string str in subpair.Value.Nodepath)
                    {
                        string[] s = str.Split('_');
                        if (s[0] == "Start" || s[0] == "End")
                        {
                            continue;
                        }
                        else
                        {
                            int sT = Convert.ToInt32(s[1]);
                            yield return string.Join(",",
                                subpair.Key.RailwayStationID,
                                subpair.Key.StaName,
                                pair.Key.Name,
                                adapter.ConvertToDateTime(sT).ToString(),
                                s[0]);

                        }

                    }
                }
            }
        }
    }
}
