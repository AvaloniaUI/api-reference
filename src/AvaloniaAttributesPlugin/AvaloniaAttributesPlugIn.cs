using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Xml.XPath;
using Sandcastle.Core.BuildEngine;
using Sandcastle.Core.PlugIn;

namespace AvaloniaAttributes
{
    /// <summary>
    /// This plug-in helps to render Avalonia-specific Attributes like "Unstable" and "NotClientImplementable". 
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
    [HelpFileBuilderPlugInExport("AvaloniaAttributes", Version = AssemblyInfo.ProductVersion,
        Copyright = AssemblyInfo.Copyright, Description = "AvaloniaAttributes plug-in")]
    public sealed class AvaloniaAttributesPlugIn : IPlugIn
    {
        //=====================================================================

        private List<ExecutionPoint>? _executionPoints;

        private IBuildProcess? _builder;

        //=====================================================================

        /// <summary>
        /// This read-only property returns a collection of execution points that define when the plug-in should
        /// be invoked during the build process.
        /// </summary>
        public IEnumerable<ExecutionPoint> ExecutionPoints
        {
            get
            {
                return _executionPoints ??= new List<ExecutionPoint>
                {
                    new ExecutionPoint(BuildStep.GenerateReflectionInfo, ExecutionBehaviors.Before),
                    new ExecutionPoint(BuildStep.ApplyDocumentModel, ExecutionBehaviors.Before),
                };
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

            _builder.ReportProgress("{0} Version {1}\r\n{2}", metadata.Id, metadata.Version, metadata.Copyright);
        }

        /// <summary>
        /// This method is used to execute the plug-in during the build process
        /// </summary>
        /// <param name="context">The current execution context</param>
        public void Execute(ExecutionContext context)
        {
            switch (context.BuildStep)
            {
                case BuildStep.GenerateReflectionInfo:
                    AddAvaloniaAttributesToReflectionInfo();
                    break;
                case BuildStep.ApplyDocumentModel:
                    FilterPrivateApi();
                    break;
            }
        }

        /// <summary>
        /// Adds the AvaloniaAttributes to the reflection info file 
        /// </summary>
        /// <exception cref="NullReferenceException">If no <see cref="_builder"/> is present</exception>
        private void AddAvaloniaAttributesToReflectionInfo()
        {
            if (_builder is null) 
                throw new NullReferenceException(nameof(_builder));
            
            _builder.ReportProgress("Adding PrivateApi-Attribute");

            string configFile = Path.Combine(_builder.WorkingFolder, "MRefBuilder.config");

            var config = XDocument.Load(configFile);
            var currentFilter = config.Root?.Descendants("attributeFilter").FirstOrDefault();

            currentFilter?.Add(
                new XElement("namespace", new XAttribute("name", "Avalonia.Metadata"),
                new XAttribute("expose", "true"),
                new XElement("type", new XAttribute("name", "PrivateApiAttribute"),
                    new XAttribute("expose", "true"))));

            config.Save(configFile);
        }

        private void FilterPrivateApi()
        {
            // Copy idea of the `XPathReflectionFileFilterPlugIn`, but with also removing all items having a specific attribute
            // see: https://ewsoftware.github.io/SHFB/html/d8ae7c4a-6112-efe6-a28f-27a84d943034.htm 
            
            if (_builder is null) 
                throw new NullReferenceException(nameof(_builder));
            
            XDocument refInfo = XDocument.Load(_builder.ReflectionInfoFilename);

            int counter = 0, counterSum = 0;

            // Remove all Nodes whose id ends with "Impl"
            // Finds any text that ends with Impl or contains "Impl" - followed by upper-case-char.
            // The RegEx is needed here since there may be classes that has Impl in it's name but doesn't match to this rule,
            // for example "Implicit" 
            
            Regex regex = new Regex(@"Impl([A-Z.,)]|\b)", RegexOptions.Compiled);  

            var nodes = refInfo.XPathSelectElements("/reflection/apis/api[contains(@id, 'Impl')]").ToArray();

            foreach (var node in nodes)
            {
                if (node.Attribute("id")?.Value is { } attr && regex.IsMatch(attr) && node.Parent != null)
                {
                    node.Remove();
                }

                counter++;
            }

            _builder.ReportProgress("    Removed {0} for expression '{1}'", counter,
                "/reflection/apis/api[contains(@id, 'Impl')]");

            counterSum += counter;
            counter = 0;

            // -------------------------------------------------------------------------------------------------------------------
            // Remove all Nodes whose api-name ends with "Impl", this time for elements.

            nodes = refInfo.XPathSelectElements("/reflection/apis/api/elements/element[contains(@api, 'Impl')]")
                .ToArray();

            // Remove all Nodes whose name ends with "impl"
            foreach (var node in nodes)
            {
                if (node.Attribute("api")?.Value is { } attr && regex.IsMatch(attr) && node.Parent != null)
                {
                    node.Remove();
                }

                counter++;
            }

            _builder.ReportProgress("    Removed {0} for expression '{1}'", counter,
                "/reflection/apis/api/elements/element[contains(@api, 'Impl')]");

            counterSum += counter;
            counter = 0;

            // -------------------------------------------------------------------------------------------------------------------
            // Remove all Nodes with PrivateApi-Attribute

            nodes = refInfo
                .XPathSelectElements(
                    "/reflection/apis/api[attributes/attribute/type/@api='T:Avalonia.Metadata.PrivateApiAttribute']")
                .ToArray();

            HashSet<string> childrenToRemove = new HashSet<string>();

            // Remove all Nodes with Attribute "PrivateApi"
            foreach (var node in nodes)
            {
                var children = node.Descendants("element")
                    .Select(x => x.Attribute("api")?.Value ?? string.Empty)
                    .Where(x => x?.Contains("Avalonia") ?? false);

                foreach (var child in children)
                {
                    childrenToRemove.Add(child);
                }

                if (node.Parent != null)
                {
                    node.Remove();
                    childrenToRemove.Add(node.Attribute("id")?.Value ?? string.Empty);
                    counter++;
                }
            }

            _builder.ReportProgress("    Removed {0} for expression '{1}'", counter,
                "/reflection/apis/api[attributes/attribute/type/@api='T:Avalonia.Metadata.PrivateApiAttribute']");

            counterSum += counter;
            counter = 0;

            // collect all elements from types to remove
            foreach (var item in childrenToRemove.Where(x => x.StartsWith("T:")).ToArray())
            {
                nodes = refInfo
                    .XPathSelectElements(
                        $"/reflection/apis/api[contains(@id, '{item}')]").ToArray();

                foreach (var node in nodes)
                {
                    var children = node.Descendants("element")
                        .Select(x => x.Attribute("api")?.Value ?? string.Empty)
                        .Where(x => x?.Contains("Avalonia") ?? false);

                    childrenToRemove.Add(node.Attribute("id")?.Value ?? string.Empty);

                    foreach (var child in children)
                    {
                        childrenToRemove.Add(child);
                    }
                }
            }

            _builder.ReportProgress("    Removed {0} for expression '{1}'", counter,
                "/reflection/apis/api[contains(@id, '{item}')]");

            counterSum += counter;
            counter = 0;

            foreach (var child in childrenToRemove)
            {
                nodes = refInfo
                    .XPathSelectElements(
                        $"/reflection/apis/api/elements/element[contains(@api, '{child}')]").ToArray();

                foreach (var node in nodes)
                {
                    if (node.Parent != null)
                    {
                        node.Remove();
                        counter++;
                    }
                }

                nodes = refInfo
                    .XPathSelectElements(
                        $"/reflection/apis/api[contains(@id, '{child}')]").ToArray();

                foreach (var node in nodes)
                {
                    if (node.Parent != null)
                    {
                        node.Remove();
                        counter++;
                    }
                }
            }

            _builder.ReportProgress("    Removed {0} for expression '{1}'", counter,
                "/reflection/apis/api[contains(@id, '{item}')]");

            counterSum += counter;

            _builder.ReportProgress("    Removed {0} items in total", counterSum);

            refInfo.Save(_builder.ReflectionInfoFilename);
        }

        //=====================================================================

        /// <summary>
        /// This handles garbage collection to ensure proper disposal of the plug-in if not done explicitly
        /// with <see cref="Dispose()"/>.
        /// </summary>
        ~AvaloniaAttributesPlugIn()
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