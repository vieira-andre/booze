using Microsoft.Phone.Globalization;
using System.Collections.Generic;
using System.Globalization;

namespace Booze
{
    class Agrupamento<T> : List<T>
    {
        public delegate string GetKeyDelegate(T item);
        public string Key { get; private set; }

        public Agrupamento(string key)
        {
            Key = key;
        }

        private static List<Agrupamento<T>> CriaGrupos(SortedLocaleGrouping slg)
        {
            List<Agrupamento<T>> list = new List<Agrupamento<T>>();

            foreach (string key in slg.GroupDisplayNames)
            {
                list.Add(new Agrupamento<T>(key));
            }

            return list;
        }

        public static List<Agrupamento<T>> CriaGrupos(IEnumerable<T> items, CultureInfo ci, GetKeyDelegate getKey, bool sort)
        {
            SortedLocaleGrouping slg = new SortedLocaleGrouping(ci);
            List<Agrupamento<T>> list = CriaGrupos(slg);

            foreach (T item in items)
            {
                int index = slg.GetGroupIndex(getKey(item));

                if (index >= 0 && index < list.Count)
                {
                    list[index].Add(item);
                }
            }

            if (sort)
            {
                foreach (Agrupamento<T> group in list)
                {
                    group.Sort((c0, c1) => { return ci.CompareInfo.Compare(getKey(c0), getKey(c1)); });
                }
            }

            return list;
        }
    }
}
