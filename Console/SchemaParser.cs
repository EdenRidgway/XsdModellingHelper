namespace XsdHelper
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Diagnostics;
    using System.IO;
    using System.Xml;
    using System.Xml.Schema;

    using Visitors;

    internal class SchemaParser
    {
        private readonly XmlSchemaSet _schemaSet;
        private readonly XmlSchema _schema;
        private readonly TextWriter _writer;
        private readonly Dictionary<string, XmlSchema> _loadedSchemas = new Dictionary<string, XmlSchema>(StringComparer.InvariantCultureIgnoreCase);

        public SchemaParser(string sourceFileName, TextWriter writer)
        {
            _schemaSet = new XmlSchemaSet();
            _schemaSet.XmlResolver = new XmlUrlResolver();

            using (StreamReader reader = new StreamReader(sourceFileName))
            {
                _schema = XmlSchema.Read(reader, SchemaValidationHandler);
                FileInfo fileInfo = new FileInfo(sourceFileName);
                this.LoadedSchemas.Add(fileInfo.Name, _schema);
                _schema.SourceUri = fileInfo.FullName;
            }

            _schemaSet.Add(_schema);
            LoadReferencedSchemas(_schema);

            _writer = writer;
        }

        public Dictionary<string, XmlSchema> LoadedSchemas
        {
            get { return this._loadedSchemas; }
        }

        /// <summary>
        /// Extracts the XPaths from the schema into a csv file.
        /// </summary>
        /// <param name="nodesToSkip">The nodes to skip when compiling the list.</param>
        /// <param name="additionalNodesToExtract">Additional nodes to add to the csv output.</param>
        /// <param name="rootNodeName">The name of the root node from which to start generating the XPaths.</param>
        public void ExtractXPaths(IEnumerable<string> nodesToSkip, IEnumerable<string> additionalNodesToExtract, string rootNodeName)
        {
            List<string> header = new List<string>();
            header.AddRange(new[] { "Node", "XPath", "Annotation", "Data Type", "Optional" } .Union(additionalNodesToExtract));
            _writer.WriteLine(String.Join(",", header.ToArray()));

            try
            {
                _schemaSet.Compile();
            }
            catch (XmlSchemaException ex)
            {
                XmlSchemaObject ultimateParent = ex.SourceSchemaObject;
                while (ultimateParent.Parent != null)
                {
                    ultimateParent = ultimateParent.Parent;
                }

                Console.WriteLine(String.Format(
                                  "Error when compiling schemas: {0}\nProblem with schema {1} on line {2}, position {3}",
                                  ex.Message,
                                  ultimateParent.SourceUri,
                                  ex.LineNumber,
                                  ex.LinePosition));

                Environment.Exit(1);
            }

            if (String.IsNullOrEmpty(rootNodeName))
            {
                RelationshipsSchemaVisitor referenceSchemaVisitor = new RelationshipsSchemaVisitor(_schemaSet);

                foreach (XmlSchema schema in _schemaSet.Schemas())
                {
                    referenceSchemaVisitor.Visit(schema);
                }

                if (referenceSchemaVisitor.RootNodes.Count() > 0)
                {
                    string[] rootNodeNames = referenceSchemaVisitor.RootNodes.ToArray();
                    Console.WriteLine("Found the following root nodes: {0}", String.Join(",", rootNodeNames));
                    rootNodeName = rootNodeNames[0];
                    Console.WriteLine("Automatically selecting: {0} as the root node", rootNodeName);
                }
                else
                {
                    // TODO: Possibly move to a model where these are application exceptions that are then handled elsewhere
                    Console.WriteLine("Unable to find a root node. Please supply one.");
                    Environment.Exit(1);
                }
            }

            XmlSchemaObject rootItem = GetRootItemNode(rootNodeName);

            if (rootItem == null)
            {
                Console.WriteLine("Unable to find a root node {0}. Please supply a properly qualified node name.", rootNodeName);
                Environment.Exit(1);
            }

            CsvSchemaVisitor csvSchemaVisitor = new CsvSchemaVisitor(_schemaSet, _writer, nodesToSkip, additionalNodesToExtract);
            csvSchemaVisitor.Visit(rootItem, new Stack<string>());
        }

        private void LoadReferencedSchemas(XmlSchema schema)
        {
            FileInfo parentFileInfo = new FileInfo(schema.SourceUri);

            foreach (XmlSchemaExternal schemaRef in schema.Includes)
            {
                if (schemaRef.SchemaLocation == null || schemaRef.SchemaLocation.Trim().Length == 0)
                {
                    schemaRef.SchemaLocation = null;
                }

                if (schemaRef.SchemaLocation == null)
                {
                    if (schemaRef is XmlSchemaImport)
                    {
                        continue;
                    }

                    if (schemaRef is XmlSchemaInclude)
                    {
                        throw new ApplicationException("Schema locaton not specified for included schema ");
                    }
                }

                string schemaLocation = schemaRef.SchemaLocation;

                if (schemaLocation == null)
                {
                    throw new ApplicationException("Schema locaton is null or empty.");
                }

                if (this.LoadedSchemas.ContainsKey(schemaLocation))
                {
                    Debug.WriteLine(String.Format("{0}: Referenced schema {1} already loaded.", parentFileInfo.Name, schemaLocation));

                    schemaRef.Schema = this.LoadedSchemas[schemaLocation];
                    schemaRef.SchemaLocation = null;
                    continue;
                }

                Debug.WriteLine(String.Format("{0}: Loading referenced schema {1}.", parentFileInfo.Name, schemaLocation));

                string schemaFileName = Path.Combine(parentFileInfo.Directory.FullName, schemaLocation);

                using (StreamReader reader = new StreamReader(schemaFileName))
                {
                    XmlSchema childSchema = XmlSchema.Read(reader, SchemaValidationHandler);
                    childSchema.SourceUri = schemaFileName;
                    this.LoadedSchemas.Add(schemaLocation, childSchema);

                    schemaRef.Schema = childSchema;
                    schemaRef.SchemaLocation = null;

                    _schemaSet.Add(childSchema);

                    LoadReferencedSchemas(childSchema);
                }
            }
        }

        /// <summary>
        /// Gets the root node in the schema by searching for an element with the rootNodeName in all loaded namespaces.
        /// </summary>
        /// <param name="rootNodeName">The name of the root node to be found</param>
        /// <returns>Null if the node is not found or the node if it is.</returns>
        private XmlSchemaObject GetRootItemNode(string rootNodeName)
        {
            if (rootNodeName.Contains(":"))
            {
                return GetFullyQualifiedNode(rootNodeName);
            }

            // Search all the namespaces for the node
            IEnumerable<XmlSchema> schemas = this._schemaSet.Schemas().OfType<XmlSchema>();

            var namespaces = from schema in schemas
                             from namespsace in schema.Namespaces.ToArray()
                             select namespsace.Namespace;

            foreach (string schemaNameSpace in namespaces)
            {
                XmlSchemaObject rootItem = this._schemaSet.GlobalElements[new XmlQualifiedName(rootNodeName, schemaNameSpace)];

                if (rootItem == null)
                {
                    continue;
                }

                Console.WriteLine("Found root node {0} in namespace {1}.", rootNodeName, schemaNameSpace);
                return rootItem;
            }

            return null;
        }

        private XmlSchemaObject GetFullyQualifiedNode(string rootNodeName)
        {
            int lastColonPosition = rootNodeName.LastIndexOf(":");
            string nodeNameSpace = rootNodeName.Substring(0, lastColonPosition);
            string nodeName = rootNodeName.Substring(lastColonPosition + 1);

            return this._schemaSet.GlobalTypes[new XmlQualifiedName(nodeName, nodeNameSpace)] ??
                   this._schemaSet.GlobalElements[new XmlQualifiedName(nodeName, nodeNameSpace)];
        }

        /// <summary>
        /// Report on any schema errors
        /// </summary>
        private void SchemaValidationHandler(object sender, ValidationEventArgs e)
        {
            Console.WriteLine(e.Exception);
        }

    }
}
