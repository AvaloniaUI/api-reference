// This file is copied and edited from here: https://github.com/EWSoftware/SHFB 
// 
//===============================================================================================================
// System  : Sandcastle Tools Standard Presentation Styles
// Author  : Eric Woodruff  (Eric@EWoodruff.us)
// Note    : Copyright 2015-2022, Eric Woodruff, All rights reserved
//
// This file contains the presentation style definition for the markdown content presentation style
//
// This code is published under the Microsoft Public License (Ms-PL).  A copy of the license should be
// distributed with the code and can be found at the project website: https://GitHub.com/EWSoftware/SHFB.  This
// notice, the author's name, and all copyright notices must remain intact in all applications, documentation,
// and source files.
//
//    Date     Who  Comments
// ==============================================================================================================
// 04/02/2015  EFW  Created the code
// 02/27/2025  TU   Adjusted the code to support the MDX-format
//===============================================================================================================

using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Sandcastle.Core;
using Sandcastle.Core.PresentationStyle;
using Sandcastle.Core.Project;

namespace DocusaurusPresentationStyle.DocusaurusMarkdown
{
    /// <summary>
    /// This defines a presentation style used to generate markdown content (Docusaurus flavored)
    /// </summary>
    [PresentationStyleExport("DocusaurusMarkdown", "Markdown Content (Docusaurus compatible)", Version = AssemblyInfo.ProductVersion,
      Copyright = AssemblyInfo.Copyright, Description = "This generates markdown content (Docusaurus compatible)")]
    public sealed class DocusaurusMarkdownPresentationStyle : PresentationStyleSettings
    {
        /// <inheritdoc />
        public override string Location => ComponentUtilities.AssemblyFolder(Assembly.GetExecutingAssembly());

        /// <summary>
        /// Constructor
        /// </summary>
        public DocusaurusMarkdownPresentationStyle()
        {
            // The base path of the presentation style files relative to the assembly's location
            this.BasePath = "DocusaurusMarkdown";

            this.SupportedFormats = HelpFileFormats.Markdown;

            this.SupportsNamespaceGrouping = true;

            this.DocumentModelApplicator = new StandardDocumentModel();
            this.ApiTableOfContentsGenerator = new StandardApiTocGenerator();
            this.TopicTransformation = new DocusaurusMarkdownTransformation(ResolvePath);

            // If relative, these paths are relative to the base path
            this.BuildAssemblerConfiguration = @"Configuration\BuildAssembler.config";
        }

        /// <inheritdoc />
        /// <remarks>This presentation style uses the standard shared content and overrides a few items with
        /// Markdown specific values.</remarks>
        public override IEnumerable<string> ResourceItemFiles(string languageName)
        {
            string filePath = this.ResolvePath(@"..\Shared\Content"),
                fileSpec = "SharedContent_" + languageName + ".xml";

            if(!File.Exists(Path.Combine(filePath, fileSpec)))
                fileSpec = "SharedContent_en-US.xml";

            yield return Path.Combine(filePath, fileSpec);

            fileSpec = "Markdown_" + languageName + ".xml";

            if(!File.Exists(Path.Combine(filePath, fileSpec)))
                fileSpec = "Markdown_en-US.xml";

            yield return Path.Combine(filePath, fileSpec);

            foreach(string f in this.AdditionalResourceItemsFiles)
                yield return f;
        }
    }
}
