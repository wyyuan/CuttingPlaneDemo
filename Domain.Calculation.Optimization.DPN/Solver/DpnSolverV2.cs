using Algorithms.Graphs;
using DataStructures.Graphs;
using Domain.Base.Demand.Reservation;
using Domain.Base.Network.RailwayBasicNetwork;
using Domain.Base.Schedule.Timetable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Domain.Calculation.Optimization.DPN
{


    [DisplayName("DPNv2")]
    public class DpnSolverV2 : AbsDpnSolver
    {
        public DpnSolverV2(DPNProblemContext ctx) : base(ctx) { }

        public decimal ObjValue;

        public override void Work()
        {
            //获取求解设置
            decimal terminalFactor = _ctx.GetParameter<decimal>("TerminalFactor", 0.001m);
            int iteration = _ctx.GetParameter<int>("Iteration", 10);
            int resolution = _ctx.GetParameter<int>("Resolution", 60);
            decimal initMultipler = _ctx.GetParameter<decimal>("InitMultiper", 0.1m);
            string objectiveType = _ctx.GetParameter("ObjectiveType", "");

            //相对-绝对时间转化器
            DiscreteTimeAdapter adapter = new DiscreteTimeAdapter(_ctx.StartTime, _ctx.EndTime, resolution);

            //路径集
            Dictionary<CustomerArrival, List<TravelPath>> pathDict
                = new Dictionary<CustomerArrival, List<TravelPath>>();

            #region  建立初始网络，搜索可行路径
            //目标函数网络
            var objgraph = ObjectNetworkFactory.Create(objectiveType, _ctx, adapter); //new ObjectTravelHyperNetwork(_ctx, adapter);
            objgraph.Build();

            //基础网络
            var basicGraph = new BasicTravelHyperNetwork(_ctx, adapter);
            basicGraph.Build();

            SubTasks.Clear();
            foreach (CustomerArrival customer in _ctx.Pal)
            {
                Task ta = factory.StartNew(() =>
                {
                    var ori = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).OriSta;
                    var des = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).DesSta;
                    var paths = DepthFirstSearcher.FindAllPaths(basicGraph,
                        new TravelHyperNode() { Time = adapter.ConvertToDiscreteTime(customer.ArriveTime), Station = ori, Price = 0 },
                        new TravelHyperNode() { Time = adapter.Horizon + 1440, Station = des, Price = 0 } );
                    pathDict.Add(customer, new List<TravelPath>());
                    foreach (var path in paths)
                    {
                        pathDict[customer].Add(new TravelPath(basicGraph, path));
                    }
                });
                SubTasks.Add(ta);
            }
            Task.WaitAll(SubTasks.ToArray());
            #endregion 

            #region 变量与乘子
            //决策变量 x
            Dictionary<CustomerArrival, TravelPath> x
               = new Dictionary<CustomerArrival, TravelPath>();
            foreach (CustomerArrival customer in _ctx.Pal)
            {
                x.Add(customer, new TravelPath());
            }

            //决策变量 w
            Dictionary<ITrainTrip, Dictionary<IRailwayStation, PricePath>> w
                = new Dictionary<ITrainTrip, Dictionary<IRailwayStation, PricePath>>();
            foreach (var train in _ctx.Wor.RailwayTimeTable.Trains)
            {
                w.Add(train, new Dictionary<IRailwayStation, PricePath>());
                foreach (var sta in _ctx.Wor.Net.StationCollection)
                {
                    w[train].Add(sta, null);
                }
            }

            //辅助变量 y
            //记录每条弧在当前w的取值下是否可行(available)，值为true = 可行；false = 不可行
            //超出了y记录的reservation arc 不会有人走
            Dictionary<IEdge<TravelHyperNode>, bool> y
                = new Dictionary<IEdge<TravelHyperNode>, bool>();
            foreach (var p in pathDict.Values.SelectMany(i => i))
            {
                if (p.ReservationArc != null && !y.ContainsKey(p.ReservationArc))
                    y.Add(p.ReservationArc, false);
            }

            //拉格朗日乘子 rho
            Dictionary<IServiceSegment, decimal> LM_rho = _ctx.Wor.RailwayTimeTable.Trains
            .SelectMany(i => i.ServiceSegments).ToDictionary(i => i, i => initMultipler);

            //拉格朗日乘子 rho 迭代方向
            Dictionary<IServiceSegment, decimal> Grad_rho = _ctx.Wor.RailwayTimeTable.Trains
            .SelectMany(i => i.ServiceSegments).ToDictionary(i => i, i => initMultipler);


            //拉格朗日乘子 mu
            Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> LM_mu
                = new Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>>();
            foreach (CustomerArrival customer in _ctx.Pal)
            {
                LM_mu.Add(customer, new Dictionary<TravelPath, decimal>());
                foreach (var path in pathDict[customer])
                {
                    LM_mu[customer].Add(path, initMultipler);
                }
            }
            //拉格朗日乘子 mu 迭代方向
            Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> Grad_mu
                = new Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>>();
            foreach (CustomerArrival customer in _ctx.Pal)
            {
                Grad_mu.Add(customer, new Dictionary<TravelPath, decimal>());
                foreach (var path in pathDict[customer])
                {
                    Grad_mu[customer].Add(path, initMultipler);
                }
            }

            //拉格朗日乘子 lambda
            Dictionary<IEdge<TravelHyperNode>, decimal> LM_lambda = new Dictionary<IEdge<TravelHyperNode>, decimal>();
            //WARNING: 这里缺少了没有旅客选择的reservation arc
            foreach (CustomerArrival customer in _ctx.Pal)
            {
                foreach (var path in pathDict[customer])
                {
                    if (!LM_lambda.ContainsKey(path.ReservationArc))
                    {
                        LM_lambda.Add(path.ReservationArc, initMultipler);
                    }
                }
            }

            //拉格朗日乘子 lambda 迭代方向
            Dictionary<IEdge<TravelHyperNode>, decimal> Grad_lambda = new Dictionary<IEdge<TravelHyperNode>, decimal>();
            foreach (CustomerArrival customer in _ctx.Pal)
            {
                foreach (var path in pathDict[customer])
                {
                    if (!Grad_lambda.ContainsKey(path.ReservationArc))
                    {
                        Grad_lambda.Add(path.ReservationArc, initMultipler);
                    }
                }
            }

            #endregion 

            decimal bigM1 = pathDict.Max(i => i.Value.Max(j => basicGraph.GetPathCost(j)));
            decimal bigM2 = _ctx.Pal.Count();
            decimal bigM = Math.Max(bigM1, bigM2);
            decimal lowerBound = decimal.MinValue;
            decimal upperBound = decimal.MaxValue;

            PrintIterationInfo($"Iteration Number, Lower Bound, Upper Bound, Best Lower Bound, Best Upper Bound, Total Gap(%) ");

            for (int iter = 0; iter < iteration; iter++)
            {
                Log($"--------------第{iter}轮求解开始--------------");

                bool hasFeasibleSolution = true;

                #region 求解LR问题
                SubTasks.Clear();             
                foreach (var train in _ctx.Wor.RailwayTimeTable.Trains)
                {
                    foreach (IRailwayStation station in _ctx.Wor.Net.StationCollection)// 求解w       
                    {
                        Task ta = factory.StartNew(() =>
                        {
                            var graph = DpnAlgorithm.BuildLRwGraph(_ctx, adapter, train, station, pathDict, basicGraph.LinkTrainDict, LM_mu, LM_lambda);
                            DijkstraShortestPaths<DirectedWeightedSparseGraph<string>, string> dijkstra
                                = new DijkstraShortestPaths<DirectedWeightedSparseGraph<string>, string>(graph, "Start");//考虑该旅客到达时间
                            var nodepath = dijkstra.ShortestPathTo("End");
                            if (nodepath == null)
                            {
                                throw new Exception("No path found");
                            }
                            else
                            {
                                w[train][station] = new PricePath(graph, nodepath);
                            }

                        });
                        SubTasks.Add(ta);
                    }
                }
                Task.WaitAll(SubTasks.ToArray());

                foreach (var edge in y.Keys.ToArray())//更新y
                {
                    var sta = edge.Source.Station;
                    var train = basicGraph.GetTrainByReservationLink(edge);
                    y[edge] = w[train][sta].GetWrapPoints(edge.Destination.Price, edge.Source.Time).Any();
                }

                SubTasks.Clear();
                foreach (CustomerArrival customer in _ctx.Pal)// 求解x
                {
                    Task ta = factory.StartNew(() =>
                    {
                        var controlledLRxgraph = new ControlledLRxTravelHyperNetwork(
                            _ctx, adapter, objgraph, customer, pathDict, LM_rho, LM_mu, LM_lambda, y);
                        controlledLRxgraph.Build();
                        var ori = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).OriSta;
                        var des = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).DesSta;

                        TravelHyperNode startNode = new TravelHyperNode()
                        {
                            Time = adapter.ConvertToDiscreteTime(customer.ArriveTime),
                            Station = ori,
                            Price = 0
                        };
                        TravelHyperNode endNode = new TravelHyperNode()
                        {
                            Time = adapter.Horizon + 1440,
                            Station = des,
                            Price = 0
                        };

                        DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode> dijkstra
                            = new DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode>
                            (controlledLRxgraph, startNode);//考虑该旅客到达时间

                        if (!dijkstra.HasPathTo(endNode))
                        {
                            throw new Exception("没有路径!");
                        }
                        else
                        {
                            x[customer] = new TravelPath(controlledLRxgraph, dijkstra.ShortestPathTo(endNode));
                        }
                    });
                    SubTasks.Add(ta);
                }
                Task.WaitAll(SubTasks.ToArray());
                #endregion

                #region 计算拉格朗日函数值作为下界

                decimal templowerBound = 0m;
                decimal templowerBound_part1 = 0m;
                decimal templowerBound_part2 = 0m;
                decimal templowerBound_part3 = 0m;
                decimal templowerBound_part4 = 0m;
                Dictionary<CustomerArrival, decimal> lbValueDic
                    = new Dictionary<CustomerArrival, decimal>();
                //1 计算在基础网络中的路径cost
                foreach (CustomerArrival customer in _ctx.Pal)
                {
                    lbValueDic.Add(customer, objgraph.GetPathCost(x[customer]));
                    templowerBound_part1 += lbValueDic[customer];
                }

                //2计算BRUE项
                templowerBound_part2 += _ctx.Pal.Sum(c => pathDict[c].Sum(p =>
                {
                    decimal secondItem = 0m;
                    secondItem += basicGraph.GetPathCost(x[c]) - basicGraph.GetPathCost(p) - _ctx.SitaDic[c.Customer.MarSegID];
                    secondItem -= (p.ReservationArc != null && y[p.ReservationArc]) ? 0 : bigM;
                    return secondItem * LM_mu[c][p];
                }));

                //3计算In-train Capacity项
                Dictionary<IServiceSegment, int> ServiceDic = _ctx.Wor.RailwayTimeTable
                    .Trains.SelectMany(i => i.ServiceSegments)
                    .ToDictionary(i => i, i => 0);//当前service segment使用情况

                foreach (var p in x.Values)
                {
                    foreach (IServiceSegment seg in p.GetSegments(basicGraph))
                    {
                        ServiceDic[seg] += 1;
                    }
                }
                foreach (var train in _ctx.Wor.RailwayTimeTable.Trains)
                {
                    foreach (var seg in train.ServiceSegments)
                    {
                        templowerBound_part3 += LM_rho[seg] * (ServiceDic[seg] - train.Carriage.Chairs.Count());
                    }
                }

                //4 计算reservation constraint 项
                Dictionary<IEdge<TravelHyperNode>, int> reservationDic
                    = new Dictionary<IEdge<TravelHyperNode>, int>();
                foreach (var p in x.Values)
                {
                    if (reservationDic.ContainsKey(p.ReservationArc))
                    {
                        reservationDic[p.ReservationArc] += 1;
                    }
                    else
                    {
                        reservationDic.Add(p.ReservationArc, 1);
                    }
                }
                foreach (var pair in y.Keys)
                {
                    //y是所有的reservation 的集合 reservationDic 是已经使用的reservation 集合
                    var res = reservationDic.Keys.FirstOrDefault(i => i.Source == pair.Source && i.Destination == pair.Destination);
                    templowerBound_part4 += LM_lambda[pair] * ((res != null ? reservationDic[res] : 0) - (y[pair] ? bigM : 0));
                }

                templowerBound = templowerBound_part1 + templowerBound_part2 + templowerBound_part3 + templowerBound_part4;
                bool hasBetterLowerbound = templowerBound > lowerBound;
                lowerBound = Math.Max(lowerBound, templowerBound);

                Log($"Lower Bound = { Math.Round(templowerBound, 2)}," +
                    $"({ Math.Round(templowerBound_part1, 2) }" +
                    $"+{ Math.Round(templowerBound_part2, 2)}" +
                    $"+{ Math.Round(templowerBound_part3, 2)}" +
                    $"+{ Math.Round(templowerBound_part4, 2)})");

                PrintLBSolution(DpnAlgorithm.GetTravelPathString(_ctx, adapter, objgraph, x, lbValueDic));
                #endregion

                #region 通过一个启发式规则计算上界(按照w模拟到达)

                var pathcost = lbValueDic.ToDictionary(i => i.Key, i => i.Value);
                var x_least = x.ToDictionary(i => i.Key, i => i.Value);//当前w下每个旅客的最短路径
                var x_upperbound = x.ToDictionary(i => i.Key, i => i.Value);              

                # region 2-构建当前y下的出行最小值
                var solutiongraph = new ControlledTravelHyperNetwork(_ctx, adapter, y);
                solutiongraph.Build();
                Parallel.ForEach(_ctx.Pal, customer =>                //foreach (var customer in _ctx.Pal)//求此网络下每个旅客的最短路径
                {
                    var ori = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).OriSta;
                    var des = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).DesSta;

                    TravelHyperNode startNode = new TravelHyperNode()
                    {
                        Time = adapter.ConvertToDiscreteTime(customer.ArriveTime),
                        Station = ori,
                        Price = 0
                    };

                    TravelHyperNode endNode = new TravelHyperNode()
                    {
                        Time = adapter.Horizon + 1440,
                        Station = des,
                        Price = 0
                    };

                    DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode> dijkstra
                       = new DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode>
                       (solutiongraph, startNode);

                    if(!dijkstra.HasPathTo(endNode))
                    {
                        throw new Exception("没有路径!");
                    }
                    else
                    {
                        x_least[customer] = new TravelPath(solutiongraph, dijkstra.ShortestPathTo(endNode));
                    }
                });
                #endregion

                #region 3-修复可行解
                var solutiongraphTemp = new SimNetwork(_ctx, adapter, y);//建立仿真网络
                solutiongraphTemp.Build();
                foreach (var customer in _ctx.Pal)
                {
                    x_upperbound[customer] = x[customer];
                    TravelPath path = x[customer];

                    if (!solutiongraphTemp.IsPathFeasible(path) ||
                       solutiongraphTemp.GetPathCost(path) > solutiongraph.GetPathCost(x_least[customer]) + _ctx.SitaDic[customer.Customer.MarSegID])//如果违反了容量约束或者BRUE约束
                    {
                        var ori = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).OriSta;
                        var des = (_ctx.Wor.Mar[customer.Customer.MarSegID] as IRailwayMarketSegment).DesSta;
                        DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode> dijkstra
                           = new DijkstraShortestPaths<DirectedWeightedSparseGraph<TravelHyperNode>, TravelHyperNode>(solutiongraphTemp, new TravelHyperNode()
                           {
                               Time = adapter.ConvertToDiscreteTime(customer.ArriveTime),
                               Station = ori,
                               Price = 0
                           });

                        //重新查找路径，如果存在路径
                        if (dijkstra.HasPathTo(new TravelHyperNode()
                        {
                            Time = adapter.Horizon + 1440,
                            Station = des,
                            Price = 0
                        }))
                        {
                            x_upperbound[customer] = new TravelPath(solutiongraphTemp, dijkstra.ShortestPathTo(new TravelHyperNode()
                            {
                                Time = adapter.Horizon + 1440,
                                Station = des,
                                Price = 0
                            }));
                            if (solutiongraphTemp.GetPathCost(x_upperbound[customer]) <= //满足BRUE约束
                                solutiongraph.GetPathCost(x_least[customer]) + _ctx.SitaDic[customer.Customer.MarSegID])
                            {
                                path = x_upperbound[customer];
                            }
                            else
                            {
                                hasFeasibleSolution = false;
                                break;
                            }
                        }
                        else
                        {
                            hasFeasibleSolution = false;
                            break;
                        }
                    }

                    pathcost[customer] = objgraph.GetPathCost(path);

                    //加载路径
                    foreach (var seg in path.GetSegments(basicGraph))
                    {
                        solutiongraphTemp.AddUsage(seg, 1);
                    }
                }
                var tempUpperbound = _ctx.Pal.Sum(c => objgraph.GetPathCost(x_upperbound[c]));
                #endregion

                //如果有最优解再更新上界
                bool hasBetterUpperbound = tempUpperbound < upperBound;
                if (hasFeasibleSolution) upperBound = Math.Min(upperBound, tempUpperbound);
                Log($"Upper Bound = {  Math.Round(tempUpperbound,2) },找到可行解 : { hasFeasibleSolution.ToString()}");
                #endregion

                #region  Terminatation 判定 
                decimal totalGap = 0;
                string gapStr = "";
                //如果上限是无穷，那么此时gap也是无穷
                if (upperBound == decimal.MaxValue)
                {
                    totalGap = decimal.MaxValue;
                    gapStr = $"+∞";
                }
                else
                {
                    totalGap = (upperBound - lowerBound) / lowerBound;
                    gapStr = $"{ Math.Round(totalGap * 100m, 2)}%";
                }
                if (totalGap < terminalFactor && totalGap > 0)
                {
                    Log($"已经达到终止条件，Gap={ totalGap }");
                    break;
                }

                Log($"Total Gap = { gapStr }");
                #endregion

                #region 更新乘子
                decimal step = 1.618m / (iter + 1);
                foreach (CustomerArrival c in _ctx.Pal)//更新mu
                {
                    foreach (TravelPath p in pathDict[c])
                    {
                        Grad_mu[c][p] = basicGraph.GetPathCost(x[c]) - basicGraph.GetPathCost(p) - _ctx.SitaDic[c.Customer.MarSegID]
                            - ((p.ReservationArc != null && y[p.ReservationArc]) ? 0 : bigM);
                        LM_mu[c][p] = Math.Max(0, LM_mu[c][p] + step * Grad_mu[c][p]);
                    }
                }
                foreach (var pair in y.Keys) //更新lambda
                {
                    var res = reservationDic.Keys.FirstOrDefault(i => i.Source == pair.Source && i.Destination == pair.Destination);
                    Grad_lambda[pair] = ((res != null ? reservationDic[res] : 0) - (y[pair] ? bigM : 0));
                    LM_lambda[pair] = Math.Max(0, LM_lambda[pair] + step * Grad_lambda[pair]);
                }
                foreach (var train in _ctx.Wor.RailwayTimeTable.Trains)//更新rho
                {
                    foreach (var seg in train.ServiceSegments)
                    {
                        Grad_rho[seg] = ServiceDic[seg] - train.Carriage.Chairs.Count();
                        LM_rho[seg] = Math.Max(0, LM_rho[seg] + step * Grad_rho[seg]);
                    }
                }
                #endregion

                #region 输出信息

                //SendMessage($"#Iteration Number, Lower Bound, Upper Bound, Best Lower Bound, Best Upper Bound, Total Gap(%) ");
                PrintIterationInfo($"#{iter},{ Math.Round(templowerBound) },{ Math.Round(tempUpperbound) },{ Math.Round(lowerBound)}" +
                    $",{ Math.Round(upperBound) },{ gapStr }");

                if (hasFeasibleSolution && hasBetterUpperbound)
                {
                    ObjValue = pathcost.Sum(i => i.Value);
                    PrintSolution(
                        DpnAlgorithm.GetTravelPathString(_ctx, adapter, solutiongraph, x_upperbound, pathcost),
                        DpnAlgorithm.GetPricingPathString(_ctx, adapter, w));
                }

                #endregion

                Log($"--------------第{iter}轮求解结束--------------");
            }
        }
    }
}