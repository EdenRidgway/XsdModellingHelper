using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XsdHelper
{
    public class TopologicalEdgeSorter<T> where T : IComparable
    {
        public ICollection<T> Sort(IEnumerable<Edge<T>> edges) 
        {
            List<T> combinedList = (from edge in edges select edge.Source)
                                   .Union
                                   (from edge in edges select edge.Target)
                                   .Distinct()
                                   .ToList();

            Dictionary<T, IEnumerable<T>> dependencies = combinedList.ToDictionary(i => i, i => edges.Where(e => e.Source.Equals(i)).Select(e => e.Target));

            List<T> visited = new List<T>();
            List<T> sorted = new List<T>();

            combinedList.ForEach(n => Visit(n, visited, sorted, dependencies));

            return sorted;
        }

        /// <summary>
        /// Depth first approach to partially sorting the list
        /// </summary>
        private static void Visit(T node, ICollection<T> visited, ICollection<T> sorted, IDictionary<T, IEnumerable<T>> dependencies)
        {
            if (visited.Contains(node)) return;

            visited.Add(node);

            foreach (T dependency in dependencies[node])
            {
                Visit(dependency, visited, sorted, dependencies);
            }

            sorted.Add(node);
        }
    }
}
