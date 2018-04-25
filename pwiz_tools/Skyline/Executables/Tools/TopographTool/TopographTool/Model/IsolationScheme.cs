using System.Collections.Generic;
using System.Linq;
using pwiz.Common.Collections;

namespace TopographTool.Model
{
    public class IsolationScheme
    {
        public static readonly IsolationScheme DEFAULT = new IsolationScheme(FromDoubles(new[]
        {
            400, 407.7,
            415.3, 422.2,
            422.2, 429.2,
            436.8, 444.3,
            444.3, 451.2,
            451.2, 458.2,
            458.2, 466.2,
            466.2, 472.9,
            472.9, 477.8,
            477.8, 484.9,
            484.9, 490.7,
            490.7, 497.2,
            497.2, 503.8,
            503.8, 510.7,
            510.7, 517.7,
            517.7, 524.8,
            524.8, 532.8,
            532.8, 539.8,
            539.8, 546.3,
            546.3, 554,
            554, 561.3,
            561.3, 567.8,
            567.8, 575.2,
            575.2, 581.8,
            581.8, 588.3,
            588.3, 595.3,
            595.3, 601.3,
            601.3, 608.4,
            608.4, 616.4,
            616.4, 624.3,
            624.3, 631.7,
            631.7, 640.3,
            640.3, 647.4,
            647.4, 654.3,
            654.3, 661,
            661, 669.8,
            669.8, 678.3,
            678.3, 687.3,
            687.3, 696.4,
            696.4, 706.4,
            706.4, 715.4,
            715.4, 725.7,
            725.7, 736.9,
            736.9, 746.1,
            746.1, 757,
            757, 767.4,
            767.4, 779,
            779, 792.4,
            792.4, 806.5,
            806.5, 819.5,
            819.5, 833.7,
            833.7, 848.9,
            848.9, 865.5,
            865.5, 883.9,
            883.9, 899.4,
            899.4, 918.5,
            918.5, 941.6,
            941.6, 971.1,
            971.1, 1005.5,
            1005.5, 1052.5,
            1052.5, 1110.1,
            1110.1, 1200
        }));

        public IsolationScheme(IEnumerable<IsolationWindow> windows)
        {
            Windows = ImmutableList.ValueOf(windows.OrderBy(w=>w).Distinct());
        }
        public ImmutableList<IsolationWindow> Windows { get; private set; }

        public IEnumerable<IsolationWindow> GetWindows(double precursorMz)
        {
            return Windows.Where(w => w.Contains(precursorMz));
        }

        public static IEnumerable<IsolationWindow> FromDoubles(IEnumerable<double> windows)
        {
            var enumerator = windows.GetEnumerator();
            while (enumerator.MoveNext())
            {
                var start = enumerator.Current;
                enumerator.MoveNext();
                yield return new IsolationWindow(start, enumerator.Current);
            }
        }
    }
}
