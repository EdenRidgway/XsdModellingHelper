
namespace XsdHelper
{
    using System;
    using System.Diagnostics;

    [DebuggerDisplay("Source={Source}, Target={Target}")]
    public class Edge<T> where T : IComparable 
    {
        public Edge()
        {
        }

        public Edge(T source, T target)
        {
            this.Source = source;
            this.Target = target;
        }

        public T Source { get; set; }
        public T Target { get; set; }

        public override bool Equals(object obj)
        {
            Edge<T> otherEdge = obj as Edge<T>;

            if (obj == null || otherEdge == null)
            {
                return false;
            }

            if ((this.Source == null && otherEdge.Source == null) &&
               (this.Target == null && otherEdge.Target == null))
            {
                return true;
            }

            if (this.Source == null || this.Target == null) return false;

            return (this.Source.CompareTo(otherEdge.Source) == 0) && (this.Target.CompareTo(otherEdge.Target) == 0);
        }

        public override int GetHashCode()
        {
            return this.Source.GetHashCode() + this.Target.GetHashCode();
        }

        public Edge<T> Clone()
        {
            return new Edge<T>(this.Source, this.Target);
        }

        public override string ToString()
        {
            return String.Format("Source={0}, Target={1}", Source, Target);
        }
    };

}
