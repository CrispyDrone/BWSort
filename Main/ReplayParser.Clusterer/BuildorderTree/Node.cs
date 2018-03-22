using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReplayParser.Clusterer.BuildorderTree
{
    public class Node<T>
    {
        private T data;
        private long occurances = 0;
        private NodeList<T> neighbors = null;

        public Node() { }
        public Node(T data) : this(0, data, null) { }
        public Node(long occurances, T data, NodeList<T> neighbors)
        {
            this.data = data;
            this.neighbors = neighbors;
            this.occurances = occurances;
        }

        public T Value
        {
            get
            {
                return data;
            }
            set
            {
                data = value;
            }
        }

        public long Occurances
        {
            get
            {
                return occurances;
            }
            set
            {
                occurances = value;
            }
        }

        public NodeList<T> Neighbors
        {
            get
            {
                return neighbors;
            }
            set
            {
                neighbors = value;
            }
        }

        public void Visit(Action<Node<T>> v)
        {
            v(this);
            foreach (Node<T> n in Neighbors)
            {
                n.Visit(v);
            }
        }
    }
}
