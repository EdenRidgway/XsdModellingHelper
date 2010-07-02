namespace XsdHelper.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Schema;

    /// <summary>
    /// Determines all the edges (relationships) between the nodes
    /// </summary>
    public class RelationshipsSchemaVisitor : SchemaVisitorBase<Edge<string>>
    {
        private List<Edge<string>> _nodeEdges = new List<Edge<string>>();
        private List<XmlSchemaElement> _elementsProcessed = new List<XmlSchemaElement>();
        private XmlSchemaSet _schemaSet;
        private TopologicalEdgeSorter<string> _dependencySorter = new TopologicalEdgeSorter<string>();

        public RelationshipsSchemaVisitor(XmlSchemaSet schemaSet) : base()
        {
            _schemaSet = schemaSet;
            _visitors.Add(typeof(XmlSchemaComplexContent), (schemaObject, collector) => Visit((XmlSchemaComplexContent)schemaObject, collector));
            _visitors.Add(typeof(XmlSchemaSimpleContent), (schemaObject, collector) => Visit((XmlSchemaSimpleContent)schemaObject, collector));
            _visitors.Add(typeof(XmlSchemaComplexContentExtension), (schemaObject, collector) => Visit((XmlSchemaComplexContentExtension)schemaObject, collector));
            _visitors.Add(typeof(XmlSchemaSimpleContentExtension), (schemaObject, collector) => Visit((XmlSchemaSimpleContentExtension)schemaObject, collector));
            _visitors.Add(typeof(XmlSchemaGroupRef), (schemaObject, collector) => this.Visit((XmlSchemaGroupRef)schemaObject, collector));
            _visitors.Add(typeof(XmlSchemaGroup), (schemaObject, collector) => this.Visit((XmlSchemaGroup)schemaObject, collector));
        }

        /// <summary>
        /// The node edge relationships (i.e. the relationships between the elements)
        /// </summary>
        public IList<Edge<string>> NodeEdges
        {
            get
            {
                return _nodeEdges;
            }
        }

        /// <summary>
        /// All the nodes (elements) in the schema set
        /// </summary>
        public IEnumerable<string> Nodes
        {
            get
            {
                return (from edge in NodeEdges select edge.Source)
                       .Union
                       (from edge in NodeEdges select edge.Target)
                       .Distinct();
            }
        }

        /// <summary>
        /// Gets a dictionary mapping the depedencies that each node has by element name
        /// </summary>
        public Dictionary<string, IEnumerable<string>> Dependencies
        {
            get
            {
                return Nodes.ToDictionary(i => i, i => NodeEdges.Where(e => e.Source.Equals(i)).Select(e => e.Target));
            }
        }

        /// <summary>
        /// Gets the dependencies sorted from the root nodes out
        /// </summary>
        public IEnumerable<string> SortedDependencies
        {
            get
            {
                return _dependencySorter.Sort(_nodeEdges).Reverse();
            }
        }

        /// <summary>
        /// Gets the dependencies sorted from the root nodes out
        /// </summary>
        public IEnumerable<string> RootNodes
        {
            get
            {
                return from node in Nodes 
                       where !NodeEdges.Any(ne => ne.Target == node) 
                       select node;
            }
        }

        /// <summary>
        /// Visits all the elements in the schema to establish the depedencies between the nodes
        /// </summary>
        /// <param name="schema">
        /// The schema.
        /// </param>
        public override void Visit(XmlSchema schema)
        {
            foreach (XmlSchemaObject schemaObject in schema.Elements.Values)
            {
                Edge<string> edge = new Edge<string>();
                this.Visit(schemaObject, edge);
            }
        }

        public override void Visit(XmlSchemaElement schemaObject, Edge<string> collector)
        {
            if (_elementsProcessed.Contains(schemaObject)) return;
            _elementsProcessed.Add(schemaObject);

            //If this element can be substituted for another then it is dependent on that structure so record this relationship
            if (!String.IsNullOrEmpty(schemaObject.SubstitutionGroup.Name))
            {
                //The groups must be elements
                XmlSchemaElement substitutionGroup = (XmlSchemaElement)_schemaSet.GlobalElements[schemaObject.SubstitutionGroup];
                _nodeEdges.Add(new Edge<string>(schemaObject.Name, substitutionGroup.Name));

                Visit(substitutionGroup, new Edge<string>(substitutionGroup.Name, null));
            }

            string referencedElementName = schemaObject.RefName.Name;

            if (!String.IsNullOrEmpty(referencedElementName))
            {
                schemaObject = (XmlSchemaElement)_schemaSet.GlobalElements[schemaObject.RefName];
                Edge<string> matchedEdge = collector.Clone();
                matchedEdge.Target = referencedElementName;
                _nodeEdges.Add(matchedEdge);
            }
            else
            {
                collector.Source = schemaObject.Name;
            }

            if (schemaObject.SchemaType != null)
            {
                Visit(schemaObject.SchemaType, new Edge<string>(schemaObject.Name, null));
            }
            // type="ComplexType" references are exposed as ElementSchemaType
            else if (schemaObject.ElementSchemaType != null)
            {
                Edge<string> childCollector = null;

                if (!String.IsNullOrEmpty(schemaObject.ElementSchemaType.Name))
                {
                    Edge<string> matchedEdge = collector.Clone();
                    matchedEdge.Target = schemaObject.ElementSchemaType.Name;
                    _nodeEdges.Add(matchedEdge);

                    childCollector = new Edge<string>(schemaObject.ElementSchemaType.Name, null);
                }

                if (schemaObject.ElementSchemaType.BaseXmlSchemaType != null)
                {
                    Visit(schemaObject.ElementSchemaType.BaseXmlSchemaType, childCollector ?? collector);
                }
                else
                {
                    Visit(schemaObject.ElementSchemaType, childCollector ?? collector);
                }
            }
        }

        public override void Visit(XmlSchemaComplexType schemaObject, Edge<string> collector)
        {
            if (schemaObject.BaseXmlSchemaType != null && !String.IsNullOrEmpty(schemaObject.Name))
            {
                if (!String.IsNullOrEmpty(collector.Source))
                {
                    _nodeEdges.Add(new Edge<string>(collector.Source, schemaObject.Name));
                }

                collector = new Edge<string>(schemaObject.Name, null);
                Visit(schemaObject.BaseXmlSchemaType, collector);
            }

            // Get the underlying reference [that the content model nodes provide] and then below process the inherited/referenced elements
            if (schemaObject.ContentModel != null)
            {
                Visit(schemaObject.ContentModel, collector);
            }
            
            if (schemaObject.ContentType == XmlSchemaContentType.ElementOnly || schemaObject.ContentType == XmlSchemaContentType.Mixed)
            {
                Visit(schemaObject.ContentTypeParticle, collector);
            }
            else if (schemaObject.Particle != null)
            {
                Visit(schemaObject.Particle, collector);
            }
            // Not sure about other cases yet
        }

        public void Visit(XmlSchemaSimpleContentExtension schemaObject, Edge<string> collector)
        {
            
        }

        public void Visit(XmlSchemaComplexContent schemaObject, Edge<string> collector)
        {
            Visit(schemaObject.Content, collector);
        }

        public void Visit(XmlSchemaSimpleContent schemaObject, Edge<string> collector)
        {
            Visit(schemaObject.Content, collector);
        }

        public void Visit(XmlSchemaComplexContentExtension schemaObject, Edge<string> collector)
        {
            Visit(schemaObject.Particle, collector);
        }

        public void Visit(XmlSchemaGroup schemaObject, Edge<string> collector)
        {

            collector = new Edge<string>(schemaObject.Name, null);

            this.Visit(schemaObject.Particle, collector);
        }

        public override void Visit(XmlSchemaSimpleType schemaObject, Edge<string> collector)
        {
        }

        public void Visit(XmlSchemaGroupRef schemaObject, Edge<string> collector)
        {
            _nodeEdges.Add(new Edge<string>(collector.Source, schemaObject.RefName.Name));
        }

        public override void Visit(XmlSchemaAttribute schemaObject, Edge<string> collector)
        {
            ///TODO: Handle attributes
        }
    }
}
