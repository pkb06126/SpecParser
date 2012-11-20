using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Xml.Linq;

namespace specParser
{
    public class Program
    {
        static void Main(string[] args)
        {
            string inputFile;
            //try
            //{
            //    inputFile = args[0];
            //}
            //catch (Exception)
            //{
            //    Console.WriteLine("You must specify an xml document");
            //    return;
            //}
            inputFile = "results.xml";

            var xdoc = XDocument.Load(inputFile);

            File.WriteAllText("../../../SpecOutput.html",
                              WriteToHtml(DefineStructure(Parse(xdoc))).ToString());

            Console.ReadKey();
        }

        public static StringWriter WriteToHtml(List<NameSp> nameSps)
        {
            var stringwriter = new StringWriter();
            var writer = new HtmlTextWriter(stringwriter);
            
            writer.Write("<link rel=\"stylesheet\" href=\"http://code.jquery.com/ui/1.9.1/themes/base/jquery-ui.css\" />");
            writer.Write("<script src=\"http://code.jquery.com/jquery-1.8.2.js\"></script>");
            writer.Write("<script src=\"http://code.jquery.com/ui/1.9.1/jquery-ui.js\"></script>");
            writer.Write("<link rel=\"stylesheet\" href=\"style.css\" />");

            writer.Write("<script type='text/javascript' src='scripts/jquery.nestedAccordion.js'></script>");

            writer.RenderBeginTag(HtmlTextWriterTag.Script);
            writer.Write(" $(function() {$(\"#accordion-1\").accordion();}); ");
            writer.RenderEndTag();

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "accordion-1");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);
                writer.AddAttribute(HtmlTextWriterAttribute.Class, "accordion");

                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                foreach (var nameSp in nameSps)
                {
                    writer.RenderBeginTag(HtmlTextWriterTag.Li);
                    writer.Write(nameSp.Name);
                        writer.AddAttribute(HtmlTextWriterAttribute.Class, "accordion");
                        writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                            foreach (var concern in nameSp.Concerns)
                            {
                                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                                writer.Write(concern.Name);
                                
                                writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                                foreach (var context in concern.Contexts)
                                {
                                    writer.RenderBeginTag(HtmlTextWriterTag.Li);
                                    writer.Write(context.Name);
                                    
                                    writer.RenderBeginTag(HtmlTextWriterTag.Ul);
                                    foreach (var spec in context.Specs)
                                    {
                                        writer.RenderBeginTag(HtmlTextWriterTag.Li);
                                        writer.Write(spec.Name);
                                        writer.RenderEndTag();
                                        writer.AddStyleAttribute(HtmlTextWriterStyle.Color, "Red");
                                        writer.RenderBeginTag(HtmlTextWriterTag.Li);
                                        writer.Write(spec.Status);
                                        writer.RenderEndTag();
                                    }
                                    writer.RenderEndTag();
                                    writer.RenderEndTag();
                                }
                                writer.RenderEndTag();
                                writer.RenderEndTag();
                            }
                        writer.RenderEndTag();
                    writer.RenderEndTag();
                }
                writer.RenderEndTag();
            writer.RenderEndTag();

