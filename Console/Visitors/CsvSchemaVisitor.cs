namespace XsdHelper.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;

    internal class CsvSchemaVisitor : SchemaVisitorBase<Stack<string>>
    {
        private readonly TextWriter _writer;
        private readonly XmlSchemaSet _schemaSet;
        private readonly List<string> _nodesToSkip;
        private readonly List<string> _additionalNodesToExtract;

        public CsvSchemaVisitor(XmlSchemaSet schemaSet, TextWriter writer, IEnumerable<string> nodesToSkip, IEnumerable<string> additionalNodesToExtract)
        {
            _writer = writer;
            _schemaSet = schemaSet;
            _nodesToSkip = new List<string>(nodesToSkip);
            _additionalNodesToExtract = new List<string>(additionalNodesToExtract);
        }

        public override void Visit(XmlSchema schema)
        {
            foreach (XmlSchemaObject schemaObject in schema.Items)
            {
                Stack<string> path = new Stack<string>();
                this.Visit(schemaObject, path);
            }
        }

        public override void Visit(XmlSchemaElement schemaObject, Stack<string> collector)
        {
            string elementName = schemaObject.RefName.Name;

            if (!String.IsNullOrEmpty(elementName))
            {
                schemaObject = (XmlSchemaElement)_schemaSet.GlobalElements[schemaObject.RefName];
            }
            else
            {
                elementName = schemaObject.Name;
            }

            if (_nodesToSkip.Contains(elementName))
            {
                return;
            }

            // Avoid self referencing relationships that will cause an infinite loop
            if (collector.Contains(elementName))
            {
                return;
            }

            collector.Push(elementName);

            if (schemaObject.SchemaType != null)
            {
                this.Visit(schemaObject.SchemaType, collector);
            }
            else if (schemaObject.ElementSchemaType != null)
            {
                string[] currentPath = collector.ToArray();
                string parentElementName = String.Empty;
                string annotation = GetAnnotation(schemaObject);

                if (currentPath.Length > 1)
                {
                    parentElementName = currentPath[1];
                }

                string escapedAnnotation = annotation.Replace("\"", "\"\"");
                string[] nodePath = currentPath.Reverse().ToArray();
                bool isOptional = schemaObject.MinOccurs == 0;

                string valueType = schemaObject.ElementSchemaType.TypeCode.ToString();

                if (valueType == "None")
                {
                    valueType = schemaObject.ElementSchemaType.Name;
                }

                List<string> csvItems = new List<string>
                                            {
                                                    parentElementName + "/" + schemaObject.Name, 
                                                    "/" + String.Join(@"/", nodePath), 
                                                    "\"" + escapedAnnotation + "\"", 
                                                    valueType,
                                                    isOptional.ToString()
                                            };

                foreach (string nodeToExtract in _additionalNodesToExtract)
                {
                    if (schemaObject.UnhandledAttributes == null)
                    {
                        csvItems.Add(String.Empty);
                    }
                    else
                    {
                        XmlNode node = schemaObject.UnhandledAttributes.SingleOrDefault(a => a.Name.Equals(nodeToExtract));
                        string nodeValue = node == null ? String.Empty : node.Value;

                        csvItems.Add(nodeValue);
                    }
                }

                _writer.WriteLine(String.Join(",", csvItems.ToArray()));

                // Drill into the child nodes of the type
                Visit(schemaObject.ElementSchemaType, collector);
            }

            collector.Pop();
        }

        public override void Visit(XmlSchemaAttribute schemaObject, Stack<string> collector)
        {
            string atributeName = schemaObject.Name;

            if (_nodesToSkip.Contains(atributeName))
            {
                return;
            }

            string[] currentPath = collector.ToArray();
            string parentElementName = currentPath[0];
            string annotation = GetAnnotation(schemaObject);

            string escapedAnnotation = annotation.Replace("\"", "\"\"");
            string[] nodePath = currentPath.Reverse().ToArray();
            string valueType = schemaObject.SchemaTypeName.ToString();
            _writer.WriteLine(String.Format(@"{0}@{1},/{2}@{1},""{3}"",{4}", parentElementName, schemaObject.Name, String.Join(@"/", nodePath), escapedAnnotation, valueType));
        }

        /// <summary>
        /// Gets the documentation for the node
        /// </summary>
        /// <param name="annotatedNode">The annotated node</param>
        /// <returns>The documentation text</returns>
        private static string GetAnnotation(XmlSchemaAnnotated annotatedNode)
        {
            if (annotatedNode.Annotation == null)
            {
                return String.Empty;
            }

            List<string> annotations = new List<string>();

            foreach (XmlSchemaObject item in annotatedNode.Annotation.Items)
            {
                XmlSchemaAppInfo appInfo = item as XmlSchemaAppInfo;
                XmlSchemaDocumentation documentation = item as XmlSchemaDocumentation;
                XmlNode[] markup = null;

                if (appInfo != null)
                {
                    markup = appInfo.Markup;
                }

                if (documentation != null)
                {
                    markup = documentation.Markup;
                }

                if (markup != null)
                {
                    annotations.AddRange(markup.Select(node => node.InnerText));
                }
            }

            return String.Join(" ", annotations.ToArray());
        }
    }
}
