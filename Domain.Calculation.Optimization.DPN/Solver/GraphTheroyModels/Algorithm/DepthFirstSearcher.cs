/***
 * Implements the Depth-First Search algorithm in two ways: Iterative and Recursive. 
 * 
 * Provides multiple functions for traversing graphs: 
 *  1. PrintAll(), 
 *  2. VisitAll(Action<T> forEachFunc), 
 *  3. FindFirstMatch(Predicate<T> match). 
 * 
 * The VisitAll() applies a function to every graph node. The FindFirstMatch() function searches the graph for a predicate match.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using DataStructures.Graphs;

namespace Algorithms.Graphs
{
    public static class DepthFirstSearcher
    {
        /// <summary>
        /// DFS Recursive Helper function. 
        /// Visits the neighbors of a given vertex recusively, and applies the given Action<T> to each one of them.
        /// </summary>
        private static void _visitNeighbors<T>(T Vertex, ref IGraph<T> Graph, ref Dictionary<T, object> Parents, Action<T> Action) where T : IComparable<T>
        {
            foreach (var adjacent in Graph.Neighbours(Vertex))
            {
                if (!Parents.ContainsKey(adjacent))
                {
                    // DFS VISIT NODE
                    Action(adjacent);

                    // Save adjacents parent into dictionary
                    Parents.Add(adjacent, Vertex);

                    // Recusively visit adjacent nodes
                    _visitNeighbors(adjacent, ref Graph, ref Parents, Action);
                }
            }
        }

        /// <summary>
        /// Recursive DFS Implementation with helper.
        /// Traverses all the nodes in a graph starting from a specific node, applying the passed action to every node.
        /// </summary>
        public static void VisitAll<T>(ref IGraph<T> Graph, T StartVertex, Action<T> Action) where T : IComparable<T>
        {
            // Check if graph is empty
            if (Graph.VerticesCount == 0)
                throw new Exception("Graph is empty!");

            // Check if graph has the starting vertex
            if (!Graph.HasVertex(StartVertex))
                throw new Exception("Starting vertex doesn't belong to graph.");

            var parents = new Dictionary<T, object>(Graph.VerticesCount);	// keeps track of visited nodes and tree-edges

            foreach (var vertex in Graph.Neighbours(StartVertex))
            {
                if (!parents.ContainsKey(vertex))
                {
                    // DFS VISIT NODE
                    Action(vertex);

                    // Add to parents dictionary
                    parents.Add(vertex, null);

                    // Visit neighbors using recusrive helper
                    _visitNeighbors(vertex, ref Graph, ref parents, Action);
                }
            }
        }

        /// <summary>
        /// Iterative DFS Implementation.
        /// Given a starting node, dfs the graph and print the nodes as they get visited.
        /// </summary>
        public static void PrintAll<T>(IGraph<T> Graph, T StartVertex) where T : IComparable<T>
        {
            // Check if graph is empty
            if (Graph.VerticesCount == 0)
                throw new Exception("Graph is empty!");

            // Check if graph has the starting vertex
            if (!Graph.HasVertex(StartVertex))
                throw new Exception("Starting vertex doesn't belong to graph.");

            var visited = new HashSet<T>();
            var stack = new Stack<T>(Graph.VerticesCount);

            stack.Push(StartVertex);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!visited.Contains(current))
                {
                    // DFS VISIT NODE STEP
                    Console.Write(String.Format("({0}) ", current));
                    visited.Add(current);

                    // Get the adjacent nodes of current
                    foreach (var adjacent in Graph.Neighbours(current))
                        if (!visited.Contains(adjacent))
                            stack.Push(adjacent);
                }
            }

        }

        /// <summary>
        /// Iterative DFS Implementation.
        /// Given a predicate function and a starting node, this function searches the nodes of the graph for a first match.
        /// </summary>
        public static T FindFirstMatch<T>(IGraph<T> Graph, T StartVertex, Predicate<T> Match) where T : IComparable<T>
        {
            // Check if graph is empty
            if (Graph.VerticesCount == 0)
                throw new Exception("Graph is empty!");

            // Check if graph has the starting vertex
            if (!Graph.HasVertex(StartVertex))
                throw new Exception("Starting vertex doesn't belong to graph.");

            var stack = new Stack<T>();
            var parents = new Dictionary<T, object>(Graph.VerticesCount);	// keeps track of visited nodes and tree-edges

            object currentParent = null;
            stack.Push(StartVertex);

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                // Skip loop if node was already visited
                if (!parents.ContainsKey(current))
                {
                    // Save its parent into the dictionary
                    // Mark it as visited
                    parents.Add(current, currentParent);

                    // DFS VISIT NODE STEP
                    if (Match(current))
                        return current;

                    // Get currents adjacent nodes (might add already visited nodes).
                    foreach (var adjacent in Graph.Neighbours(current))
                        if (!parents.ContainsKey(adjacent))
                            stack.Push(adjacent);

                    // Mark current as the father of its adjacents. This helps keep track of tree-nodes.
                    currentParent = current;
                }
            }//end-while

            throw new Exception("Item was not found!");
        }

        /// <summary>
        /// Find all paths from start to end
        /// </summary>
        public static List<IEnumerable<T>> FindAllPaths<T>(IGraph<T> Graph,
        T StartVertex, T EndVertex) where T : IComparable<T>
        {
            return FindAllPaths(Graph, StartVertex, EndVertex, (list) => true);
        }

        public static List<IEnumerable<T>> FindAllPaths<T>(IGraph<T> Graph,
            T StartVertex, T EndVertex, Func<IEnumerable<T>,bool> PathContstraintFunc) where T : IComparable<T>
        {
            List<IEnumerable<T>> list = new List<IEnumerable<T>>();

            // Check if graph is empty
            if (Graph.VerticesCount == 0)
                throw new Exception("Graph is empty!");

            // Check if graph has the starting vertex
            if (!Graph.HasVertex(StartVertex))
                throw new Exception("Starting vertex doesn't belong to graph.");

            if (!Graph.HasVertex(EndVertex))
                throw new Exception("End vertex doesn't belong to graph.");

            var stack = new Stack<T>();
            var states = new Dictionary<T, bool>(Graph.VerticesCount);	// keeps track of visited nodes and tree-edges
            foreach (T v in Graph.Vertices)
            {
                states.Add(v, false);
            }
            // A 将始点设置为已访问，将其入栈      
            stack.Push(StartVertex);
            states[StartVertex] = true;
            T adjNode = default(T);
            while (stack.Count != 0)
            {
                var current = stack.Peek();
                if (current.CompareTo(EndVertex) == 0)  //E 当栈顶元素为终点时，设置终点没有被访问过，打印栈中元素，弹出栈顶节点
                {
                    List<T> path = PrintPath(stack);
                    if(PathContstraintFunc.Invoke(path))
                        list.Add(path);//print path
                    adjNode = stack.Pop();
                    states[adjNode] = false;
                }
                else
                {
                    //B 查看栈顶节点V在图中，有没有可以到达、且没有入栈、且没有从这个节点V出发访问过的节点.
                    var next_node = getNextNode(Graph, states, stack, current, adjNode);
                    if (next_node != null && next_node.CompareTo(default(T)) != 0)
                    {
                        //置当前节点访问状态为已在stack中
                        stack.Push(next_node);
                        //临接点重置
                        adjNode = default(T);
                        states[next_node] = true;
                    }
                    else
                    {
                        adjNode = stack.Pop();
                        states[adjNode] = false;
                    }
                }
            }
            return list;
        }

        private static T getNextNode<T>(IGraph<T> graph, Dictionary<T, bool> states, Stack<T> stack,
                        T current, T adjNode) where T : IComparable<T>
        {
            if ((adjNode == null || adjNode.CompareTo(default(T)) == 0)
                && (graph.Neighbours(current).Count != 0))
            {
                return graph.Neighbours(current)[0];
            }
            else if (stack.Contains(adjNode)
                || (adjNode != null && graph.Neighbours(current).Count>0 && !graph.Neighbours(current).Contains(adjNode))
                || graph.Neighbours(current).Count == 0
                || (adjNode != null && adjNode.CompareTo(graph.Neighbours(current).Last()) == 0))
            {
                return default(T);
            }
            else
            {
                int n = graph.Neighbours(current).IndexOf(adjNode);
                T node = graph.Neighbours(current)[n + 1];
                if (states[node])
                {
                    return default(T);
                }
                else
                {
                    return node;
                }
            }
        }

        private static List<T> PrintPath<T>(Stack<T> stack) where T : IComparable<T>
        {
            List<T> path = new List<T>();
            foreach (T a in stack)
            {
                path.Insert(0, a);
            }
            return path;
        }
    }

}