            return stringwriter;
        }
      
        public static List<Specification> Parse(XDocument xdoc)
        {
            var concernElement = xdoc.Descendants("concern");

            var specifications = (from c in concernElement
                                      let concernName = (string)c.Attribute("name")
                                      let contextElement = c.Elements("context")
                                          from d in contextElement
                                          let typeName = (string)d.Attribute("type-name")
                                          let contextName = (string)d.Attribute("name")
                                          let specificationElement = d.Elements("specification")
                                              from e in specificationElement
                                              let specificationName = (string)e.Attribute("name")
                                              let specificationStatus = (string)e.Attribute("status")
                                  
                                  select new Specification
                                  {
                                      Concern = concernName,
                                      Context = contextName,
                                      Spec = specificationName,
                                      Status = specificationStatus,
                                      Namesp = (typeName.Split('+')[0]).Split('.')[3]
                                  }).ToList();

            return specifications;
        }

        public static List<NameSp> DefineStructure(List<Specification> specifications)
        {
            var nameSps = new List<NameSp>();
           
            foreach (var s in specifications)
            {
                var exists = false;
                foreach (var n in nameSps.Where(n => n.Name.Equals(s.Namesp)))
                    exists = true;
                
                if (!exists)
                    nameSps.Add(new NameSp { Name = s.Namesp });
            }

            //get concerns
            foreach (var nameSp in nameSps)
            {
                foreach (var specification in specifications)
                {
                    if (nameSp.Name.Equals(specification.Namesp))
                    {
                        var exists = false;
                        foreach (var c in nameSp.Concerns.Where(c => c.Name.Equals(specification.Concern)))
                            exists = true;
                        if (!exists)
                            nameSp.Concerns.Add(new Concern {Name = specification.Concern});
                    }
                }
            }

            //get contexts
            foreach (var nameSp in nameSps)
            {
                foreach (var sp in nameSp.Concerns)
                {
                    foreach (var specification in specifications)
                    {
                        if(nameSp.Name.Equals(specification.Namesp) && sp.Name.Equals(specification.Concern))
                        {
                            var exists = false;
                            foreach (var c in sp.Contexts.Where(c => c.Name.Equals(specification.Context)))
                                exists = true;
                            if(!exists)
                                sp.Contexts.Add(new Context{Name = specification.Context});
                        }
                    }
                }
            }

            //get specs
            foreach (var nameSp in nameSps)
            {
                foreach (var sp in nameSp.Concerns)
                {
                    foreach (var cnt in sp.Contexts)
                    {
                        foreach (var specification in specifications)
                        {
                            if (nameSp.Name.Equals(specification.Namesp) && sp.Name.Equals(specification.Concern) && cnt.Name.Equals(specification.Context))
                            {
                                var exists = false;
                                foreach (var c in cnt.Specs.Where(c => c.Name.Equals(specification.Spec)))
                                    exists = true;
                            if(!exists)
                                cnt.Specs.Add(new Spec{Name = specification.Spec, Status = specification.Status});
                            }
                        }
                    }
                }
            }

            //sort
            nameSps.Sort((s1, s2) => String.CompareOrdinal(s1.Name, s2.Name));
            foreach (var n in nameSps)
            {
                Console.WriteLine(n.Name);
                n.Concerns.Sort((s1, s2) => String.CompareOrdinal(s1.Name, s2.Name));
                foreach (var concern in n.Concerns)
                {
                    Console.WriteLine(concern.Name);
                    concern.Contexts.Sort((s1, s2) => String.CompareOrdinal(s1.Name, s2.Name));
                    foreach (var context in concern.Contexts)
                    {
                        Console.WriteLine(context.Name);
                        context.Specs.Sort((s1, s2) => String.CompareOrdinal(s1.Name, s2.Name));
                        foreach (var spec in context.Specs)
                        {
                            Console.WriteLine(spec.Name);
                            Console.WriteLine(spec.Status);
                        }
                       
                    }
                }
            }

            return nameSps;
        }

    }

    public class Specification
    {
        public string Namesp; //DocumentsTests.DocumentDetailsFormViewModelTests.Specifications.CloseSpecifications
        public string Concern; //Close Is Visible
        public string Context; //when selected document status equals any
        public string Spec; //IsVisibleClose should be true
        public string Status; //passed
    }

    public class NameSp
    {
        public string Name;
        public List<Concern> Concerns = new List<Concern>();
    }

    public class Concern
    {
        public string Name;
        public List<Context> Contexts = new List<Context>();
    }

    public class Context
    {
        public string Name;
        public List<Spec> Specs = new List<Spec>();
    }

    public class Spec
    {
        public string Name;
        public string Status;
    }
}
