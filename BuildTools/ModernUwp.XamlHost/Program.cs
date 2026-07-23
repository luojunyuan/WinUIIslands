using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;
using Microsoft.Build.Logging;

namespace ModernUwp.XamlHost
{
    internal static class Program
    {
        private static readonly XNamespace MsBuildNamespace = "http://schemas.microsoft.com/developer/msbuild/2003";

        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Usage: ModernUwp.XamlHost.exe <request-file>");
                return 2;
            }

            try
            {
                XamlRequest request = XamlRequest.Load(args[0]);
                Validate(request);
                PrepareOutputFiles(request);

                string projectPath = WriteInternalProject(request);
                try
                {
                    BuildParameters parameters = new BuildParameters
                    {
                        EnableNodeReuse = false,
                        MaxNodeCount = 1,
                        Loggers = new ILogger[] { new ConsoleLogger(LoggerVerbosity.Quiet) }
                    };

                    BuildRequestData buildRequest = new BuildRequestData(
                        projectPath,
                        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                        "4.0",
                        new[] { "CompileXaml" },
                        null);

                    BuildResult result = BuildManager.DefaultBuildManager.Build(parameters, buildRequest);
                    WriteAudit(request, GetAuditProjectPath(request), result.OverallResult.ToString());

                    if (result.OverallResult != BuildResultCode.Success)
                    {
                        return 1;
                    }

                    EnsureResultFile(request.ResultCodeFile);
                    EnsureResultFile(request.ResultXamlFile);
                    EnsureResultFile(request.ResultXbfFile);
                    RejectVisualStudioAssemblies();
                    return 0;
                }
                finally
                {
                    DeleteIfExists(projectPath);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("[ModernUwp.XamlHost] {0}", exception);
                return 1;
            }
        }

