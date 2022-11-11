using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NuGet.Frameworks;

namespace Buildalyzer.Construction
{
    /// <summary>
    /// Encapsulates an MSBuild project file and provides some information about it's format.
    /// This class only parses the existing XML and does not perform any evaluation.
    /// </summary>
    public class ProjectFile : IProjectFile
    {
        /// <summary>
        /// These imports are known to require a .NET Framework host and build tools.
        /// </summary>
        public static readonly string[] ImportsThatRequireNetFramework = new string[]
        {
            "Microsoft.Portable.CSharp.targets",
            "Microsoft.Windows.UI.Xaml.CSharp.targets"
        };

        private readonly XDocument _document;
        private readonly XElement _projectElement;

        private NuGetFramework[] _targetFrameworks = null;

        // The project file path should already be normalized
        internal ProjectFile(string path)
        {
            Path = path;
            Name = new FileInfo(path).Name;
            _document = XDocument.Load(path);

            // Get the project element
            _projectElement = _document.GetDescendants(ProjectFileNames.Project).FirstOrDefault();
            if (_projectElement == null)
            {
                throw new ArgumentException("Unrecognized project file format");
            }
        }

        /// <inheritdoc />
        public string Path { get; }

        /// <inheritdoc />
        public string Name { get; }

        /// <inheritdoc />
        public NuGetFramework[] TargetFrameworks => _targetFrameworks ??= GetTargetFrameworks(
            _projectElement.GetDescendants(ProjectFileNames.TargetFrameworks).Select(x => x.Value),
            _projectElement.GetDescendants(ProjectFileNames.TargetFramework).Select(x => x.Value),
            _projectElement.GetDescendants(ProjectFileNames.TargetFrameworkVersion)
                .Select(x => (TargetFrameworkIdentifier: x.Parent.GetDescendants(ProjectFileNames.TargetFrameworkIdentifier).FirstOrDefault()?.Value ?? ".NETFramework", TargetFrameworkVersion: x.Value)));

        /// <inheritdoc />
        public bool UsesSdk =>
            _projectElement.GetAttributeValue(ProjectFileNames.Sdk) != null
                || _projectElement.GetDescendants(ProjectFileNames.Import).Any(x => x.GetAttributeValue(ProjectFileNames.Sdk) != null);

        /// <inheritdoc />
        public bool RequiresNetFramework =>
            _projectElement.GetDescendants(ProjectFileNames.Import).Any(x => ImportsThatRequireNetFramework.Any(i => x.GetAttributeValue(ProjectFileNames.Project)?.EndsWith(i, StringComparison.OrdinalIgnoreCase) ?? false))
            || _projectElement.GetDescendants(ProjectFileNames.LanguageTargets).Any(x => ImportsThatRequireNetFramework.Any(i => x.Value.EndsWith(i, StringComparison.OrdinalIgnoreCase)))
            || ToolsVersion != null;

        /// <inheritdoc />
        public bool IsMultiTargeted => _projectElement.GetDescendants(ProjectFileNames.TargetFrameworks).Any();

        /// <inheritdoc />
        public string OutputType => _projectElement.GetDescendants(ProjectFileNames.OutputType).FirstOrDefault()?.Value;

        /// <inheritdoc />
        public bool ContainsPackageReferences => _projectElement.GetDescendants(ProjectFileNames.PackageReference).Any();

        /// <inheritdoc />
        public IReadOnlyList<IPackageReference> PackageReferences => _projectElement.GetDescendants(ProjectFileNames.PackageReference).Select(s => new PackageReference(s)).ToList();

        /// <inheritdoc />
        public string ToolsVersion => _projectElement.GetAttributeValue(ProjectFileNames.ToolsVersion);

        internal static NuGetFramework[] GetTargetFrameworks(
            IEnumerable<string> targetFrameworksValues,
            IEnumerable<string> targetFrameworkValues,
            IEnumerable<(string TargetFrameworkIdentifier, string TargetFrameworkVersion)> targetFrameworkIdentifierAndVersionValues)
        {
            // Use TargetFrameworks and/or TargetFramework if either were found
            IEnumerable<string> allTargetFrameworks = null;
            if (targetFrameworksValues != null)
            {
                allTargetFrameworks = targetFrameworksValues
                    .Where(x => x != null)
                    .SelectMany(x => x.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(v => v.Trim()));
            }
            if (targetFrameworkValues != null)
            {
                allTargetFrameworks = allTargetFrameworks == null
                    ? targetFrameworkValues.Where(x => x != null).Select(x => x.Trim())
                    : allTargetFrameworks.Concat(targetFrameworkValues.Where(x => x != null).Select(x => x.Trim()));
            }
            if (allTargetFrameworks != null)
            {
                string[] distinctTargetFrameworks = allTargetFrameworks.Distinct().ToArray();
                if (distinctTargetFrameworks.Length > 0)
                {
                    // Only return if we actually found any
                    return distinctTargetFrameworks.Select(NuGetFramework.ParseFolder).ToArray();
                }
            }

            return targetFrameworkIdentifierAndVersionValues?
                .Where(value => value.TargetFrameworkIdentifier != null && value.TargetFrameworkVersion != null)
                .Select(value => new NuGetFramework(value.TargetFrameworkIdentifier, Version.Parse(value.TargetFrameworkVersion.TrimStart('v'))))
                .ToArray() ?? Array.Empty<NuGetFramework>();
        }
    }
}