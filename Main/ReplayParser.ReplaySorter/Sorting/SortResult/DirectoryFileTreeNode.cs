using ReplayParser.ReplaySorter.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReplayParser.ReplaySorter.Sorting.SortResult
{
    public class DirectoryFileTreeNode<T> : IEnumerable<DirectoryFileTreeNode<T>> where T : IFile
    {
        #region private

        #region enumerator

        // public class BreadthFirstEnumerator : IEnumerator<DirectoryFileTreeNode<T>>
        // {
        //     private DirectoryFileTreeNode<T> _node;
        //     private Queue<DirectoryFileTreeNode<T>> _nodeQueue;

        //     public BreadthFirstEnumerator(DirectoryFileTreeNode<T> node)
        //     {
        //         _node = node;
        //         _nodeQueue = new Queue<DirectoryFileTreeNode<T>>();
        //         _nodeQueue.Enqueue(node);
        //     }

        //     public DirectoryFileTreeNode<T> Current => _nodeQueue.First();

        //     object IEnumerator.Current => Current;

        //     public void Dispose()
        //     {
        //         // No resources used...
        //     }

        //     public bool MoveNext()
        //     {
        //         if (_nodeQueue.Count == 0)
        //             return false;

        //         var head = _nodeQueue.Dequeue();
        //         if (head.Children != null)
        //         {
        //             foreach (var child in head.Children)
        //             {
        //                 if (child != null)
        //                     _nodeQueue.Enqueue(child);
        //             }
        //         }

        //         return true;
        //     }

        //     public void Reset()
        //     {
        //         _nodeQueue.Clear();
        //         _nodeQueue.Enqueue(_node);
        //     }
        // }

        #endregion

        #region fields

        private List<DirectoryFileTreeNode<T>> _children;
        private T _value;

        #endregion

        #region constructors

        public DirectoryFileTreeNode(T value)
        {
            //TODO prevent null?
            _value = value;
            Name = value.FilePath;
            IsDirectory = false;
        }

        public DirectoryFileTreeNode(string directoryName)
        {
            Name = directoryName;
            IsDirectory = true;
            _children = new List<DirectoryFileTreeNode<T>>();
        }

        public DirectoryFileTreeNode(string directoryName, IEnumerable<DirectoryFileTreeNode<T>> children)
        {
            Name = directoryName;
            IsDirectory = true;
            _children = children.ToList();
        }

        #endregion

        #endregion

        #region public

        #region enumerator
        // #region static factory constructor

        // public DirectoryFileTreeNode<T> Create(string name, bool IsDirectory)
        // {
        //     if (string.IsNullOrWhiteSpace(name)) return null;

        //     return new DirectoryFileTreeNode<T>(name, IsDirectory);
        // }

        // public DirectoryFileTreeNode<T> Create(string name, List<DirectoryFileTreeNode<T>> children)
        // {
        //     if (string.IsNullOrWhiteSpace(name)) return null;
        //     if (children == null) return null;

        //     return new DirectoryFileTreeNode<T>(name, children);
        // }

        public IEnumerator<DirectoryFileTreeNode<T>> GetEnumerator()
        {
            // return new BreadthFirstEnumerator(this);
            if (IsDirectory)
                return Children.GetEnumerator();

            return Enumerable.Empty<DirectoryFileTreeNode<T>>().GetEnumerator();

            // throw new InvalidOperationException("Can not iterate of a non-directory node!");
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region properties

        public string Name { get; }

        public T Value
        {
            get
            {
                if (IsDirectory)
                    ///TODO null instead of exception?
                    throw new InvalidOperationException("A directory does not have a value!");

                return _value;
            }
        }

        public bool IsDirectory { get; }

        public IEnumerable<DirectoryFileTreeNode<T>> Children
        {
            get
            {
                if (IsDirectory)
                    return _children;

                return null;
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Add terminal value. Only possible in case the node is a directory. Verify with the IsDirectory property.
        /// </summary>
        /// <param name="value"></param>
        public DirectoryFileTreeNode<T> AddChild(T value)
        {
            if (IsDirectory)
            {
                var newNode = new DirectoryFileTreeNode<T>(value);
                _children.Add(newNode);
                return newNode;
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        /// <summary>
        /// Adds a new directory based node as a child.
        /// </summary>
        /// <param name="directoryName"></param>
        public DirectoryFileTreeNode<T> AddDir(string directoryName)
        {
            if (IsDirectory)
            {
                var newNode = new DirectoryFileTreeNode<T>(directoryName);
                _children.Add(newNode);
                return newNode;
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        /// <summary>
        /// Adds a directory node with the specified terminal values.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="terminalValues"></param>
        public DirectoryFileTreeNode<T> AddDirWithChildren(string directoryName, IEnumerable<T> terminalValues)
        {
            if (IsDirectory)
            {
                var newNode = new DirectoryFileTreeNode<T>(directoryName, terminalValues.Select(c => new DirectoryFileTreeNode<T>(c)));
                _children.Add(newNode);
                return newNode;
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        /// <summary>
        /// Add a sub tree.
        /// </summary>
        /// <param name="node"></param>
        public void AddTree(DirectoryFileTreeNode<T> node)
        {
            if (IsDirectory)
            {
                if (node == null) return;
                _children.Add(node);
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        #endregion

        #endregion
    }

    public class DirectoryFileTreeNode : IEnumerable<DirectoryFileTreeNode>
    {
        #region private

        #region fields

        private List<DirectoryFileTreeNode> _children;
        private FileReplay _value;

        #endregion

        #region constructors

        public DirectoryFileTreeNode(FileReplay value)
        {
            //TODO prevent null?
            _value = value;
            Name = Path.GetFileName(value.FilePath);
            IsDirectory = false;
        }

        public DirectoryFileTreeNode(string directoryName)
        {
            Name = directoryName;
            IsDirectory = true;
            _children = new List<DirectoryFileTreeNode>();
        }

        public DirectoryFileTreeNode(string directoryName, IEnumerable<DirectoryFileTreeNode> children)
        {
            Name = directoryName;
            IsDirectory = true;
            _children = children.ToList();
        }

        #endregion

        #endregion

        #region public

        #region enumerator
        public IEnumerator<DirectoryFileTreeNode> GetEnumerator()
        {
            if (IsDirectory)
                return Children.GetEnumerator();

            return Enumerable.Empty<DirectoryFileTreeNode>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #region properties

        public string Name { get; }

        public FileReplay Value
        {
            get
            {
                if (IsDirectory)
                    ///TODO null instead of exception?
                    throw new InvalidOperationException("A directory does not have a value!");

                return _value;
            }
        }

        public bool IsDirectory { get; }

        public IEnumerable<DirectoryFileTreeNode> Children
        {
            get
            {
                if (IsDirectory)
                    return _children;

                return null;
            }
        }

        #endregion

        #region methods

        /// <summary>
        /// Add terminal value. Only possible in case the node is a directory. Verify with the IsDirectory property.
        /// </summary>
        /// <param name="value"></param>
        public DirectoryFileTreeNode AddChild(FileReplay value)
        {
            if (IsDirectory)
            {
                var newNode = new DirectoryFileTreeNode(value);
                _children.Add(newNode);
                return newNode;
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        /// <summary>
        /// Adds a new directory based node as a child.
        /// </summary>
        /// <param name="directoryName"></param>
        public DirectoryFileTreeNode AddDir(string directoryName)
        {
            if (IsDirectory)
            {
                var newNode = new DirectoryFileTreeNode(directoryName);
                _children.Add(newNode);
                return newNode;
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        /// <summary>
        /// Adds a directory node with the specified terminal values.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="terminalValues"></param>
        public DirectoryFileTreeNode AddDirWithChildren(string directoryName, IEnumerable<FileReplay> terminalValues)
        {
            if (IsDirectory)
            {
                var newNode = new DirectoryFileTreeNode(directoryName, terminalValues.Select(c => new DirectoryFileTreeNode(c)));
                _children.Add(newNode);
                return newNode;
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        /// <summary>
        /// Add a sub tree.
        /// </summary>
        /// <param name="node"></param>
        public void AddTree(DirectoryFileTreeNode node)
        {
            if (IsDirectory)
            {
                if (node == null) return;
                _children.Add(node);
            }
            else
            {
                throw new InvalidOperationException("This node is not a directory, can not add children to it!");
            }
        }

        public override string ToString()
        {
            return Name + ": " + Children?.Count() ?? "0";
        }

        #endregion

        #endregion

    }
}
