using System;
using System.Diagnostics;
using DataStructures.Graphs;
using Algorithms.Graphs;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace Domain.Calculation.Optimization.DPN.Test
{
    [TestClass]
    public class GraphsDepthFirstSearchTest
	{
        [TestMethod]
        public void DoTest()
		{
            DirectedWeightedSparseGraph<string> graph = new DirectedWeightedSparseGraph<string>();

			// Add vertices
			var verticesSet1 = new string[] { "a", "z", "s", "x", "d", "c", "f", "v" };
			graph.AddVertices (verticesSet1);

			// Add edges
			graph.AddEdge("a", "s", 1);
			graph.AddEdge("a", "z", 1);
			graph.AddEdge("s", "x", 1);
			graph.AddEdge("x", "d", 1);
			graph.AddEdge("x", "c", 1);
			graph.AddEdge("d", "f", 1);
			graph.AddEdge("d", "c", 1);
			graph.AddEdge("c", "f", 1);
			graph.AddEdge("c", "v", 1);
			graph.AddEdge("v", "f", 1);

            // Print the nodes in graph
            Console.WriteLine(" [*] DFS PrintAll: ");
            DepthFirstSearcher.PrintAll(graph, "a");
            Console.WriteLine("\r\n");

            var list = DepthFirstSearcher.FindAllPaths(graph, "a","f");

            foreach(var path in list)
            {
                Console.WriteLine(string.Join(",",path));
            }

            Assert.AreEqual(5,list.Count);
		}
	}
}

