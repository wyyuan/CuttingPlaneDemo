namespace Domain.Calculation.Optimization.DPN.Solver
{
    public static class DpnSolverFactory
    {
        public static AbsDpnSolver Build(DPNProblemContext ctx, string solverName)
        {
            switch (solverName)
            { 
                case "Subgradient":
                    {
                        return new DpnSolver(ctx);
                    }
                case "CuttingPlane":
                    {
                        return new DpnSolverV3(ctx);
                    }
                case "CuttingPlaneWithTrustRegion":
                    {
                        return new DpnSolverV4(ctx);
                    }
                default:
                    {
                        return new DpnSolverV4(ctx);
                    }
            }
        }
    }
}
