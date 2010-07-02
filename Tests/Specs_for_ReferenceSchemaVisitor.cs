// ReSharper disable InconsistentNaming
namespace Specs_for_ReferenceSchemaVisitor
{
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml.Schema;

    using NUnit.Framework;

    using XsdHelper;
    using XsdHelper.Tests.Properties;
    using XsdHelper.Visitors;

    [TestFixture]
    public class When_an_element_references_other_elements
    {
        private XmlSchemaSet _schemaSet = new XmlSchemaSet();
        private XmlSchema _schema;

        [TestFixtureSetUp]
        public void InitFixture()
        {
            using (StringReader schemaReader = new StringReader(Resources.Element_References_Other_Elements))
            {
                _schema = XmlSchema.Read(schemaReader, null);
                _schemaSet.Add(_schema);
                _schemaSet.Compile();
            }
        }

        [Test]
        public void The_node_dependencies_should_contain_all_referenced_elements()
        {
            RelationshipsSchemaVisitor visitor = new RelationshipsSchemaVisitor(_schemaSet);
            visitor.Visit(_schema);

            List<Edge<string>> expectedList = new List<Edge<string>>
                                                  {
                                                      new Edge<string>("Root", "FirstLevelChild"),
                                                      new Edge<string>("FirstLevelChild", "SecondLevelChild"),
                                                      new Edge<string>("SecondLevelChild", "SecondAndThirdLevelChild"),
                                                      new Edge<string>("FirstLevelChild", "SecondAndThirdLevelChild"),
                                                  };

            Assert.That(visitor.NodeEdges, Is.EquivalentTo(expectedList));
        }

        [Test]
        public void The_dependencies_should_be_ordered_from_the_root_to_the_leaf_elements()
        {
            RelationshipsSchemaVisitor visitor = new RelationshipsSchemaVisitor(_schemaSet);
            visitor.Visit(_schema);

            List<string> expectedList = new List<string>() { "Root", "FirstLevelChild", "SecondLevelChild", "SecondAndThirdLevelChild" };

            Assert.That(visitor.SortedDependencies, Is.EqualTo(expectedList));
        }

        [Test]
        public void The_root_node_should_have_no_dependencies_on_it()
        {
            RelationshipsSchemaVisitor visitor = new RelationshipsSchemaVisitor(_schemaSet);
            visitor.Visit(_schema);

            List<string> expectedList = new List<string>() { "Root" };

            Assert.That(visitor.RootNodes, Is.EqualTo(expectedList));
        }
    }


    [TestFixture]
    public class When_an_element_references_other_complex_types
    {
        private readonly XmlSchemaSet _schemaSet = new XmlSchemaSet();
        private XmlSchema _schema;

        [TestFixtureSetUp]
        public void InitFixture()
        {
            using (StringReader schemaReader = new StringReader(Resources.Element_References_Complex_Types_Extensions_And_Groups))
            {
                _schema = XmlSchema.Read(schemaReader, null);
                _schemaSet.Add(_schema);
                _schemaSet.Compile();
            }
        }

        [Test]
        public void The_dependencies_should_include_referenced_complex_types_and_groups()
        {
            RelationshipsSchemaVisitor visitor = new RelationshipsSchemaVisitor(_schemaSet);
            visitor.Visit(_schema);

            List<Edge<string>> expectedList = new List<Edge<string>>
                                                  {
                                                      new Edge<string>("equity", "EquityAsset"),
                                                      new Edge<string>("EquityAsset", "ExchangeTraded"),
                                                      new Edge<string>("ExchangeTraded", "UnderlyingAsset"),
                                                      new Edge<string>("UnderlyingAsset", "IdentifiedAsset"),
                                                      new Edge<string>("IdentifiedAsset", "Asset"),
                                                      new Edge<string>("ExchangeTraded", "ExchangeIdentifier.model"),
                                                      new Edge<string>("equity", "underlyingAsset"),
                                                      new Edge<string>("underlyingAsset", "Asset"),
                                                  };

            var missingEdges = expectedList.Except(visitor.NodeEdges);
            Assert.That(visitor.NodeEdges, Is.EquivalentTo(expectedList));
        }

        [Test]
        public void The_dependencies_should_be_ordered_from_the_root_to_the_leaf_nodes()
        {
            RelationshipsSchemaVisitor visitor = new RelationshipsSchemaVisitor(_schemaSet);
            visitor.Visit(_schema);

            List<string> expectedList = new List<string>()
                                        {
                                                "equity",
                                                "EquityAsset",
                                                "ExchangeTraded",
                                                "UnderlyingAsset",
                                                "IdentifiedAsset",
                                                "Asset",
                                                "ExchangeIdentifier.model",
                                                "underlyingAsset",
                                        };

            Assert.That(visitor.SortedDependencies, Is.EquivalentTo(expectedList));
        }
    }
}
// ReSharper restore InconsistentNaming