using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NuGet.Frameworks;

namespace Buildalyzer
{
    public class AnalyzerResults : IAnalyzerResults
    {
        private readonly ConcurrentDictionary<NuGetFramework, IAnalyzerResult> _results = new ConcurrentDictionary<NuGetFramework, IAnalyzerResult>();

        private bool? _overallSuccess = null;

        public bool OverallSuccess => _overallSuccess == true;

        internal void Add(IEnumerable<IAnalyzerResult> results, bool overallSuccess)
        {
            foreach (IAnalyzerResult result in results)
            {
                _results[result.TargetFramework ?? NuGetFramework.AnyFramework] = result;
            }
            _overallSuccess = _overallSuccess.HasValue ? _overallSuccess.Value && overallSuccess : overallSuccess;
        }

        public IAnalyzerResult this[NuGetFramework targetFramework] => _results[targetFramework];

        public IEnumerable<NuGetFramework> TargetFrameworks => _results.Keys.OrderBy(e => e, new NuGetFrameworkSorter());

        public IEnumerable<IAnalyzerResult> Results => TargetFrameworks.Select(e => _results[e]);

        public int Count => _results.Count;

        public bool ContainsTargetFramework(NuGetFramework targetFramework) => _results.ContainsKey(targetFramework);

        public bool TryGetTargetFramework(NuGetFramework targetFramework, out IAnalyzerResult result) => _results.TryGetValue(targetFramework, out result);

        public IEnumerator<IAnalyzerResult> GetEnumerator() => Results.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}