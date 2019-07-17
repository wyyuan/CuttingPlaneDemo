using Domain.Base.Demand.Reservation;
using Domain.Base.RNSM.Others;
using Domain.Base.Schedule.Timetable;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    /// <summary>
    /// 铁路席位控制问题环境
    /// </summary>
    [DisplayName("DPN")]
    public class DPNProblemContext : AbsProblemContext
    {
        public DPNProblemContext(IRnsmWorkspace wor) : base(wor) { }

        /// <summary>
        /// 价格等级
        /// </summary>
        public List<decimal> PriceLevelList
        {
            get => GetParameter("PriceLevelList","").Split(',').Select(i => Convert.ToDecimal(i)).ToList();
            set => SetParameter("PriceLevelList",string.Join(",", value));
        }
        /// <summary>
        /// 基本价格
        /// </summary>
        public Dictionary<IServiceSegment, decimal> BasicPriceDic { get; set; }
        /// <summary>
        /// 每个市场旅客的indifference band
        /// </summary>
        public Dictionary<int, decimal> SitaDic { get; set; }
        /// <summary>
        /// 到达序列
        /// </summary>
        public CustomerArrivalChain Pal { get; set; }
        /// <summary>
        /// 预售开始时间
        /// </summary>
        public DateTime StartTime
        {
            get => Convert.ToDateTime(GetParameter("StartTime", ""));
            set => SetParameter("StartTime", value);
        }
        /// <summary>
        /// 预售结束时间
        /// </summary>
        public DateTime EndTime
        {
            get => Convert.ToDateTime(GetParameter("EndTime", ""));
            set => SetParameter("EndTime", value);
        }
        /// <summary>
        /// Value of time (元/分钟)
        /// </summary>
        public decimal Vot
        {
            get => Convert.ToDecimal(GetParameter("Vot", ""));
            set => SetParameter("Vot", value);            
        }
        /// <summary>
        /// 等待成本
        /// </summary>
        public decimal WaitingVot
        {
            get => Convert.ToDecimal(GetParameter("WaitingCost", ""));
            set => SetParameter("WaitingCost", value);
        }
        /// <summary>
        /// 控制频率(分钟)
        /// </summary>
        public int ControlInterval
        {
            get => GetParameter<int>("ControlInterval");
            set => SetParameter("ControlInterval", value);
        }
        /// <summary>
        /// 换乘时间最小值(分钟)
        /// </summary>
        public int TransferThreshold
        {
            get => GetParameter<int>("TransferThreshold");
            set => SetParameter("TransferThreshold", value);
        }
    }
}
