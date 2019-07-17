namespace Domain.Calculation.Optimization.DPN
{
    public static class ObjectNetworkFactory
    {
        public static ObjectTravelHyperNetwork Create(string typeName, DPNProblemContext ctx, DiscreteTimeAdapter adapter)
        {
            switch (typeName)
            {
                case "MinTotalCost":
                    {
                        return new ObjectTravelHyperNetwork(ctx, adapter);
                    }
                case "MinTravelTimeCost":
                    {
                        return new MinTravelTimeObjectTravelHyperNetwork(ctx, adapter);
                    }
                case "MaxRevenue":
                    {
                        return new MaxRevenueTravelHyperNetwork(ctx, adapter);
                    }
                case "MinTotalCost_MaxRevenue_Mix":
                    {
                        decimal cost_weight = ctx.GetParameter<decimal>("MinTotalCost_weight", 0.5m);
                        decimal rev_weight = ctx.GetParameter<decimal>("MaxRevenue_weight", 0.5m);
                        return new MinTravelTime_MaxRevenue_MixTravelHyperNetwork(ctx, adapter, cost_weight, rev_weight);
                    }
                default:
                    {
                        return new ObjectTravelHyperNetwork(ctx, adapter);
                    }
            }
        }
    }


}
