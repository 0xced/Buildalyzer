using System.Collections.Generic;
using NuGet.Frameworks;

namespace Buildalyzer
{
    public interface IAnalyzerResults : IEnumerable<IAnalyzerResult>
    {
        IAnalyzerResult this[NuGetFramework targetFramework] { get; }

        int Count { get; }

        bool OverallSuccess { get; }

        IEnumerable<IAnalyzerResult> Results { get; }

        IEnumerable<NuGetFramework> TargetFrameworks { get; }

        bool ContainsTargetFramework(NuGetFramework targetFramework);

        bool TryGetTargetFramework(NuGetFramework targetFramework, out IAnalyzerResult result);
    }
}