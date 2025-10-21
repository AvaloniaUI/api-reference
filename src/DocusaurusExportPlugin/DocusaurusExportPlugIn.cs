using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Xml.Linq;
using Sandcastle.Core.BuildEngine;
using Sandcastle.Core.PlugIn;

namespace DocusaurusExportPlugin
{
    /// <summary>
    /// A plugin to export docusaurus compliant mdx-files
    /// </summary>
    /// <remarks>The <c>HelpFileBuilderPlugInExportAttribute</c> is used to export your plug-in so that the help
    /// file builder finds it and can make use of it.  The example below shows the basic usage for a common
    /// plug-in.  Set the additional attribute values as needed:
    ///
    /// <list type="bullet">
    ///     <item>
    ///         <term>RunsInPartialBuild</term>
    ///         <description>Set this to true if your plug-in should run in partial builds used to generate
    /// reflection data for the API Filter editor dialog or namespace comments used for the Namespace Comments
    /// editor dialog.  Typically, this is left set to false.</description>
    ///     </item>
    /// </list>
    /// 
    /// Plug-ins are singletons in nature.  The composition container will create instances as needed and will
    /// dispose of them when the container is disposed of.</remarks>
    [HelpFileBuilderPlugInExport("DocusaurusExport", Version = AssemblyInfo.ProductVersion,
      Copyright = AssemblyInfo.Copyright, Description = "DocusaurusExport plug-in")]
    public sealed class DocusaurusExportPlugIn : IPlugIn
    {
        //=====================================================================

        private List<ExecutionPoint>? _executionPoints;

        private IBuildProcess? _builder;

        /// <summary>
        /// Gets a dictionary with the mapping between the AssemblyName and the nuget package
        /// </summary>
        /// <remarks>
        /// The setting is stored in the following file:
        /// <c>\src\ApiDocumentation\settings\AssemblyPackageMapping.json</c>
        /// </remarks>
        public static Dictionary<string, string>? AssemblyPackageMapping { get; private set; }

        //=====================================================================

        /// <summary>
        /// This read-only property returns a collection of execution points that define when the plug-in should
        /// be invoked during the build process.
        /// </summary>
        public IEnumerable<ExecutionPoint> ExecutionPoints
        {
            get
            {
                return _executionPoints ??= [
                    new ExecutionPoint(BuildStep.CompilingHelpFile, ExecutionBehaviors.InsteadOf)
                ];
            }
        }

        /// <summary>
        /// This method is used to initialize the plug-in at the start of the build process
        /// </summary>
        /// <param name="buildProcess">A reference to the current build process</param>
        /// <param name="configuration">The configuration data that the plug-in should use to initialize itself</param>
        public void Initialize(IBuildProcess buildProcess, XElement configuration)
        {
            _builder = buildProcess;

            var metadata = (HelpFileBuilderPlugInExportAttribute)this.GetType().GetCustomAttributes(
                typeof(HelpFileBuilderPlugInExportAttribute), false).First();

            AssemblyPackageMapping = JsonSerializer.Deserialize<Dictionary<string, string>>(
                File.ReadAllText(Path.Combine(buildProcess.ProjectFolder, "settings", "AssemblyPackageMapping.json")));
            
            _builder.ReportProgress("{0} Version {1}\r\n{2}", metadata.Id, metadata.Version, metadata.Copyright);
        }

        /// <summary>
        /// This method is used to execute the plug-in during the build process
        /// </summary>
        /// <param name="context">The current execution context</param>
        public void Execute(ExecutionContext context)
        {
            if (_builder is null) 
                throw new NullReferenceException(nameof(_builder));
            
            _builder.ReportProgress("In DocusaurusExport-PlugIn Execute() method");
            new DocusaurusContentGenerator(_builder).Execute();
            _builder.ReportProgress("DocusaurusExport-PlugIn execution finished");
        }

        //=====================================================================

        /// <summary>
        /// This handles garbage collection to ensure proper disposal of the plug-in if not done explicitly
        /// with <see cref="Dispose()"/>.
        /// </summary>
        ~DocusaurusExportPlugIn()
        {
            this.Dispose();
        }
        
        /// <summary>
        /// This implements the Dispose() interface to properly dispose of the plug-in object
        /// </summary>
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
