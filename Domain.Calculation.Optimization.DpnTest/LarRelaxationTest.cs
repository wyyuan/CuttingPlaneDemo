using Domain.Base.Demand.Reservation;
using Domain.Base.RNSM.Others;
using Domain.Base.Schedule.Timetable;
using Domain.Base.Service.Reservation;
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
    public class LarRelaxationTest
    {
        /*
         * 路网       A----------B----------C
         * 列车1      A---30m--->B---30m--->C 快车
         * 列车2      A---40m--->B---40m--->C 慢车 停 5 分
         * 价格等级   {80, 100, 120}
         * 旅客       A--------->B
         *            A-------------------->C
         *            A-------------------->C
         *            A-------------------->C
         *            
         * 预售期长度 11
         * 区间单价   100
         * 时间成本   0.1
         */
        private static IRnsmWorkspace GenerateWorkspace0()
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
                Name = "Train1",
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
                },
                Carriage = new Base.TranResource.TrainCarriage(2)
            });//Train 1
            wor.RailwayTimeTable.Add(new TrainTrip
            {
                Name = "Train2",
                StopStaions = new List<StopStation>()
                {
                    new StopStation()
                    {
                        Station = sta_A,
                        DepTime = new DateTime(1991, 7, 5, 8, 15, 0)
                    },
                    new StopStation()
                    {
                        Station = sta_B,
                        ArrTime = new DateTime(1991, 7, 5, 8, 55, 0),
                        DepTime = new DateTime(1991, 7, 5, 9, 0, 0)
                    },
                    new StopStation()
                    {
                        Station = sta_C,
                        ArrTime = new DateTime(1991, 7, 5, 9, 40, 0)
                    },
                },
                ServiceSegments = new List<ServiceSegment>
                {
                    new ServiceSegment()
                    {
                        DepStation = sta_A,
                        DepTime = new DateTime(1991, 7, 5, 8, 15, 0),
                        ArrStation = sta_B,
                        ArrTime = new DateTime(1991, 7, 5, 8, 55, 0)
                    },
                    new ServiceSegment()
                    {
                        DepStation = sta_B,
                        DepTime = new DateTime(1991, 7, 5, 9, 0, 0),
                        ArrStation = sta_C,
                        ArrTime = new DateTime(1991, 7, 5, 9, 40, 0),
                    }
                },
                Carriage = new Base.TranResource.TrainCarriage(2)
            });//Train 2
            #endregion

            #region Generate demand
            wor.Mar = new RailwayMarket()
            {
                AggRo = new List<Interval>()
                {
                    new Interval()
                    {
                        StartDate = new DateTime(1991,7,5,0,0,0),
                        StartTime = 0,
                        EndDate = new DateTime(1991,7,5,0,10,10),
                        EndTime = 10,
                    }
                },
                TimeHorizon = 10
            };
            wor.Mar.ConvertToInttime = x =>
            {
                for (int i = 0; i < wor.Mar.AggRo.Count; i++)
                {
                    if (x >= wor.Mar.AggRo[i].StartDate && x <= wor.Mar.AggRo[i].EndDate)
                    {
                        return wor.Mar.AggRo[i].StartTime + (int)((wor.Mar.AggRo[i].EndTime - wor.Mar.AggRo[i].StartTime)
                            * (x - wor.Mar.AggRo[i].StartDate).TotalSeconds
                            / (wor.Mar.AggRo[i].EndDate - wor.Mar.AggRo[i].StartDate).TotalSeconds);
                    };
                }
                throw new Exception("时间超出范围");
            }; //将刻度时间转化为真实时间
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
        private static DPNProblemContext GenerateProblemContext0()
        {
            IRnsmWorkspace wor = GenerateWorkspace0();
            DPNProblemContext _ctx = new DPNProblemContext(wor)
            {
                TransferThreshold = 60,
                Vot = 0.1m,
                ControlInterval = 1,
                WaitingVot = 0.1m,
                PriceLevelList = new List<decimal> { 0.8m, 1, 1.2m },
                BasicPriceDic = new Dictionary<IServiceSegment, decimal>()
                {
                    { wor.RailwayTimeTable.Trains.First().ServiceSegments.First(),100m},
                    { wor.RailwayTimeTable.Trains.First().ServiceSegments.Last(),100m},
                    { wor.RailwayTimeTable.Trains.Last().ServiceSegments.First(),100m},
                    { wor.RailwayTimeTable.Trains.Last().ServiceSegments.Last(),100m}
                },
                Pal = new CustomerArrivalChain()
                {
                    new CustomerArrival()
                    {
                        QueueOrder = 1,
                        ArriveTime = new DateTime(1991,7,5,0,0,2),
                        Customer = new CustomerInfo(){ MarSegID = 2 }
                    },                  
                    new CustomerArrival()
                    {
                        QueueOrder = 2,
                        ArriveTime = new DateTime(1991,7,5,0,3,0),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 3,
                        ArriveTime = new DateTime(1991,7,5,0,6,0),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 4,
                        ArriveTime = new DateTime(1991,7,5,0,9,0),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                },
                StartTime = new DateTime(1991, 7, 5, 0, 0, 0),
                EndTime = new DateTime(1991, 7, 5, 0, 10, 0),
                SitaDic = new Dictionary<int, decimal>()
            };

            foreach (var marketseg in _ctx.Wor.Mar as IEnumerable<IRailwayMarketSegment>)
            {
                _ctx.SitaDic.Add(marketseg.MSID, 100m);
            }
            _ctx.SetParameter("TerminalFactor", 0.0001);
            _ctx.SetParameter("Iteration", 50);
            _ctx.SetParameter("Resolution", 1);
            _ctx.SetParameter("InitMultiper", 0.1m);
           
            return _ctx;
        }
        [TestMethod]
        public void Case0_MinTotal()
        {
            DPNProblemContext ctx = GenerateProblemContext0();
            ctx.SetParameter("ObjectiveType", "");
            DpnSolver solver = new DpnSolver(ctx);
            solver.Logger = Console.Out;
            Console.WriteLine($"旅客到达信息：");
            foreach (CustomerArrival arr in ctx.Pal)
            {
                Console.WriteLine($"旅客{arr.QueueOrder}:{arr.ArriveTime.ToLongTimeString()}(" +
                    $"{ctx.Wor.Mar.ConvertToInttime(arr.ArriveTime)}),市场:{arr.Customer.MarSegID}");
            }

            solver.OnFeasibleSolutionGenerated = (a,b)=>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
                foreach (string str in b)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnIterationFinished = (s) =>
            {

            };

            Console.WriteLine($"计算开始：");

            solver.Work();

            Assert.AreEqual(6.5m+6.5m+4m+8.5m,Math.Round(solver.ObjValue,1));
        }
        [TestMethod]
        public void Case0_MinTravelTime()
        {
            DPNProblemContext ctx = GenerateProblemContext0();
            ctx.SetParameter("ObjectiveType", "MinTravelTimeCost");
            DpnSolverV4 solver = new DpnSolverV4(ctx);
            solver.Logger = Console.Out;
            Console.WriteLine($"旅客到达信息：");
            foreach (CustomerArrival arr in ctx.Pal)
            {
                Console.WriteLine($"旅客{arr.QueueOrder}:{arr.ArriveTime.ToLongTimeString()}(" +
                    $"{ctx.Wor.Mar.ConvertToInttime(arr.ArriveTime)}),市场:{arr.Customer.MarSegID}");
            }
            solver.OnLowerBoundSolutionGenerated = (a) =>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnFeasibleSolutionGenerated = (a, b) =>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
                foreach (string str in b)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnIterationFinished = (s) =>
            {
                Console.WriteLine(s);
            };

            Console.WriteLine($"计算开始：");

            solver.Work();

            Assert.AreEqual(6.5m + 6.5m + 4m + 8.5m, Math.Round(solver.ObjValue, 1));
        }
        [TestMethod]
        public void Case0_MaxRev()
        {
            DPNProblemContext ctx = GenerateProblemContext0();
            ctx.SetParameter("ObjectiveType", "MaxRevenue");
            DpnSolver solver = new DpnSolver(ctx);
            solver.Logger = Console.Out;
            Console.WriteLine($"旅客到达信息：");
            foreach (CustomerArrival arr in ctx.Pal)
            {
                Console.WriteLine($"旅客{arr.QueueOrder}:{arr.ArriveTime.ToLongTimeString()}(" +
                    $"{ctx.Wor.Mar.ConvertToInttime(arr.ArriveTime)}),市场:{arr.Customer.MarSegID}");
            }

            solver.OnFeasibleSolutionGenerated = (a, b) =>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
                foreach (string str in b)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnIterationFinished = (s) =>
            {

            };

            Console.WriteLine($"计算开始：");

            solver.Work();

            Assert.AreEqual(-120 - 240m * 3, Math.Round(solver.ObjValue, 1));
        }

        private static IRnsmWorkspace GenerateWorkspace1()
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
                Name = "Train1",
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
                },
                Carriage = new Base.TranResource.TrainCarriage(5)
            });//Train 1
            wor.RailwayTimeTable.Add(new TrainTrip
            {
                Name = "Train2",
                StopStaions = new List<StopStation>()
                {
                    new StopStation()
                    {
                        Station = sta_A,
                        DepTime = new DateTime(1991, 7, 5, 8, 15, 0)
                    },
                    new StopStation()
                    {
                        Station = sta_B,
                        ArrTime = new DateTime(1991, 7, 5, 8, 55, 0),
                        DepTime = new DateTime(1991, 7, 5, 9, 0, 0)
                    },
                    new StopStation()
                    {
                        Station = sta_C,
                        ArrTime = new DateTime(1991, 7, 5, 9, 40, 0)
                    },
                },
                ServiceSegments = new List<ServiceSegment>
                {
                    new ServiceSegment()
                    {
                        DepStation = sta_A,
                        DepTime = new DateTime(1991, 7, 5, 8, 15, 0),
                        ArrStation = sta_B,
                        ArrTime = new DateTime(1991, 7, 5, 8, 55, 0)
                    },
                    new ServiceSegment()
                    {
                        DepStation = sta_B,
                        DepTime = new DateTime(1991, 7, 5, 9, 0, 0),
                        ArrStation = sta_C,
                        ArrTime = new DateTime(1991, 7, 5, 9, 40, 0),
                    }
                },
                Carriage = new Base.TranResource.TrainCarriage(5)
            });//Train 2
            #endregion

            #region Generate demand
            wor.Mar = new RailwayMarket()
            {
                AggRo = new List<Interval>()
                {
                    new Interval()
                    {
                        StartDate = new DateTime(1991,7,5,0,0,0),
                        StartTime = 0,
                        EndDate = new DateTime(1991,7,5,0,10,10),
                        EndTime = 10,
                    }
                },
                TimeHorizon = 10
            };
            wor.Mar.ConvertToInttime = x =>
            {
                for (int i = 0; i < wor.Mar.AggRo.Count; i++)
                {
                    if (x >= wor.Mar.AggRo[i].StartDate && x <= wor.Mar.AggRo[i].EndDate)
                    {
                        return wor.Mar.AggRo[i].StartTime + (int)((wor.Mar.AggRo[i].EndTime - wor.Mar.AggRo[i].StartTime)
                            * (x - wor.Mar.AggRo[i].StartDate).TotalSeconds
                            / (wor.Mar.AggRo[i].EndDate - wor.Mar.AggRo[i].StartDate).TotalSeconds);
                    };
                }
                throw new Exception("时间超出范围");
            }; //将刻度时间转化为真实时间
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
        private static DPNProblemContext GenerateProblemContext1()
        {
            IRnsmWorkspace wor = GenerateWorkspace1();
            DPNProblemContext _ctx = new DPNProblemContext(wor)
            {
                TransferThreshold = 60,
                Vot = 0.1m,
                ControlInterval = 4,
                PriceLevelList = new List<decimal> { 0.8m, 1, 1.2m },
                BasicPriceDic = new Dictionary<IServiceSegment, decimal>()
                {
                    { wor.RailwayTimeTable.Trains.First().ServiceSegments.First(),100m},
                    { wor.RailwayTimeTable.Trains.First().ServiceSegments.Last(),100m},
                    { wor.RailwayTimeTable.Trains.Last().ServiceSegments.First(),100m},
                    { wor.RailwayTimeTable.Trains.Last().ServiceSegments.Last(),100m}
                },
                Pal = new CustomerArrivalChain()
                {
                    new CustomerArrival()
                    {
                        QueueOrder = 1,
                        ArriveTime = new DateTime(1991,7,5,0,0,1),
                        Customer = new CustomerInfo(){ MarSegID = 2 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 2,
                        ArriveTime = new DateTime(1991,7,5,0,1,0),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 3,
                        ArriveTime = new DateTime(1991,7,5,0,2,0),
                        Customer = new CustomerInfo(){ MarSegID = 2 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 4,
                        ArriveTime = new DateTime(1991,7,5,0,2,1),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 5,
                        ArriveTime = new DateTime(1991,7,5,0,2,2),
                        Customer = new CustomerInfo(){ MarSegID = 2 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 6,
                        ArriveTime = new DateTime(1991,7,5,0,2,3),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 7,
                        ArriveTime = new DateTime(1991,7,5,0,2,4),
                        Customer = new CustomerInfo(){ MarSegID = 2 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 8,
                        ArriveTime = new DateTime(1991,7,5,0,2,5),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 9,
                        ArriveTime = new DateTime(1991,7,5,0,5,1),
                        Customer = new CustomerInfo(){ MarSegID = 1 }
                    },
                    new CustomerArrival()
                    {
                        QueueOrder = 10,
                        ArriveTime = new DateTime(1991,7,5,0,9,7),
                        Customer = new CustomerInfo(){ MarSegID = 3 }
                    },
                },
                StartTime = new DateTime(1991, 7, 5, 0, 0, 0),
                EndTime = new DateTime(1991, 7, 5, 0, 10, 0),
                SitaDic = new Dictionary<int, decimal>()
            };

            foreach (var marketseg in _ctx.Wor.Mar as IEnumerable<IRailwayMarketSegment>)
            {
                _ctx.SitaDic.Add(marketseg.MSID, 50m);
            }
            _ctx.SetParameter("TerminalFactor", 0.0001);
            _ctx.SetParameter("Iteration", 50);
            _ctx.SetParameter("Resolution", 1);
            _ctx.SetParameter("WaitingCost", 0.1m);
            _ctx.SetParameter("InitMultiper", 0.1m);

            return _ctx;
        }
        [TestMethod]
        public void Case1()
        {
            DPNProblemContext ctx = GenerateProblemContext1();
            ctx.SetParameter("ObjectiveType", "MinTravelTimeCost");
            DpnSolverV4 solver = new DpnSolverV4(ctx);
            solver.Logger = Console.Out;
            Console.WriteLine($"旅客到达信息：");
            foreach (CustomerArrival arr in ctx.Pal)
            {
                Console.WriteLine($"旅客{arr.QueueOrder}:{arr.ArriveTime.ToLongTimeString()}(" +
                    $"{ctx.Wor.Mar.ConvertToInttime(arr.ArriveTime)}),市场:{arr.Customer.MarSegID}");
            }

            solver.OnFeasibleSolutionGenerated = (a, b) =>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
                foreach (string str in b)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnIterationFinished = (s) =>
            {
                Console.WriteLine(s);
            };

            Console.WriteLine($"计算开始：");

            solver.Work();

            Assert.IsTrue(true);
        }
        [TestMethod]
        public void Case1_Mix1()//2:8
        {
            DPNProblemContext ctx = GenerateProblemContext1();
            DpnSolver solver = new DpnSolver(ctx);
            ctx.SetParameter("ObjectiveType", "MinTotalCost_MaxRevenue_Mix");
            ctx.SetParameter("MinTotalCost_weight", 0.2m);
            ctx.SetParameter("MaxRevenue_weight", 0.8m);
            solver.Logger = Console.Out;
            Console.WriteLine($"旅客到达信息：");
            foreach (CustomerArrival arr in ctx.Pal)
            {
                Console.WriteLine($"旅客{arr.QueueOrder}:{arr.ArriveTime.ToLongTimeString()}(" +
                    $"{ctx.Wor.Mar.ConvertToInttime(arr.ArriveTime)}),市场:{arr.Customer.MarSegID}");
            }

            solver.OnFeasibleSolutionGenerated = (a, b) =>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
                foreach (string str in b)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnIterationFinished = (s) =>
            {

            };

            Console.WriteLine($"计算开始：");

            solver.Work();

            Assert.IsTrue(true);
        }
        [TestMethod]
        public void Case1_Mix2()//5:5
        {
            DPNProblemContext ctx = GenerateProblemContext1();
            DpnSolver solver = new DpnSolver(ctx);
            ctx.SetParameter("ObjectiveType", "MinTotalCost_MaxRevenue_Mix");
            ctx.SetParameter("MinTotalCost_weight", 0.5m);
            ctx.SetParameter("MaxRevenue_weight", 0.5m);
            solver.Logger = Console.Out;
            Console.WriteLine($"旅客到达信息：");
            foreach (CustomerArrival arr in ctx.Pal)
            {
                Console.WriteLine($"旅客{arr.QueueOrder}:{arr.ArriveTime.ToLongTimeString()}(" +
                    $"{ctx.Wor.Mar.ConvertToInttime(arr.ArriveTime)}),市场:{arr.Customer.MarSegID}");
            }

            solver.OnFeasibleSolutionGenerated = (a, b) =>
            {
                foreach (string str in a)
                {
                    Console.WriteLine(str);
                }
                foreach (string str in b)
                {
                    Console.WriteLine(str);
                }
            };
            solver.OnIterationFinished = (s) =>
            {

            };

            Console.WriteLine($"计算开始：");

            solver.Work();

            Assert.IsTrue(true);
        }
    }
}
