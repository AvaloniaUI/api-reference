using System;
using System.Xml.Linq;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;

namespace DocusaurusPresentationStyle.DocusaurusMarkdown.Elements;

public class MdxListElement : Element
{
    public MdxListElement(string name) : base(name)
    {
    }

    public override void Render(TopicTransformationCore? transformation, XElement? element)
    {
        if(transformation == null)
            throw new ArgumentNullException(nameof(transformation));

        if(element == null)
            throw new ArgumentNullException(nameof(element));

        if (transformation.CurrentElement.Name != "document")
        {
            transformation.CurrentElement.Add(" [...] ");
            return;
        }


        switch (element.Attribute("type")?.Value)
        {
            case "bullet":
            case "": 
            case null:
                transformation.CurrentElement.Add("\n");
                
                foreach (var node in element.Elements("item"))
                {
                    transformation.CurrentElement.Add("* ");
                    transformation.RenderChildElements(transformation.CurrentElement, [ node ]);
                    transformation.CurrentElement.Add("\n");
                }
                break;
            
            case "table":
                XElement row, td, th;
                var table = new XElement("table", "\n");

                foreach (var header in element.Elements("listheader"))
                {
                    row = new XElement("tr");
                    th = new XElement("th");
                    transformation.RenderChildElements(th, header.Elements("term"));
                    row.Add(th);
                    
                    th = new XElement("th");
                    transformation.RenderChildElements(th, header.Elements("description"));
                    row.Add(th);
                    
                    table.Add(row,"\n");
                }
                
                foreach (var item in element.Elements("item"))
                {
                    row = new XElement("tr");
                    td = new XElement("td");
                    transformation.RenderChildElements(td, item.Elements("term"));
                    row.Add(td);
                    
                    td = new XElement("td");
                    transformation.RenderChildElements(td, item.Elements("description"));
                    row.Add(td);
                    
                    table.Add(row,"\n");
                }
                
                transformation.CurrentElement.Add("\n\n", table, "\n\n");
                
                break;
                
            default:
                throw new ArgumentException($"Invalid element type {element.Attribute("type")?.Value}");
        }
    }
}