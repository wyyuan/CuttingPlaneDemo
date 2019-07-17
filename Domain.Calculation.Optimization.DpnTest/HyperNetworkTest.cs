using Algorithms.Graphs;
using DataStructures.Graphs;
using Domain.Base.Demand.Reservation;
using Domain.Base.Schedule.Timetable;
using Domain.Impl.Context;
using Domain.Impl.Demand.Reservation;
using Domain.Impl.Network.RailwayBasicNetwork;
using Domain.Impl.Schedule.Timetable;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN.Test
{
    [TestClass]
    public class HyperNetworkTest
    {
        /*
         * 路网       A----------B----------C
         * 列车1      A---30m--->B---30m--->C
         * 列车2                 B---40m--->C
         * 价格等级   {0.9, 1, 1.1}
         * 旅客       A--------->B
         *            A-------------------->C
         *                       B--------->C
         * 预售期长度 11
         * 区间单价   20
         * 时间成本   1
         */
        private static RnsmWorkspace GenerateWorkspace()
        {
            //Build a RNSM workspace
            RnsmWorkspace wor = new RnsmWorkspace();

            #region Build railway netowrk
            wor.Net = new RailwayNetwork();
            //Build stations
            wor.Net.RailwayStations = new List<RailwayStation>();
            RailwayStation sta_A = new RailwayStation() { RailwayStationID = 1, StaName = "A" };
            RailwayStation sta_B = new RailwayStation() { RailwayStationID = 2, StaName = "B" };
            RailwayStation sta_C = new RailwayStation() { RailwayStationID = 3, StaName = "C" };
            wor.Net.RailwayStations.Add(sta_A);
            wor.Net.RailwayStations.Add(sta_B);
            wor.Net.RailwayStations.Add(sta_C);
            //Build sections
            wor.Net.RailwaySections = new List<RailwaySection>();
            wor.Net.RailwaySections.Add(new RailwaySection()
            {
                StartStation = sta_A,
                EndStation = sta_B,
                RailwaySectionID = 1
            });
            wor.Net.RailwaySections.Add(new RailwaySection()
            {
                StartStation = sta_B,
                EndStation = sta_C,
                RailwaySectionID = 2
            });
            #endregion

            #region Generate a timetable
            wor.RailwayTimeTable = new TrainDiagram();
            wor.RailwayTimeTable.Add(new TrainTrip
            {
                Name = "Train01",
                StopStaions = new List<StopStation>
                {
                    new StopStation()
                    {
                        Station = sta_A,
                        DepTime = new DateTime(1991, 7, 5, 8, 0, 0)
                    },
                    new StopStation()
                    {
                        Station = sta_B,
                        ArrTime = new DateTime(1991, 7, 5, 8, 30, 0),
                        DepTime = new DateTime(1991, 7, 5, 8, 35, 0)
                    },
                    new StopStation()
                    {
                        Station = sta_C,
                        ArrTime = new DateTime(1991, 7, 5, 9, 5, 0)
                    },

                },
                ServiceSegments = new List<ServiceSegment>
                {
                    new ServiceSegment()
                    {
                        DepStation = sta_A,
                        DepTime = new DateTime(1991, 7, 5, 8, 0, 0),
                        ArrStation = sta_B,
                        ArrTime = new DateTime(1991, 7, 5, 8, 30, 0)
                    },
                    new ServiceSegment()
                    {
                        DepStation = sta_B,
                        DepTime = new DateTime(1991, 7, 5, 8, 35, 0),
                        ArrStation = sta_C,
                        ArrTime = new DateTime(1991, 7, 5, 9, 5, 0)
                    }
                }
            });//Train 1
            wor.RailwayTimeTable.Add(new TrainTrip
            {
                Name = "Train02",
                StopStaions = new List<StopStation>(),
                ServiceSegments = new List<ServiceSegment>
                    {
                        new ServiceSegment()
                        {
                            DepStation = sta_B,
                            DepTime = new DateTime(1991, 7, 5, 9, 0, 0),
                            ArrStation = sta_C,
                            ArrTime = new DateTime(1991, 7, 5, 9, 40, 0),
                        }
                    }
            });//Train 2
            #endregion

            #region Generate demand
            wor.Mar = new RailwayMarket();
            wor.Mar.TimeHorizon = 10;
            (wor.Mar as List<RailwayMarketSegment>).AddRange(new List<RailwayMarketSegment>()
            {
                new RailwayMarketSegment
                 {
                     MSID = 1,
                     Description = "A->B",
                     OriSta = sta_A,
                     DesSta = sta_B
                 },
                new RailwayMarketSegment
                {
                    MSID = 2,
                    Description = "B->C",
                    OriSta = sta_B,
                    DesSta = sta_C
                },
                 new RailwayMarketSegment
                 {
                     MSID = 3,
                     Description = "A->C",
                     OriSta = sta_A,
                     DesSta = sta_C,
                 }
            });
            #endregion

            return wor;
        }
        private static DPNProblemContext GenerateProblemContext()
        {
            RnsmWorkspace wor = GenerateWorkspace();
            DPNProblemContext _ctx = new DPNProblemContext(wor)
            {
                TransferThreshold = 3,
                Vot = 1,
                ControlInterval = 3,
                PriceLevelList = new List<decimal> { 0.9m, 1, 1.1m },
                BasicPriceDic = new Dictionary<IServiceSegment, decimal>()
                {
                    { wor.RailwayTimeTable.TrainTrips.First().ServiceSegments.First(),20m},
                    { wor.RailwayTimeTable.TrainTrips.First().ServiceSegments.Last(),20m},
                    { wor.RailwayTimeTable.TrainTrips.Last().ServiceSegments.First(),20m}
                },
                Pal = new CustomerArrivalChain()
                {
                    new CustomerArrival()
                    {
                        ArriveTime =  new DateTime(1991, 7, 5, 0, 0, 1),
                        QueueOrder = 1,
                        Customer = new CustomerInfo()
                        {
                            MarSegID = 3
                        }
                    },
                    new CustomerArrival()
                    {
                        ArriveTime = new DateTime(1991, 7, 5, 0, 2, 0),
                        QueueOrder = 2,
                        Customer = new CustomerInfo()
                        {
                            MarSegID = 3
                        }
                    },
                },
                StartTime = new DateTime(1991, 7, 5, 0, 0, 0),
                EndTime = new DateTime(1991, 7, 5, 0, 10, 0),
                SitaDic = new Dictionary<int, decimal>()
            };
            _ctx.SetParameter("BigM", 9999m);
            _ctx.SetParameter("Resolution", 1);
            _ctx.SetParameter("WaitingCost", 0.1);
            return _ctx;
        }

        /// <summary>
        /// 保证构造的网络弧数量正确
        /// </summary>
        [TestMethod]
        public void BuildBasicNetTest()
        {
            DPNProblemContext ctx = GenerateProblemContext();
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(ctx.StartTime, ctx.EndTime, 1);
            var graph = new BasicTravelHyperNetwork(ctx, adapter);
            graph.Build();
            Assert.AreEqual(138, graph.EdgesCount);
        }

        /// <summary>
        /// 保证构造的网络弧数量正确
        /// </summary>
        [TestMethod]
        public void BuildLRxTest()
        {
            DPNProblemContext ctx = GenerateProblemContext();
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(ctx.StartTime, ctx.EndTime, 1);
            var graph = new LRxTravelHyperNetwork(ctx, adapter,
            ObjectNetworkFactory.Create("", ctx, adapter),
            new CustomerArrival(),
            new Dictionary<CustomerArrival, List<TravelPath>>(),
            new Dictionary<IServiceSegment, decimal>()
            {
                { ctx.Wor.RailwayTimeTable.Trains.First().ServiceSegments.First(),0},
                { ctx.Wor.RailwayTimeTable.Trains.First().ServiceSegments.Last(),0},
                { ctx.Wor.RailwayTimeTable.Trains.Last().ServiceSegments.First(),0}
            },
            new Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>>(),
            new Dictionary<IEdge<TravelHyperNode>, decimal>());
            graph.Build();
            /*
             * In-train:4*3
             * Finish:3*3
             * Reservation:11*3*(2+1)
             * Waiting:10*3
             * Total:137
             */

            Assert.AreEqual(138, graph.EdgesCount);
        }

        /// <summary>
        /// 构建LRw网络测试
        /// </summary>
        [TestMethod]
        public void BuildLRwTest()
        {
            DPNProblemContext ctx = GenerateProblemContext();
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(ctx.StartTime, ctx.EndTime, 1);
            var travelgraph = new BasicTravelHyperNetwork(ctx, adapter);
            travelgraph.Build();

            var train = ctx.Wor.RailwayTimeTable.Trains.First();
            var station = ctx.Wor.Net.StationCollection.First();

            //路径集
            Dictionary<CustomerArrival, List<TravelPath>> pathDict
                = new Dictionary<CustomerArrival, List<TravelPath>>();

            foreach (CustomerArrival c in ctx.Pal)
            {
                var ori = (ctx.Wor.Mar[c.Customer.MarSegID] as IRailwayMarketSegment).OriSta;
                var des = (ctx.Wor.Mar[c.Customer.MarSegID] as IRailwayMarketSegment).DesSta;
                var paths = DepthFirstSearcher.FindAllPaths(travelgraph,
                    new TravelHyperNode() { Time = adapter.ConvertToDiscreteTime(c.ArriveTime), Station = ori, Price = 0 },
                    new TravelHyperNode() { Time = adapter.Horizon + 1440, Station = des, Price = 0 });
                pathDict.Add(c, new List<TravelPath>());
                foreach (var path in paths)
                {
                    pathDict[c].Add(new TravelPath(travelgraph, path));
                }
            }

            //拉格朗日乘子 mu
            Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> LM_mu
                = new Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>>();
            foreach (CustomerArrival customer in ctx.Pal)
            {
                LM_mu.Add(customer, new Dictionary<TravelPath, decimal>());
                foreach (var path in pathDict[customer])
                {
                    LM_mu[customer].Add(path, 2);
                }
            }

            // 拉格朗日乘子 lambda
            Dictionary<IEdge<TravelHyperNode>, decimal> LM_lambda = new Dictionary<IEdge<TravelHyperNode>, decimal>();
            //WARNING: 这里缺少了没有旅客选择的reservation arc
            foreach (CustomerArrival customer in ctx.Pal)
            {
                foreach (var path in pathDict[customer])
                {
                    if (!LM_lambda.ContainsKey(path.ReservationArc))
                    {
                        LM_lambda.Add(path.ReservationArc, 1);
                    }
                }
            }

            var graph = DpnAlgorithm.BuildLRwGraph(ctx, adapter, train, station,          
            pathDict,
            travelgraph.LinkTrainDict,
            LM_mu,
            LM_lambda);
 
            Assert.AreEqual(27, graph.EdgesCount);
        }

        /// <summary>
        /// 搜索从A到C的一条最短路
        /// </summary>
        [TestMethod]
        public void LRxPathSearchTest()
        {
            DPNProblemContext ctx = GenerateProblemContext();
            CustomerArrival customer = new CustomerArrival();
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(ctx.StartTime, ctx.EndTime, 1);

            TravelPath p = new TravelPath();
            var graph = new LRxTravelHyperNetwork(ctx, adapter,
            ObjectNetworkFactory.Create("", ctx, adapter),
            customer,
            new Dictionary<CustomerArrival, List<TravelPath>>()//path dict
            {
                { customer, new List<TravelPath>(){ p } }
            },
           new Dictionary<IServiceSegment, decimal>()//rho
            {
                { ctx.Wor.RailwayTimeTable.Trains.First().ServiceSegments.First(),0},
                { ctx.Wor.RailwayTimeTable.Trains.First().ServiceSegments.Last(),0},
                { ctx.Wor.RailwayTimeTable.Trains.Last().ServiceSegments.First(),0}
            },
            new Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>>()
            {
                { customer, new Dictionary<TravelPath, decimal>(){ { p,1 } } }
            },//mu
            new Dictionary<IEdge<TravelHyperNode>, decimal>());
            graph.Build();

            DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode> dijkstra
                = new DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode>(graph,
                new TravelHyperNode() { Time = 0, Station = ctx.Wor.Net.StationCollection.First(), Price = 0 });

            Assert.IsTrue(dijkstra.HasPathTo(new TravelHyperNode() { Time = adapter.Horizon + 1440, Station = ctx.Wor.Net.StationCollection.Last(), Price = 0 }) == true);

            var desNode = new TravelHyperNode()
            { Time = adapter.Horizon + 1440, Station = ctx.Wor.Net.StationCollection.Last(), Price = 0 };
            var pathToC = string.Empty;
            var shortestPath = dijkstra.ShortestPathTo(desNode);

            foreach (var node in shortestPath)
            {
                pathToC = String.Format("{0}({1}) -> ", pathToC, node);
            }

            pathToC = pathToC.TrimEnd(new char[] { ' ', '-', '>' });
            Console.WriteLine("Shortest path to Station 'C': " + pathToC + "\r\n");
            Assert.AreEqual(202m, Math.Round(dijkstra.DistanceTo(desNode)));
        }

        [TestMethod]
        public void LRwPathSearchTest()
        {
            DPNProblemContext ctx = GenerateProblemContext();
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(ctx.StartTime, ctx.EndTime, 1);
            var travelgraph = new BasicTravelHyperNetwork(ctx, adapter);
            travelgraph.Build();

            /* Train01 Station A*/
            var train = ctx.Wor.RailwayTimeTable.Trains.First();
            var station = ctx.Wor.Net.StationCollection.First();

            //路径集
            Dictionary<CustomerArrival, List<TravelPath>> pathDict
                = new Dictionary<CustomerArrival, List<TravelPath>>();

            foreach (CustomerArrival c in ctx.Pal)
            {
                var ori = (ctx.Wor.Mar[c.Customer.MarSegID] as IRailwayMarketSegment).OriSta;
                var des = (ctx.Wor.Mar[c.Customer.MarSegID] as IRailwayMarketSegment).DesSta;
                var paths = DepthFirstSearcher.FindAllPaths(travelgraph,
                    new TravelHyperNode() { Time = adapter.ConvertToDiscreteTime(c.ArriveTime), Station = ori, Price = 0 },
                    new TravelHyperNode() { Time = adapter.Horizon + 1440, Station = des, Price = 0 });
                pathDict.Add(c, new List<TravelPath>());
                foreach (var path in paths)
                {
                    pathDict[c].Add(new TravelPath(travelgraph, path));
                }

            }

            //拉格朗日乘子 mu
            Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> LM_mu
                = new Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>>();
            foreach (CustomerArrival customer in ctx.Pal)
            {
                LM_mu.Add(customer, new Dictionary<TravelPath, decimal>());
                foreach (var path in pathDict[customer])
                {
                    LM_mu[customer].Add(path, 2);
                }
            }

            // 拉格朗日乘子 lambda
            Dictionary<IEdge<TravelHyperNode>, decimal> LM_lambda = new Dictionary<IEdge<TravelHyperNode>, decimal>();
            foreach (CustomerArrival customer in ctx.Pal)
            {
                foreach (var path in pathDict[customer])
                {
                    if (!LM_lambda.ContainsKey(path.ReservationArc))
                    {
                        LM_lambda.Add(path.ReservationArc, 1);
                    }
                }
            }

            var graph = DpnAlgorithm.BuildLRwGraph(ctx, adapter, train, station,
            pathDict,
            travelgraph.LinkTrainDict,
            LM_mu,
            LM_lambda);

            DijkstraShortestPaths<DirectedWeightedSparseGraph<string>, string> dijkstra
                = new DijkstraShortestPaths<DirectedWeightedSparseGraph<string>, string>(graph,
                "Start");

            Assert.IsTrue(dijkstra.HasPathTo("End") == true);

            var pathToC = string.Empty;
            var pricepath = dijkstra.ShortestPathTo("End");
            PricePath p = new PricePath(graph, pricepath);

            Assert.IsTrue(p.GetWrapPoints(0.9m, 2).Count() > 0);
            Assert.IsFalse(p.GetWrapPoints(1.1m, 2).Count() > 0);

            foreach (var node in pricepath)
            {
                pathToC = String.Format("{0}({1}) -> ", pathToC, node);
            }

            pathToC = pathToC.TrimEnd(new char[] { ' ', '-', '>' });
            Console.WriteLine("Shortest path to Station 'C': " + pathToC + "\r\n");
            Assert.AreEqual(23m, Math.Round(dijkstra.DistanceTo("End")));
        }

        /// <summary>
        /// 搜索从A到C的所有路径
        /// </summary>
        [TestMethod]
        public void BasicSearchAllTest()
        {
            DPNProblemContext ctx = GenerateProblemContext();
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(ctx.StartTime, ctx.EndTime, 1);
            var graph = new BasicTravelHyperNetwork(ctx, adapter);
            graph.Build();

            var list = DepthFirstSearcher.FindAllPaths(graph,
                new TravelHyperNode() { Time = 0, Station = ctx.Wor.Net.StationCollection.First(), Price = 0 },
                new TravelHyperNode() { Time = adapter.Horizon + 1440, Station = ctx.Wor.Net.StationCollection.Last(), Price = 0 });
            foreach (var path in list)
            {
                Console.WriteLine(string.Join(",", path));
            }
            Assert.AreEqual(30, list.Count);
            //路径集
            TravelPath p = new TravelPath(graph, list[1]);
            Assert.AreEqual(ctx.Wor.Net.StationCollection.First(), p.StartStation);
        }
    }
}
