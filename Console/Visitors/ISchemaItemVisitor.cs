namespace XsdHelper.Visitors
{
    using System.Xml.Schema;

    public delegate void VisitDelegate<T>(XmlSchemaObject schemaObject, T collector);

    internal interface ISchemaItemVisitor<T>
    {
        void Visit(XmlSchema schema);

        void Visit(XmlSchemaObject schemaObject, T collector);

        void Visit(XmlSchemaElement schemaObject, T collector);

        void Visit(XmlSchemaAttribute schemaObject, T collector);

        void Visit(XmlSchemaComplexType schemaObject, T collector);

        void Visit(XmlSchemaSequence schemaObject, T collector);

        void Visit(XmlSchemaChoice schemaObject, T collector);
    }
}