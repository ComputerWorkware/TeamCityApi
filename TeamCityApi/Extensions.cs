using System;
using System.Collections.Generic;

namespace TeamCityApi
{
    public static class Extensions
    {
        public static void ForEach<T>(this List<T> list, Action<T> action)
        {
            foreach (T item in list)
            {
                action(item);
            }
        }
    }
}