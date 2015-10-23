using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;

namespace TeamCityApi.Helpers.Graphs
{
    public static class GraphTraversal
    {
        public static HashSet<T> FindAllParents<T>(this Graph<T> g, T child)
        {
            var allParents = new HashSet<T>();

            var directParents = g.GetParents(child).ToArray();

            allParents.UnionWith(directParents);

            if (directParents.Any())
            {
                foreach (var directParent in directParents)
                {
                    allParents.UnionWith(FindAllParents(g, directParent));
                }
            }

            return allParents;
        }
    }
}