        private static void Validate(XamlRequest request)
        {
            if (request.Pass != 1 && request.Pass != 2)
            {
                throw new InvalidDataException("The XAML pass must be 1 or 2.");
            }

            RequireFile(request.RequestPath, "request file");
            RequireFile(request.TaskAssembly, "Windows SDK XAML compiler task assembly");
            RequireFile(request.ProjectPath, "UWP project");

            if (request.TaskAssembly.IndexOf("\\Microsoft Visual Studio\\", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                throw new InvalidDataException("The XAML task assembly must come from the Windows SDK, not Visual Studio.");
            }

            if (request.Pass == 2)
            {
                RequireFile(request.LocalAssembly, "XAML intermediate assembly");
            }

            foreach (XamlItem item in request.Pages.Concat(request.Applications))
            {
                RequireFile(item.FullPath, "XAML input");
            }

            Directory.CreateDirectory(request.OutputPath);
            Directory.CreateDirectory(Path.GetDirectoryName(request.ResultCodeFile));
            Directory.CreateDirectory(Path.GetDirectoryName(request.AuditFile));
        }

        private static void RequireFile(string path, string description)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                throw new FileNotFoundException("Cannot find the " + description + ".", path);
            }
        }

        private static void PrepareOutputFiles(XamlRequest request)
        {
            DeleteIfExists(request.ResultCodeFile);
            DeleteIfExists(request.ResultXamlFile);
            DeleteIfExists(request.ResultXbfFile);

            if (request.Pass == 1)
            {
                DeleteIfExists(request.AuditFile);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (!string.IsNullOrWhiteSpace(path) && File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static string WriteInternalProject(XamlRequest request)
        {
            XElement project = new XElement(
                MsBuildNamespace + "Project",
                new XAttribute("ToolsVersion", "4.0"),
                new XAttribute("DefaultTargets", "CompileXaml"));

            project.Add(new XElement(
                MsBuildNamespace + "UsingTask",
                new XAttribute("TaskName", "Microsoft.Windows.UI.Xaml.Build.Tasks.CompileXaml"),
                new XAttribute("AssemblyFile", request.TaskAssembly)));

            XElement items = new XElement(MsBuildNamespace + "ItemGroup");
            AddItems(items, "ReferencePath", request.References);
            AddItems(items, "ReferenceAssemblyPath", request.ReferenceAssemblyPaths);
            AddItems(items, "FingerprintIgnorePath", request.FingerprintIgnorePaths);
            AddXamlItems(items, "Page", request.Pages);
            AddXamlItems(items, "ApplicationDefinition", request.Applications);

            if (request.Pass == 2)
            {
                AddItems(items, "LocalAssembly", new[] { request.LocalAssembly });
            }

            project.Add(items);

            XElement target = new XElement(MsBuildNamespace + "Target", new XAttribute("Name", "CompileXaml"));
            target.Add(new XElement(
                MsBuildNamespace + "MakeDir",
                new XAttribute("Directories", request.OutputPath + "intermediatexaml\\")));

            XElement compile = new XElement(
                MsBuildNamespace + "CompileXaml",
                Attribute("LanguageSourceExtension", ".cs"),
                Attribute("Language", "C#"),
                Attribute("RootNamespace", request.RootNamespace),
                Attribute("XamlPages", "@(Page)"),
                Attribute("XamlApplications", "@(ApplicationDefinition)"),
                Attribute("PriIndexName", request.PriIndexName),
                Attribute("ProjectName", request.ProjectName),
                Attribute("IsPass1", request.Pass == 1 ? "true" : "false"),
                Attribute("ProjectPath", request.ProjectPath),
                Attribute("OutputPath", request.OutputPath),
                Attribute("OutputType", request.OutputType),
                Attribute("ReferenceAssemblyPaths", "@(ReferenceAssemblyPath)"),
                Attribute("ReferenceAssemblies", "@(ReferencePath)"),
                Attribute("ForceSharedStateShutdown", "false"),
                Attribute("CompileMode", request.Pass == 1 ? "RealBuildPass1" : "RealBuildPass2"),
                Attribute("XAMLFingerprint", "true"),
                Attribute("FingerprintIgnorePaths", "@(FingerprintIgnorePath)"),
                Attribute("VCInstallDir", request.WindowsSdkPath),
                Attribute("SavedStateFile", request.SavedStateFile),
                Attribute("TargetPlatformMinVersion", request.TargetPlatformMinVersion),
                Attribute("WindowsSdkPath", request.WindowsSdkPath),
                Attribute("XamlResourceMapName", request.XamlResourceMapName),
                Attribute("XamlComponentResourceLocation", request.XamlComponentResourceLocation),
                Attribute("EnableTypeInfoReflection", "false"),
                Attribute("EnableXBindDiagnostics", "true"),
                Attribute("UsingCsWinRT", "true"),
                Attribute("ReduceGeneratedXamlTypeStaticInitializers", "true"),
                Attribute("EnableRequiredAndInitOnlyFeatureSupport", "true"),
                Attribute("EnableDefaultValidationContextGeneration", "true"));

            if (request.Pass == 2)
            {
                compile.Add(
                    Attribute("DisableXbfGeneration", "false"),
                    Attribute("DisableXbfLineInfo", "false"),
                    Attribute("LocalAssembly", "@(LocalAssembly)"),
                    Attribute("PlatformXmlDir", request.PlatformXmlDir));
            }

            compile.Add(
                Output("GeneratedCodeFiles", "GeneratedCodeFile"),
                Output("GeneratedXamlFiles", "GeneratedXamlFile"),
                Output("GeneratedXbfFiles", "GeneratedXbfFile"));
            target.Add(compile);
            target.Add(WriteLines(request.ResultCodeFile, "GeneratedCodeFile"));
            target.Add(WriteLines(request.ResultXamlFile, "GeneratedXamlFile"));
            target.Add(WriteLines(request.ResultXbfFile, "GeneratedXbfFile"));
            project.Add(target);

            XDocument document = new XDocument(new XDeclaration("1.0", "utf-8", null), project);
            string auditProjectPath = GetAuditProjectPath(request);
            string internalProjectPath = Path.Combine(
                Path.GetDirectoryName(request.RequestPath),
                ".ModernUwp.XamlHost." + request.Pass + "." + Guid.NewGuid().ToString("N") + ".proj");
            SaveProject(document, auditProjectPath);
            SaveProject(document, internalProjectPath);
            return internalProjectPath;
        }

        private static string GetAuditProjectPath(XamlRequest request)
        {
            return Path.Combine(Path.GetDirectoryName(request.RequestPath), "pass" + request.Pass + ".proj");
        }

        private static void SaveProject(XDocument document, string path)
        {
            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace
            };

            using (XmlWriter writer = XmlWriter.Create(path, settings))
            {
                document.Save(writer);
            }
        }

        private static XAttribute Attribute(string name, string value)
        {
            return new XAttribute(name, value ?? string.Empty);
        }

        private static XElement Output(string taskParameter, string itemName)
        {
            return new XElement(
                MsBuildNamespace + "Output",
                new XAttribute("TaskParameter", taskParameter),
                new XAttribute("ItemName", itemName));
        }

        private static XElement WriteLines(string path, string itemName)
        {
            return new XElement(
                MsBuildNamespace + "WriteLinesToFile",
                new XAttribute("File", path),
                new XAttribute("Lines", "@(" + itemName + "->'%(FullPath)')"),
                new XAttribute("Overwrite", "true"),
                new XAttribute("Encoding", "UTF-8"));
        }

        private static void AddItems(XElement itemGroup, string itemName, IEnumerable<string> paths)
        {
            foreach (string path in paths.Where(path => !string.IsNullOrWhiteSpace(path)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                itemGroup.Add(new XElement(MsBuildNamespace + itemName, new XAttribute("Include", path)));
            }
        }

        private static void AddXamlItems(XElement itemGroup, string itemName, IEnumerable<XamlItem> items)
        {
            foreach (XamlItem xamlItem in items.GroupBy(item => item.Identity, StringComparer.OrdinalIgnoreCase).Select(group => group.First()))
            {
                XElement item = new XElement(
                    MsBuildNamespace + itemName,
                    new XAttribute("Include", xamlItem.Identity),
                    new XElement(MsBuildNamespace + "Generator", "MSBuild:Compile"),
                    new XElement(MsBuildNamespace + "SubType", "Designer"),
                    new XElement(MsBuildNamespace + "XamlRuntime", "UAP"));

                if (!string.IsNullOrWhiteSpace(xamlItem.Link))
                {
                    item.Add(new XElement(MsBuildNamespace + "Link", xamlItem.Link));
                }

                itemGroup.Add(item);
            }
        }

        private static void EnsureResultFile(string path)
        {
            if (File.Exists(path))
            {
                return;
            }

            using (StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                writer.NewLine = "\r\n";
            }
        }

        private static void WriteAudit(XamlRequest request, string projectPath, string result)
        {
            using (StreamWriter writer = new StreamWriter(request.AuditFile, request.Pass != 1, new UTF8Encoding(false)))
            {
                writer.NewLine = "\r\n";
                writer.WriteLine("Pass={0}", request.Pass);
                writer.WriteLine("Result={0}", result);
                writer.WriteLine("Host={0}", typeof(Program).Assembly.Location);
                writer.WriteLine("Runtime={0}", Environment.Version);
                writer.WriteLine("TaskAssembly={0}", request.TaskAssembly);
                writer.WriteLine("InternalProject={0}", projectPath);

                foreach (string assembly in LoadedAssemblyLocations())
                {
                    writer.WriteLine("LoadedAssembly={0}", assembly);
                }
            }
        }

        private static string[] LoadedAssemblyLocations()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Select(assembly =>
                {
                    try
                    {
                        return assembly.IsDynamic ? string.Empty : assembly.Location;
                    }
                    catch (NotSupportedException)
                    {
                        return string.Empty;
                    }
                })
                .Where(location => !string.IsNullOrWhiteSpace(location))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(location => location, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        private static void RejectVisualStudioAssemblies()
        {
            string visualStudioAssembly = LoadedAssemblyLocations().FirstOrDefault(
                location => location.IndexOf("\\Microsoft Visual Studio\\", StringComparison.OrdinalIgnoreCase) >= 0);

            if (visualStudioAssembly != null)
            {
                throw new InvalidOperationException("The XAML host loaded a Visual Studio assembly: " + visualStudioAssembly);
            }
        }
    }

    internal sealed class XamlRequest
    {
        private readonly Dictionary<string, string> values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private XamlRequest(string requestPath)
        {
            RequestPath = Path.GetFullPath(requestPath);
        }

        public string RequestPath { get; private set; }
        public int Pass { get { return int.Parse(Get("pass")); } }
        public string TaskAssembly { get { return FullPath(Get("taskAssembly")); } }
        public string ProjectPath { get { return FullPath(Get("projectPath")); } }
        public string OutputPath { get { return DirectoryPath(Get("outputPath")); } }
        public string RootNamespace { get { return Get("rootNamespace"); } }
        public string ProjectName { get { return Get("projectName"); } }
        public string OutputType { get { return Get("outputType"); } }
        public string PriIndexName { get { return GetOptional("priIndexName"); } }
        public string TargetPlatformMinVersion { get { return Get("targetPlatformMinVersion"); } }
        public string WindowsSdkPath { get { return DirectoryPath(Get("windowsSdkPath")); } }
        public string PlatformXmlDir { get { return DirectoryPath(Get("platformXmlDir")); } }
        public string SavedStateFile { get { return FullPath(Get("savedStateFile")); } }
        public string LocalAssembly { get { return FullPath(GetOptional("localAssembly")); } }
        public string XamlResourceMapName { get { return GetOptional("xamlResourceMapName"); } }
        public string XamlComponentResourceLocation { get { return GetOptional("xamlComponentResourceLocation"); } }
        public string ResultCodeFile { get { return FullPath(Get("resultCodeFile")); } }
        public string ResultXamlFile { get { return FullPath(Get("resultXamlFile")); } }
        public string ResultXbfFile { get { return FullPath(Get("resultXbfFile")); } }
        public string AuditFile { get { return FullPath(Get("auditFile")); } }
        public List<string> References { get; } = new List<string>();
        public List<string> ReferenceAssemblyPaths { get; } = new List<string>();
        public List<string> FingerprintIgnorePaths { get; } = new List<string>();
        public List<XamlItem> Pages { get; } = new List<XamlItem>();
        public List<XamlItem> Applications { get; } = new List<XamlItem>();

        public static XamlRequest Load(string requestPath)
        {
            XamlRequest request = new XamlRequest(requestPath);

            foreach (string rawLine in File.ReadAllLines(request.RequestPath))
            {
                string line = rawLine.TrimEnd();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                int separator = line.IndexOf('|');
                if (separator <= 0)
                {
                    throw new InvalidDataException("Invalid request line: " + line);
                }

                string key = line.Substring(0, separator);
                string value = line.Substring(separator + 1);
                request.Add(key, value);
            }

            return request;
        }

        private void Add(string key, string value)
        {
            switch (key.ToLowerInvariant())
            {
                case "reference":
                    References.Add(FullPath(value));
                    break;
                case "referenceassemblypath":
                    ReferenceAssemblyPaths.Add(DirectoryPath(value));
                    break;
                case "fingerprintignorepath":
                    FingerprintIgnorePaths.Add(DirectoryPath(value));
                    break;
                case "page":
                    Pages.Add(ParseXamlItem(value));
                    break;
                case "application":
                    Applications.Add(ParseXamlItem(value));
                    break;
                default:
                    values[key] = value;
                    break;
            }
        }

        private XamlItem ParseXamlItem(string value)
        {
            string[] parts = value.Split(new[] { '|' }, 3);
            string identity = parts[0];
            string link = parts.Length >= 2 ? parts[1] : string.Empty;
            string fullPath = parts.Length == 3 && !string.IsNullOrWhiteSpace(parts[2])
                ? FullPath(parts[2])
                : FullPath(Path.Combine(Path.GetDirectoryName(ProjectPath), identity));
            return new XamlItem(identity, link, fullPath);
        }

        private string Get(string key)
        {
            string value;
            if (!values.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidDataException("Missing request value: " + key);
            }

            return value;
        }

        private string GetOptional(string key)
        {
            string value;
            return values.TryGetValue(key, out value) ? value : string.Empty;
        }

        private string FullPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            return Path.GetFullPath(path);
        }

        private string DirectoryPath(string path)
        {
            string fullPath = FullPath(path);
            return fullPath.EndsWith("\\", StringComparison.Ordinal) ? fullPath : fullPath + "\\";
        }

    }

    internal sealed class XamlItem
    {
        public XamlItem(string identity, string link, string fullPath)
        {
            Identity = identity;
            Link = link;
            FullPath = fullPath;
        }

        public string Identity { get; private set; }
        public string Link { get; private set; }
        public string FullPath { get; private set; }
    }
}

