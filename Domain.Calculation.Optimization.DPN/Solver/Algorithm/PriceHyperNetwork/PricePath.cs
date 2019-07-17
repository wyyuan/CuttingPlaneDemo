using DataStructures.Graphs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN
{
    public class PricePath
    {
        //string: 价格_时间
        private DirectedWeightedSparseGraph<string> _graph;
        private List<string> _nodepath;

        public PricePath(DirectedWeightedSparseGraph<string> graph, IEnumerable<string> Strings)
        {
            _nodepath = Strings.ToList();
            _graph = graph;
        }
        public List<string> Nodepath { get => _nodepath; }
        public IEnumerable<string> GetWrapPoints(decimal pricelevel,int time)
        {
            for (int i = 1; i < Nodepath.Count - 1; i++)
            {
                string[] str1 = Nodepath[i].Split('_');
                string[] str2 = Nodepath[i + 1].Split('_');

                int sT = Convert.ToInt32(str1[1]);
                int eT = Nodepath[i + 1] == "End" ? time+1 : Convert.ToInt32(str2[1]);
                if ((str1[0] == pricelevel.ToString())
                    && (sT <= time) && (time < eT))
                {
                    yield return Nodepath[i];
                }
            }
        }
    }
}
