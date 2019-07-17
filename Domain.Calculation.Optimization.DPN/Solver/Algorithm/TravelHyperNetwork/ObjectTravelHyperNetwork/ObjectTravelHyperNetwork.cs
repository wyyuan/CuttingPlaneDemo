using Domain.Base.Schedule.Timetable;

namespace Domain.Calculation.Optimization.DPN
{
    /// <summary>
    /// 实际的出行网络
    /// </summary>
    public class ObjectTravelHyperNetwork : BasicTravelHyperNetwork
    {
        public ObjectTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter) : base(ctx, adapter) { }

        public decimal CalIntrainSectionLinkCost(ITrainTrip train, IServiceSegment seg, decimal price)
        {
            return this.GetIntrainSectionLinkCost(train, seg, price);
        }
        public decimal CalIntrainStpLinkCost(ITrainTrip train, IStopStation stop, decimal price)
        {
            return this.GetIntrainStpLinkCost(train, stop, price);
        }
        public decimal CalWaitingLinkCost()
        {
            return this.GetWaitingLinkCost();
        }
        public decimal CalReservationLinkCost(TravelHyperNode depNode, TravelHyperNode arrNode)
        {
            return this.GetReservationLinkCost(depNode, arrNode);
        }
        public decimal CalFinishLinkCost()
        {
            return this.GetFinishLinkCost();
        }
    }

    public class MinTravelTimeObjectTravelHyperNetwork : ObjectTravelHyperNetwork
    {
        public MinTravelTimeObjectTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter) 
            : base(ctx, adapter) { }

        protected override decimal GetIntrainSectionLinkCost(ITrainTrip train, IServiceSegment seg, decimal price)
        {
            return System.Convert.ToDecimal((seg.ArrTime - seg.DepTime).TotalMinutes) * _ctx.Vot;
        }
        protected override decimal GetIntrainStpLinkCost(ITrainTrip train, IStopStation stop, decimal price)
        {
            return System.Convert.ToDecimal((stop.DepTime - stop.ArrTime).TotalMinutes) * _ctx.Vot;
        }
        protected override decimal GetWaitingLinkCost()
        {
            return 0;
        }
        protected override decimal GetReservationLinkCost(TravelHyperNode depNode, TravelHyperNode arrNode)
        {
            return 0;
        }
        protected override decimal GetFinishLinkCost()
        {
            return 0;
        }
    }

    public class MaxRevenueTravelHyperNetwork : ObjectTravelHyperNetwork
    {
        public MaxRevenueTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter)
            : base(ctx, adapter) { }

        protected override decimal GetIntrainSectionLinkCost(ITrainTrip train, IServiceSegment seg, decimal price)
        {
            return -price*_ctx.BasicPriceDic[seg];
        }
        protected override decimal GetIntrainStpLinkCost(ITrainTrip train, IStopStation stop, decimal price)
        {
            return 0;
        }
        protected override decimal GetWaitingLinkCost()
        {
            return 0;
        }
        protected override decimal GetReservationLinkCost(TravelHyperNode depNode, TravelHyperNode arrNode)
        {
            return 0;
        }
        protected override decimal GetFinishLinkCost()
        {
            return 0;
        }
    }

    internal class MinTravelTime_MaxRevenue_MixTravelHyperNetwork : ObjectTravelHyperNetwork
    {
        decimal _cost_weight;
        decimal _rev_weight;
        public MinTravelTime_MaxRevenue_MixTravelHyperNetwork(DPNProblemContext ctx, DiscreteTimeAdapter adapter,
            decimal cost_weight, decimal rev_weight)
            : base(ctx, adapter)
        {
            _cost_weight = cost_weight;
            _rev_weight = rev_weight;
        }

        protected override decimal GetIntrainSectionLinkCost(ITrainTrip train, IServiceSegment seg, decimal price)
        {
            decimal part1 =  System.Convert.ToDecimal((seg.ArrTime - seg.DepTime).TotalMinutes) * _ctx.Vot;
            decimal part2 =  -price * _ctx.BasicPriceDic[seg];
            return part1 * _cost_weight + _rev_weight * part2;
        }
        protected override decimal GetIntrainStpLinkCost(ITrainTrip train, IStopStation stop, decimal price)
        {
            decimal part1 = System.Convert.ToDecimal((stop.DepTime - stop.ArrTime).TotalMinutes) * _ctx.Vot;
            decimal part2 = 0;
            return part1 * _cost_weight + _rev_weight * part2;
        }
        protected override decimal GetWaitingLinkCost()
        {
            return 0;
        }
        protected override decimal GetReservationLinkCost(TravelHyperNode depNode, TravelHyperNode arrNode)
        {
            return 0;
        }
        protected override decimal GetFinishLinkCost()
        {
            return 0;
        }
    }
}
