#pragma warning disable IDE1006 // 命名样式
using AD.BASE;
using AD.Graph.Exception;
using AD.SAL;
using AD.Utility;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

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
            public OverflowException(int min, int max, int index) : base("Overflow", $"index should be sandwiched between {min} and {max} , but it is {index}") { }
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
            public ExistException(long id0, long id1) : base("Graph(Ex)", $"Node id={id0} or Node id={id1} is not exist in this graph") { }
        }
    }

    public class NodeEdgeLink<_Node> : IEnumerable<Interface.Edge<_Node>> where _Node : Interface.Node
    {
        public _Node Start;
        public Dictionary<_Node, Interface.Edge<_Node>> Links;

        public NodeEdgeLink(_Node start)
        {
            Start = start;
        }

        public Interface.Edge<_Node> GetEdge(_Node end)
        {
            if (Links.TryGetValue(end, out var edge))
                return edge;
            else
                return null;
        }

        public IEnumerator<Interface.Edge<_Node>> GetEnumerator()
        {
            foreach (var edge in Links)
            {
                yield return edge.Value;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
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
            float this[string index] { get; set; }

            int Add(string key, float value);
            int Remove(string key);
        }

        /// <summary>
        /// <list type="table"><see cref="ID"/> is a identifier in graph container</list>
        /// <list type="table"><see cref="Data"/> is satellite data</list>
        /// </summary>
        public interface Node : ILostHandle
        {
            object Data { get; set; }
            long ID { get; }
        }

        public interface NodePath<_Node> : IEnumerable<_Node>, IEnumerable<Edge<_Node>> where _Node : Node
        {
            _Node[] Nodes { get; }
            Edge<_Node>[] Edges { get; }
        }

        public interface Graph<_Node> : IEnumerable<Iterator<_Node>>, IRebuildHandle where _Node : Node
            //<_Node, _Iterator,_Container> where _Node : Node where _Iterator : Iterator<_Node> where _Container : Container<_Node, _Iterator>
        {
            _Node[] Nodes { get; }
            Edge<_Node>[] Edges { get; }

            bool Contains(_Node node);
            bool Add(_Node node);
            bool Remove(_Node node);
            bool Contains(Edge<_Node> edge);
            bool Add(Edge<_Node> edge);
            bool Remove(Edge<_Node> edge);

            bool BreakEdge(Edge<_Node> edge);
            bool InsertEdge(Edge<_Node> edge);
            int InsertEdges(params Edge<_Node>[] edges);

            Dictionary<_Node, NodeEdgeLink<_Node>> LeadWithNode();
        }

        public interface Edge<_Node> where _Node : Node
        {
            _Node From { get; }
            _Node To { get; }
            float Weight { get; }
        }
    }

    namespace Entry
    {
        [Serializable]
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

        [Serializable]
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
                            throw new ConflictException("Same key is exist");
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

            public bool TryGet(string key, out float value)
            {
                foreach (var item in source)
                {
                    if (item.Key == key && !item.IsLost)
                    {
                        value = item.Value;
                        return true;
                    }
                }
                value = 0;
                return false;
            }
        }

        [Serializable]
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
        [Serializable]
        public class LostHandler : ILostHandle
        {
            public bool IsLost { get => isLost; private set => isLost.SetValue(value); }
            [SerializeField] private Entry.ShareEntr<bool> isLost = new(false);

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

        public class BaseIter<_Node> : LostHandler, Interface.Iterator<_Node> where _Node : Interface.Node
        {
            public BaseIter(_Node[] nodes)
            {
                container = nodes;
            }

            public readonly _Node[] container;
            private int index = -1;

            public Interface.Node Current => container[index];

            object IEnumerator.Current => this.Current;

            public bool MoveNext()
            {
                if (this)
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

    [Serializable]
    public class Node : Helper.LostHandler, Interface.Node
    {
        [SerializeField] private object data;
        [SerializeField] private long iD;

        public virtual object Data { get => data; set => data = value; }
        public long ID { get => iD; private set => iD = value; }

        public override string ToString()
        {
            return ID.ToString();
        }

        private static long totallyIndex = 0;
        private static Queue<long> releaseIndexs = new();

        public Node()
        {
            if (releaseIndexs.Count == 0)
                ID = totallyIndex++;
            else ID = releaseIndexs.Dequeue();
        }
        ~Node()
        {
            releaseIndexs.Enqueue(ID);
        }
    }

    [Serializable]
    /// <summary>
    /// When container each change , all instance it release will be <see cref="ILostHandle.SetLost"/>
    /// </summary>
    public class NodeIter : Helper.BaseIter<Node>
    {
        public NodeIter(Node[] nodes, Interface.Graph<Node> nodeContainer) : base(nodes)
        {
            nodeContainer.AddListener(this.SetLost);
        }
    }

    [Serializable]
    public class Edge : Interface.Edge<Node>
    {
        [SerializeField] private Node from;
        [SerializeField] private Node to;
        [SerializeField] private float weight;

        public Node From { get => from; private set => from = value; }
        public Node To { get => to; private set => to = value; }

        public Edge(Node from, Node to, float weight)
        {
            this.From = from;
            this.To = to;
            this.Weight = weight;
        }
        public float Weight { get => weight; set => weight = value; }

        public void Insert(Node node, float previousWeight, float latterWeight, Interface.Graph<Node> graph)
        {
            graph.BreakEdge(this);
            graph.InsertEdges(new Edge(From, node, previousWeight), new Edge(node, To, latterWeight));
        }

        public void Link(Edge target, float newWeight, Interface.Graph<Node> graph)
        {
            if (target.From != this.To) throw new ConflictException("The intermediate nodes are not the same");
            graph.BreakEdge(this);
            graph.BreakEdge(target);
            graph.InsertEdge(new Edge(From, target.To, newWeight));
        }
    }

    [Serializable]
    public class NodePath : Interface.NodePath<Node>
    {
        private Node[] nodes;
        private Interface.Edge<Node>[] edges;

        protected NodePath(Node[] nodes, Interface.Edge<Node>[] edges)
        {
            this.nodes = nodes;
            this.edges = edges;
        }

        public Node[] Nodes => nodes;
        public Interface.Edge<Node>[] Edges => edges;

        public static NodePath[] GetNodePaths(Node start, Node end, Interface.Graph<Node> graph)
        {
            if (!graph.Contains(start))
                throw new ExistException(start.ID);
            if (!graph.Contains(end))
                throw new ExistException(end.ID);
            var board = graph.LeadWithNode();
            List<NodePath> result = new();
            Stack<Node> mainStack = new();
            mainStack.Push(start);
            Stack<Stack<Node>> secStack = new();
            secStack.Push(new());
            foreach (var edge in board[start])
            {
                secStack.Peek().Push(edge.To);
            }
            while (mainStack.Count > 0)
            {
                var curSec = secStack.Peek();
                if (curSec.Count > 0)
                {
                    mainStack.Push(curSec.Pop().Share(out var nowPop));
                    secStack.Push(new());
                    foreach (var edge in board[nowPop])
                    {
                        if (!mainStack.Contains(edge.To))
                            secStack.Peek().Push(edge.To);
                    }
                }
                else
                {
                    mainStack.Pop();
                    secStack.Pop();
                }

                if (mainStack.Peek() == end)
                {
                    List<Node> curNodes = mainStack.Contravariance<Node, Node>();
                    List<Interface.Edge<Node>> curEdges = new();
                    for (int i = 0, e = curNodes.Count - 1; i < e; i++)
                    {
                        curEdges.Add(board[curNodes[i]].GetEdge(curNodes[i + 1]));
                    }
#if LOW_DEBUGMESSAGE
                    result.Add(new(curNodes.ToArray(), curEdges.ToArray()));
#else
                    var temp = new NodePath(curNodes.ToArray(), curEdges.ToArray());
                    result.Add(temp);
                    DebugExtension.LogMessage(temp.ToString());
#endif
                }
                else
                {

#if LOW_DEBUGMESSAGE
#else
                    DebugExtension.LogMessage("NodePath : none");
#endif
                }
            }
            return result.ToArray();
        }

        public override string ToString()
        {
            return "{" + nodes.LinkAndInsert(',') + "}";
        }

        public IEnumerator<Node> GetNodeEnumerator()
        {
            if (Nodes == null || Nodes.Length == 0)
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

    /// <summary>
    /// This class is a directed graph ,and the ability to support duplicate edges
    /// </summary>
    [Serializable]
    public class Graph : Interface.Graph<Node>
    {
        [SerializeField] private Node[] nodes;
        [SerializeField] private int nodes_head, nodes_tail;
        [SerializeField] private Interface.Edge<Node>[] edges;
        [SerializeField] private int edges_head, edges_tail;

        public Node[] Nodes { get => nodes; private set => nodes = value; }

        public Interface.Edge<Node>[] Edges { get => edges; private set => edges = value; }

        public bool IsDirty { get; private set; }

        public bool Add(Node node)
        {
            return Add(node, 16);
        }

        /// <summary>
        /// If need to increase capacity , the capacity that grows is a multiple of parameter "increase"
        /// </summary>
        /// <param name="node"></param>
        /// <param name="increase">The capacity that grows each time, and if it grows, the capacity that grows is a multiple of this parameter</param>
        /// <returns></returns>
        public bool Add(Node node, int increase)
        {
            if (Nodes.Contains(node)) return false;
            IsDirty = true;
            if (nodes_tail < Nodes.Length)
            {
                Nodes[nodes_head] = node;
                nodes_tail++;
                return true;
            }
            else
            {
                Node[] new_nodes = new Node[nodes.Length + increase];
                for (int i = 0; i < Nodes.Length; i++)
                {
                    new_nodes[i] = Nodes[i];
                }
                Nodes = new_nodes;
                return Add(node, increase);
            }
        }

        public bool Add(Interface.Edge<Node> edge)
        {
            return Add(edge, 16);
        }

        /// <summary>
        /// If need to increase capacity , the capacity that grows is a multiple of parameter "increase"
        /// </summary>
        /// <param name="node"></param>
        /// <param name="increase">The capacity that grows each time, and if it grows, the capacity that grows is a multiple of this parameter</param>
        /// <returns></returns>
        public bool Add(Edge edge, int increase = 16)
        {
            return Add(edge, increase);
        }

        /// <summary>
        /// If need to increase capacity , the capacity that grows is a multiple of parameter "increase"
        /// </summary>
        /// <param name="node"></param>
        /// <param name="increase">The capacity that grows each time, and if it grows, the capacity that grows is a multiple of this parameter</param>
        /// <returns></returns>
        public bool Add(Interface.Edge<Node> edge, int increase)
        {
            if(!edge.Convertible<Edge>())
            {
#if JUST_EDGE_DEFAULT
#else
                Debug.LogWarning("The edge type is not expect");
#endif
            }
            if (Edges.Contains(edge)) return false;
            IsDirty = true;
            if (edges_tail < Edges.Length)
            {
                Edges[edges_head] = edge;
                edges_tail++;
                return true;
            }
            else
            {
                Interface.Edge<Node>[] new_edges = new Interface.Edge<Node>[Edges.Length + increase];
                for (int i = 0; i < Edges.Length; i++)
                {
                    new_edges[i] = Edges[i];
                }
                Edges = new_edges;
                return Add(edge, increase);
            }
        }

        public ADOrderlyEvent OnRebuild = new();

        /// <summary>
        /// <see cref="IRebuildHandle"/>::<see cref="OnRebuild"/>
        /// </summary>
        /// <param name="action">When really rebuild , it will be callback</param>
        public void AddListener(Action action)
        {
            OnRebuild.AddListener(action);
        }

        public bool BreakEdge(Interface.Edge<Node> edge)
        {
            if (edge == null) return false;
            for (int i = edges_head; i < edges_tail; i++)
            {
                if (Edges[i] == edge)
                {
                    Edges[i] = null;
                    if (i == edges_head) edges_head++;
                    else if (i == edges_tail - 1) edges_tail--;
                    IsDirty = true;
                    return true;
                }
            }
            return false;
        }

        public bool Contains(Node node)
        {
            for (int i = nodes_head;i<nodes_tail;i++)
            {
                if (Nodes[i] == node && Nodes[i]) return true;  
            }
            return false;
        }

        public bool Contains(Interface.Edge<Node> edge)
        {
            for(int i = edges_head;i<edges_tail;i++)
            {
                if (Edges[i] == edge) return true;
            }
            return false;
        }

        public IEnumerator<Interface.Iterator<Node>> GetEnumerator()
        {
            return GetNodeIter();
        }

        private readonly Queue<NodeIter> nodeIterContainer = new();

        public NodesIter GetNodeIter()
        {
            List<Node> temp_nodes = new();
            for (int i = nodes_head;i<nodes_tail;i++)
            {
                if (Nodes[i])
                {
                    temp_nodes.Add(Nodes[i]);
                }
            }
            nodeIterContainer.Enqueue(new NodeIter(temp_nodes.ToArray(), this).Share(out var temp));
            return new NodesIter(temp);
        }

        public class NodesIter: IEnumerator<Interface.Iterator<Node>>
        {
            public NodeIter Iter;

            public NodesIter(NodeIter iter)
            {
                Iter = iter;
            }

            public Interface.Iterator<Node> Current => Iter;

            object IEnumerator.Current => Iter;

            public void Dispose()
            {
                Iter.Dispose();
            }

            public bool MoveNext()
            {
                return Iter.MoveNext();
            }

            public void Reset()
            {
                Iter.Reset();
            }
        }

        /// <summary>
        /// It prioritizes the use of unreleased effective space, which is slightly slower than the <see cref="Add(Interface.Edge{Node})"/> but has better advantage
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public bool InsertEdge(Interface.Edge<Node> edge)
        {
            if (edge == null) return false;
            IsDirty = true;
            if (edges_head > 0)
            {
                edges_head--;
                Edges[edges_head] = edge;
                return true;
            }
            for (int i = edges_head; i < edges_tail; i++)
            {
                if (Edges[i] == null)
                {
                    Edges[i] = edge;
                    edges_tail++;
                    return true;
                }
            }
            return Add(edge);
        }

        /// <summary>
        /// It is much more efficient than when using <see cref="Add(Edge, int)"/> cycle addition alone
        /// </summary>
        /// <param name="edge"></param>
        /// <returns></returns>
        public int InsertEdges(params Interface.Edge<Node>[] edges)
        {
            if (edges == null || edges.Length == 0) return 0;
            IsDirty = true;
            int counter = 0;
            int offset = 0;
            while (edges_head > 0)
            {
                Interface.Edge<Node> curr = edges[counter + offset];
                if (curr != null && curr.From && curr.To)
                {
                    edges_head--;
                    Edges[edges_head] = curr;
                    counter++;
                }
                else offset++;
            }
            for (int i = edges_head; i < edges_tail; i++)
            {
                if (Edges[i] == null)
                {
                    Interface.Edge<Node> curr = edges[counter + offset];
                    if (curr != null && curr.From && curr.To)
                    {
                        Edges[i] = curr;
                        counter++;
                    }
                    else offset++;
                }
            }
            if (edges_tail + edges.Length - counter - offset < Edges.Length)
            {
                while (counter + offset < edges.Length)
                {
                    Edges[edges_head] = edges[counter + offset];
                    edges_tail++;
                    counter++;
                }
            }
            else
            {
                Interface.Edge<Node>[] new_edges = new Interface.Edge<Node>[Edges.Length + (((edges.Length - counter - offset) / 16) + 1) * 16];
                for (int i = 0; i < Edges.Length; i++)
                {
                    new_edges[i] = Edges[i];
                }
                Edges = new_edges;
                counter += InsertEdges(edges[(counter + offset + 1)..]);
            }
            return counter;
        }

        public Dictionary<Node, NodeEdgeLink<Node>> LeadWithNode()
        {
            Dictionary<Node, NodeEdgeLink<Node>> result = new();
            for (int i = nodes_head; i < nodes_tail; i++)
            {
                if (Nodes[i])
                {
                    result.Add(Nodes[i], new(Nodes[i]));
                }
            }
            for (int i = edges_head; i < edges_tail; i++)
            {
                if (Edges[i].To)
                    result[Edges[i].From].Links.Add(Edges[i].To, Edges[i]);
            }
            return result;
        }

        public void Rebuild(bool isImmediately)
        {
            if (IsDirty || isImmediately)
            {
                IsDirty = false;
                while (nodeIterContainer.Count > 0)
                {
                    nodeIterContainer.Dequeue().SetLost();
                }
                int offset = 0;
                Node[] new_nodes = new Node[Nodes.Length]; 
                for (int i = 0, e = nodes_tail; nodes_head + i < e; i++)
                {
                    var cur = Nodes[nodes_head + i];
                    if (cur == null) offset++;
                    else new_nodes[i - offset] = cur;
                }
                Nodes = new_nodes;
                nodes_tail = nodes_tail - nodes_head - offset;
                nodes_head = 0;

                offset = 0;

                Interface.Edge<Node>[] new_edges = new Interface.Edge<Node>[Edges.Length];
                for (int i = 0, e = edges_tail; edges_head + i < e; i++)
                {
                    var cur = Edges[edges_head + i];
                    if (cur == null) offset++;
                    else new_edges[i - offset] = cur;
                }
                Edges = new_edges;
                edges_tail = edges_tail - edges_head - offset;
                edges_head = 0;

                OnRebuild.Invoke();
            }
        }

        public bool Remove(Node node)
        {
            for (int i = nodes_head; i < nodes_tail; i++)
            {
                if (Nodes[i] == node)
                {
                    Nodes[i] = null;
                    if (i == nodes_head) nodes_head++;
                    else if (i == nodes_tail - 1) nodes_tail--;
                    IsDirty = true;
                    return true;
                }
            }
            return false;
        }

        public bool Remove(Interface.Edge<Node> edge)
        {
            return BreakEdge(edge);
        }

        public void RemoveAllListeners()
        {
            OnRebuild.RemoveAllListeners();
        }

        public void RemoveListener(Action action)
        {
            OnRebuild.RemoveListener(action);
        }

        public void SetDiry()
        {
            IsDirty = true;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
#pragma warning restore IDE1006 // 命名样式
