using System;

namespace Domain.Calculation.Optimization.DPN
{
    public class DiscreteTimeAdapter
    {
        private DateTime m_StartTime;
        private DateTime m_EndTime;
        private int m_Horizon;
        private int m_Resolution;

        public DiscreteTimeAdapter(DateTime startTime, DateTime endTime, int resolution)//Minutes
        {
            if (endTime < startTime) throw new Exception("开始时间应早于结束时间!");
            if (resolution <= 0) throw new Exception("解析度应为正整数!");
            m_StartTime = startTime;
            m_EndTime = endTime;
            m_Resolution = resolution;
            m_Horizon = (int)Math.Ceiling((endTime - startTime).TotalMinutes/resolution);
        }

        /// <summary>
        /// 购票周期范围
        /// </summary>
        public int Horizon  => m_Horizon;
        public int Resolution => m_Resolution;

        public DateTime ConvertToDateTime(int x)
        {
            if (x < 0 || x > m_Horizon) throw new Exception("时间超出范围");
            return m_StartTime.AddSeconds(x * (m_EndTime - m_StartTime).TotalSeconds / m_Horizon);
        }
        public int ConvertToDiscreteTime(DateTime t)
        {
            if (t < m_StartTime || t > m_EndTime) throw new Exception("时间超出范围");
            return (int)(m_Horizon * (t - m_StartTime).TotalSeconds / (m_EndTime - m_StartTime).TotalSeconds);
        }

    }
}
