#pragma warning disable IDE1006 // 命名样式
using AD.BASE;
using AD.Graph.Exception;
using AD.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace AD.Graph
{
    namespace Exception
    {
        [System.Serializable]
        public class GraphException : ADException
        {
            public GraphException() : this("Unknown") { }
            public GraphException(string message) : this("Graph", message) { }
            public GraphException(string key, string message) : base($"[{key}] : " + message) { }
        }

        [System.Serializable]
        public class OverflowException : GraphException
        {
            public OverflowException(int min,int max,int index) :base("Overflow", $"index should be sandwiched between {min} and {max} , but it is {index}") { }
        }

        [System.Serializable]
        public class ConflictException : GraphException
        {
            public ConflictException() : this("Unknown") { }
            public ConflictException(string message) : base("Conflict", message) { }
        }


        [Serializable]
        public class ExistException : GraphException
        {
            public ExistException(long id) : base("Graph(Ex)", $"Node id={id} is not exist in this graph") { }
            public ExistException(long id0,long id1) : base("Graph(Ex)",$"Node id={id0} or Node id={id1} is not exist in this graph") { }
        }
    }

    namespace Interface
    {
        public interface Iterator<_Node> : IEnumerator<Node>, ILostHandle where _Node : Node
        {

        }

        //public interface Container<_Node, _Iterator>:IEnumerable<_Iterator>, IRebuildHandle where _Node : Node where _Iterator: Iterator<_Node>
        //{
        //    
        //}

        public interface Entry : IEnumerator<float>
        {
            float this[string index] { get;set; }

            int Add(string key, float value);
            int Remove(string key);
        }

        /// <summary>
        /// <list type="table"><see cref="ID"/> is a identifier in graph container</list>
        /// <list type="table"><see cref="Data"/> is satellite data</list>
        /// </summary>
        public interface Node:ILostHandle
        {
            object Data { get; set; }
            long ID { get; }
        }

        public interface NodePath<_Node> :  IEnumerable<_Node>, IEnumerable<Edge<_Node>> where _Node : Node
        {
            _Node[] Nodes { get; }
            Edge<_Node>[] Edges { get; }
        }

        public interface Graph<_Node> : IEnumerable<Iterator<_Node>>, IRebuildHandle where _Node : Node
            //<_Node, _Iterator,_Container> where _Node : Node where _Iterator : Iterator<_Node> where _Container : Container<_Node, _Iterator>
        {
            Node[] Nodes { get; }
            Edge<_Node>[] Edges { get; }

            bool Contains(Node node);
            bool Add(Node node);
            bool Remove(Node node);
            bool Contains(Edge<_Node> edge);
            bool Add(Edge<Node> edge);
            bool Remove(Edge<_Node> edge);

            bool BreakEdge(Edge<_Node> edge);
            bool InsertEdge(Edge<_Node> edge);
            int InsertEdges(params Edge<_Node>[] edges);

            (Node, Edge<_Node>[]) LeadWithNode();
        }

        public interface Edge<_Node>where _Node : Node
        {
            Node From { get; }
            Node To { get; }
            float Weight { get; }
        }
    }

    namespace Entry
    {
        public class KeyValuePair : ILostHandle
        {
            public string Key;
            public float Value;

            public KeyValuePair() : this("null", 0)
            {
            }

            public KeyValuePair(string key, float value)
            {
                Key = key;
                Value = value;
            }

            public bool IsLost { get; private set; }

            public void Dispose()
            {
                IsLost = true;
                Key = null;
            }

            public void Incoming(ILostHandle handle, object sharPtr)
            {
                throw new GraphException("Not Support");
            }

            public void SetLost()
            {
                IsLost = true;
                Value = 0;
            }

            public void ReFocus()
            {
                IsLost = false;
            }

            public void ShareState(ILostHandle handle)
            {
                throw new GraphException("Not Support");
            }
        }

        /// <summary>
        /// Each expansion capacity will only be expanded by a small amount,
        /// please do not <see cref="Add(float)"/> frequently, otherwise pass in a large enough capacity in the constructor
        /// </summary>
        public class BaseEntr : Interface.Entry
        {
            public BaseEntr() { }
            public BaseEntr(params (string, float)[] values)
            {
                if (values == null || values.Length == 0) return;
                source = new KeyValuePair[values.Length];
                head = 0;
                tail = values.Length;
                ptr = 0;
                for (int i = 0, e = values.Length; i < e; i++)
                {
                    source[i] = new(values[i].Item1, values[i].Item2);
                }
            }
            private KeyValuePair[] source;
            private int head = 0, tail = 0;
            private int _ptr = 0;
            private int ptr
            {
                get => _ptr;
                set
                {
                    if (tail <= head) return;
                    _ptr = value % (tail - head) + head;
                }
            }

            public float Current => source[ptr].Value;

            object IEnumerator.Current => this.Current;

            public float this[string index]
            {
                get
                {
                    foreach (var item in source)
                    {
                        if (item.Key == index && !item.IsLost) return item.Value;
                    }
                    throw new GraphException($"The key you expect \"{index}\" but it is not exist or lost");
                }
                set
                {
                    foreach (var item in source)
                    {
                        if (item.Key == index)
                        {
                            item.Value = value;
                            item.ReFocus();
                            return;
                        }
                    }
                    throw new GraphException($"The key you expect \"{index}\" but it is not exist");
                }
            }

            public bool MoveNext()
            {
                do
                {
                    if (_ptr < head) return false;
                    if (ptr + 1 >= tail) return false;
                    ptr++;
                } while (source[ptr].IsLost);
                return true;
            }

            public void Reset()
            {
                throw new System.NotImplementedException();
            }

            public void Dispose()
            {
                source = null;
            }

            public const int EnlargeCount = 5;

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns>
            /// <list type="bullet"><b>-1</b> is reusing empty space where before of head or after of tail </list>
            /// <list type="bullet"><b>-2</b> is enlarge</list>
            /// <list type="bullet"><b>others</b> is reusing empty space where inside</list>
            /// </returns>
            /// <exception cref="GraphException"></exception>
            public int Add(string key, float value)
            {
                for (int i = head; i < tail; i++)
                {
                    var current = source[i];
                    if (current.Key == key)
                    {
                        if (current.IsLost)
                        {
                            current.ReFocus();
                            current.Value = value;
                        }
                        else
                            throw new ConflictException( "Same key is exist");
                        return i;
                    }
                }
                if (head != 0)
                {
                    head--;
                    var current = source[head];
                    current.ReFocus();
                    current.Key = key;
                    current.Value = value;
                    return -1;
                }
                else if (tail != source.Length)
                {
                    tail++;
                    var current = source[tail];
                    current.ReFocus();
                    current.Key = key;
                    current.Value = value;
                    return -1;
                }
                else
                {
                    KeyValuePair[] newPairs = new KeyValuePair[tail + EnlargeCount];
                    for (int i = 0, e = tail - head; i < e; i++)
                    {
                        newPairs[i] = source[i + head];
                    }
                    newPairs[tail] = new KeyValuePair(key, value);
                    head = 0;
                    tail = tail - head + 1;
                    ptr = 0;
                    return -2;
                }
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <returns>
            /// <list type="bullet"><b>-1</b> is not exist</list>
            /// <list type="bullet">exist and return it index</list>
            /// </returns>
            /// <exception cref="GraphException"></exception>
            public int Remove(string key)
            {
                for (int i = head; i < tail; i++)
                {
                    var current = source[i];
                    if (current.Key == key)
                    {
                        current.SetLost();
                        return i;
                    }
                }
                return -1;
            }

            public bool TryGet(string key,out float value)
            {
                foreach (var item in source)
                {
                    if(item.Key == key&&!item.IsLost)
                    {
                        value = item.Value;
                        return true;
                    }
                }
                value = 0;
                return false;
            }
        }

        public class ShareEntr<T>
        {
            public ShareEntr() { }
            public ShareEntr(T t) { value = t; }
            public T value;
            public static implicit operator T(ShareEntr<T> entr) => entr.value;

            public void SetValue(T o) => value = o;
            public T GetValue() => value;
        }
    }

    namespace Helper
    {
        public class LostHandler : ILostHandle
        {
            public bool IsLost { get => isLost; private set => isLost.SetValue(value); }
            private Entry.ShareEntr<bool> isLost = new(false);

            public void Dispose()
            {
                IsLost = true;
            }

            public virtual void Incoming(ILostHandle handle, object sharPtr)
            {
                if (handle.As(out LostHandler handler))
                {
                    this.isLost = handler.isLost;
                }
                else
                {
                    throw new GraphException("Not Support");
                }
            }

            public void SetLost()
            {
                IsLost = true;
            }

            public void ReFocus()
            {
                IsLost = false;
            }

            public virtual void ShareState(ILostHandle handle)
            {
                if (handle.As(out LostHandler handler))
                {
                    handler.isLost = this.isLost;
                }
                else
                {
                    handle.Incoming(this, IsLost);
                }
            }

            public static implicit operator bool(LostHandler handler) => !handler.IsLost;
        }

        public class BaseIter<_Node> : LostHandler,Interface.Iterator<_Node> where _Node : Interface.Node
        {
            public BaseIter(_Node[] nodes)
            {
                container = nodes;
            }

            private readonly _Node[] container;
            private int index = -1;

            public Interface.Node Current => container[index];

            object IEnumerator.Current => this.Current;

            public bool MoveNext()
            {
                if(this)
                {
                    index++;
                    return index < container.Length;
                }
                throw new LostException("The original container has changed and you try to use this iterator");
            }

            public void Reset()
            {
                index = -1;
            }
        }
    }

    public class Node : Helper.LostHandler, Interface.Node
    {
        public object Data { get; set; }
        public long ID { get; private set; }
    }

    /// <summary>
    /// When container each change , all instance it release will be <see cref="ILostHandle.SetLost"/>
    /// </summary>
    public class NodeIter : Helper.BaseIter<Node>
    {
        public NodeIter(Node[] nodes,Interface.Graph<Node> nodeContainer) : base(nodes)
        {
            nodeContainer.AddListener(this.SetLost);
        }
    }

    public class Edge : Interface.Edge<Node>
    {
        public readonly Node From;
        public readonly Node To;

        public Edge(Node from, Node to, float weight)
        {
            this.From = from;
            this.To = to;
            this.Weight = weight;
        }

        public float Weight { get; set; }

        Interface.Node Interface.Edge<Node>.From => this.From;

        Interface.Node Interface.Edge<Node>.To => this.To;

        public void Insert(Node node,float previousWeight, float latterWeight, Interface.Graph<Node> graph)
        {
            graph.BreakEdge(this);
            graph.InsertEdges(new Edge(From, node, previousWeight),new Edge(node,To,latterWeight));
        }

        public void Link(Edge target,float newWeight, Interface.Graph<Node> graph)
        {
            if (target.From != this.To) throw new ConflictException("The intermediate nodes are not the same");
            graph.BreakEdge(this);
            graph.BreakEdge(target);
            graph.InsertEdge(new Edge(From, target.To, newWeight));
        }
    }

    public class NodePath : Interface.NodePath<Node>
    {
        public NodePath(Node[] nodes, Interface.Edge<Node>[] edges)
        {
            Nodes = nodes;
            Edges = edges;
        }

        public Node[] Nodes { get; private set; }

        public Interface.Edge<Node>[] Edges { get; private set; }

        public static NodePath[] GetNodePaths(Node start,Node end,Interface.Graph<Node> graph)
        {
            if (!graph.Contains(start))
                throw new ExistException(start.ID);
            if (!graph.Contains(end))
                throw new ExistException(end.ID);
            NodePath[] result = null;
            //TODO
            return result;
        }

        public IEnumerator<Node> GetNodeEnumerator()
        {
            if(Nodes==null|| Nodes.Length==0)
            {
                yield return null;
            }
            else
            {
                foreach (var node in Nodes)
                {
                    yield return node;
                }
            }
        }

        public IEnumerator<Interface.Edge<Node>> GetEdgeEnumerator()
        {
            if (Edges == null || Edges.Length == 0)
            {
                yield return null;
            }
            else
            {
                foreach (var edge in Edges)
                {
                    yield return edge;
                }
            }
        }

        IEnumerator<Node> IEnumerable<Node>.GetEnumerator()
        {
            return GetNodeEnumerator();
        }

        IEnumerator<Interface.Edge<Node>> IEnumerable<Interface.Edge<Node>>.GetEnumerator()
        {
            return GetEdgeEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetNodeEnumerator();
        }
    }
}
#pragma warning restore IDE1006 // 命名样式
