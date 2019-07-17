using System;
using System.Collections.Generic;

namespace Domain.Calculation.Optimization.DPN
{
    public abstract class AbsDpnSolver : AbsOptSolver<DPNProblemContext>
    {
        public AbsDpnSolver(DPNProblemContext ctx) : base(ctx) { }

        #region Events
        /// <summary>
        /// 在一次迭代结束时执行的 Action
        /// </summary>
        public Action<string> OnIterationFinished { get; set; }
        /// <summary>
        /// 在找到一个可行解时执行的 Action
        /// </summary>
        public Action<IEnumerable<string>, IEnumerable<string>> OnFeasibleSolutionGenerated { get; set; }
        /// <summary>
        /// 在迭代完一轮之后的下界解
        /// </summary>
        public Action<IEnumerable<string>> OnLowerBoundSolutionGenerated { get; set; }

        protected void PrintIterationInfo(string message)
        {
            if (OnIterationFinished != null)
            {
                OnIterationFinished.Invoke(message);
            }
        }
        protected void PrintSolution(IEnumerable<string> solutionX, IEnumerable<string> solutionY)
        {
            if (OnFeasibleSolutionGenerated != null)
            {
                OnFeasibleSolutionGenerated.Invoke(solutionX, solutionY);
            }
        }
        protected void PrintLBSolution(IEnumerable<string> solutionX)
        {
            if (OnLowerBoundSolutionGenerated != null)
            {
                OnLowerBoundSolutionGenerated.Invoke(solutionX);
            }
        }

        #endregion
    }
}