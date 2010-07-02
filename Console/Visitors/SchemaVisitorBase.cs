namespace XsdHelper.Visitors
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Xml;
    using System.Xml.Schema;

    public abstract class SchemaVisitorBase<T> : ISchemaItemVisitor<T>
    {
        protected Dictionary<Type, VisitDelegate<T>> _visitors;

        protected SchemaVisitorBase()
        {
            SetupVisitors();
        }

        protected void SetupVisitors()
        {
            _visitors = new Dictionary<Type, VisitDelegate<T>>()
                        {
                                { typeof(XmlSchemaElement), (schemaObject, collector) => this.Visit((XmlSchemaElement)schemaObject, collector) },
                                { typeof(XmlSchemaAttribute), (schemaObject, collector) => this.Visit((XmlSchemaAttribute)schemaObject, collector) },
                                { typeof(XmlSchemaSimpleType), (schemaObject, collector) => this.Visit((XmlSchemaSimpleType)schemaObject, collector) },
                                { typeof(XmlSchemaComplexType), (schemaObject, collector) => this.Visit((XmlSchemaComplexType)schemaObject, collector) },
                                { typeof(XmlSchemaSequence), (schemaObject, collector) => this.Visit((XmlSchemaSequence)schemaObject, collector) },
                                { typeof(XmlSchemaChoice), (schemaObject, collector) => this.Visit((XmlSchemaChoice)schemaObject, collector) },
                                { typeof(XmlSchemaAny), (schemaObject, collector) => this.Visit((XmlSchemaAny)schemaObject, collector) },
                        };
        }

        public abstract void Visit(XmlSchema schema);

        public virtual void Visit(XmlSchemaObject schemaObject, T collector)
        {
            _visitors[schemaObject.GetType()](schemaObject, collector);
        }

        public abstract void Visit(XmlSchemaElement schemaObject, T collector);

        public abstract void Visit(XmlSchemaAttribute schemaObject, T collector);

        public virtual void Visit(XmlSchemaComplexType schemaObject, T collector)
        {
            if (schemaObject.ContentType == XmlSchemaContentType.ElementOnly)
            {
                this.Visit(schemaObject.ContentTypeParticle, collector);
            }
            else if (schemaObject.Particle != null)
            {
                this.Visit(schemaObject.Particle, collector);
            }
        }

        public virtual void Visit(XmlSchemaSimpleType schemaObject, T collector)
        {
            // By default do nothing
        }

        public virtual void Visit(XmlSchemaAny schemaObject, T collector)
        {
            // By default do nothing
        }

        public virtual void Visit(XmlSchemaSequence schemaObject, T collector)
        {
            foreach (XmlSchemaObject item in schemaObject.Items)
            {
                this.Visit(item, collector);
            }
        }

        public virtual void Visit(XmlSchemaChoice schemaObject, T collector)
        {
            foreach (XmlSchemaObject item in schemaObject.Items)
            {
                this.Visit(item, collector);
            }
        }
    }
}