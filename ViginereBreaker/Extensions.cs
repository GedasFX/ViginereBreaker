using System.Collections.Generic;
using System.Linq;

namespace ViginereBreaker
{
    public static class Extensions
    {
        /// <summary>
        /// N Choose K
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="elements"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public static IEnumerable<IEnumerable<T>> ChooseK<T>(this IEnumerable<T> elements, int k)
        {
            return k == 0
                ? new[] { new T[0] }
                : elements.SelectMany((e, i) => elements.Skip(i + 1).ChooseK(k - 1).Select(c => (new[] { e }).Concat(c)));
        }
    }
}
