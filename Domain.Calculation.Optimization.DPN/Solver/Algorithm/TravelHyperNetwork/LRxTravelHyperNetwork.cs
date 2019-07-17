using DataStructures.Graphs;
using Domain.Base.Demand.Reservation;
using Domain.Base.Schedule.Timetable;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    public class LRxTravelHyperNetwork : BasicTravelHyperNetwork
    {
        decimal _u;
        Dictionary<IServiceSegment, decimal> _LM_rho;//拉格朗日乘子 ρ
        Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> _LM_mu; //拉格朗日乘子 μ
        Dictionary<IEdge<TravelHyperNode>, decimal> _LM_lambda;
        ObjectTravelHyperNetwork _objNet;

        public LRxTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            ObjectTravelHyperNetwork objNet,
            CustomerArrival customer, //旅客
            Dictionary<CustomerArrival, List<TravelPath>> PathDict,
            Dictionary<IServiceSegment, decimal> LM_rho,//拉格朗日乘子 ρ
            Dictionary<CustomerArrival, Dictionary<TravelPath, decimal>> LM_mu, //拉格朗日乘子 μ
            Dictionary<IEdge<TravelHyperNode>, decimal> LM_lambda) : base(ctx, adapter)
        {
            //先执行父类构造函数，再执行子类构造函数
            _LM_rho = LM_rho;
            _LM_mu = LM_mu;
            _LM_lambda = LM_lambda;
            _u = (PathDict.ContainsKey(customer) ? PathDict[customer].Sum(i => LM_mu[customer][i]) : 0m);

            _objNet = objNet;
        }

        protected override decimal GetIntrainSectionLinkCost(ITrainTrip train, IServiceSegment seg, decimal price)
        {
            decimal part1 = _objNet.CalIntrainSectionLinkCost(train, seg, price);
            decimal part2 = _u * (_ctx.BasicPriceDic[seg] * price + ((decimal)(seg.ArrTime - seg.DepTime).TotalMinutes * _ctx.Vot))
                + _LM_rho[seg];
            return part1 + part2;
        }
        protected override decimal GetIntrainStpLinkCost(ITrainTrip train, IStopStation stop, decimal price)
        {
            decimal part1 = _objNet.CalIntrainStpLinkCost(train, stop, price);
            decimal part2 = _u * ((decimal)(stop.DepTime - stop.ArrTime).TotalMinutes * _ctx.Vot);
            return part1 + part2;
        }
        protected override decimal GetWaitingLinkCost()
        {
            decimal part1 = _objNet.CalWaitingLinkCost();
            decimal part2 = _u * _adapter.Resolution * _ctx.WaitingVot;
            return part1 + part2;
        }
        protected override decimal GetReservationLinkCost(TravelHyperNode depNode, TravelHyperNode arrNode)
        {
            decimal part1 = _objNet.CalReservationLinkCost(depNode, arrNode);
            var link = _LM_lambda.Keys.FirstOrDefault(i => i.Source == depNode && i.Destination == arrNode);
            decimal part2 = (link != null ? _LM_lambda[link] : 0) + DpnAlgorithm.ASmallCost; // 一次预订花费一分钟;
            return part1 + part2;
        }
        protected override decimal GetFinishLinkCost()
        {
            decimal part1 = _objNet.CalFinishLinkCost();
            decimal part2 = base.GetFinishLinkCost();
            return part1 + part2;
        }
    }
}
