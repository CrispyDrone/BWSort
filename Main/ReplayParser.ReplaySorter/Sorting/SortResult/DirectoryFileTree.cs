using System.Collections;
using System.Collections.Generic;
using System.IO;
using ReplayParser.ReplaySorter.IO;
using System.Linq;
using System.Text;

namespace ReplayParser.ReplaySorter.Sorting.SortResult
{
    public class DirectoryFileTree<T> : IEnumerable<DirectoryFileTreeNode<T>> where T : IFile
    {
        #region private

        #region fields

        private DirectoryFileTreeNode<T> _root;

        #endregion

        #endregion

        #region public

        #region constructor

        public DirectoryFileTree(string rootName)
        {
            _root = new DirectoryFileTreeNode<T>(rootName);
            Count++;
        }

        #endregion

        #region properties

        public DirectoryFileTreeNode<T> Root => _root;
        public int Count { get; private set; } = 0;

        #endregion

        #region methods

        #region tree operations

        /// <summary>
        /// Adds a single node to the parent node. 
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="value"></param>
        /// <param name="isDirectory"></param>
        public DirectoryFileTreeNode<T> AddToNode(DirectoryFileTreeNode<T> parentNode, T value)
        {
            var newNode = parentNode.AddChild(value);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Add a new empty directory node.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="directoryName"></param>
        public DirectoryFileTreeNode<T> AddToNode(DirectoryFileTreeNode<T> parentNode, string directoryName)
        {
            var newNode = parentNode.AddDir(directoryName);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Adds a new directory based node and initializes it with the specified terminal values.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="terminalValues"></param>
        public DirectoryFileTreeNode<T> AddToNode(DirectoryFileTreeNode<T> parentNode, string directoryName, IEnumerable<T> terminalValues)
        {
            var newNode = parentNode.AddDirWithChildren(directoryName, terminalValues);
            Count += terminalValues.Count() + 1;
            return newNode;
        }

        /// <summary>
        /// Add a subtree.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tree"></param>
        public void AddToNode(DirectoryFileTreeNode<T> parentNode, DirectoryFileTree<T> tree)
        {
            parentNode.AddTree(tree.Root);
            Count += tree.Count;
        }

        #endregion

        #region enumerator

        public IEnumerator<DirectoryFileTreeNode<T>> GetEnumerator()
        {
            return _root.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        #endregion

        #endregion

        #endregion
    }

    public class DirectoryFileTree : IEnumerable<DirectoryFileTreeNode>
    {
        #region private

        #region fields

        private DirectoryFileTreeNode _root;

        #endregion

        #endregion

        #region public

        #region constructor

        public DirectoryFileTree(string rootName)
        {
            _root = new DirectoryFileTreeNode(rootName);
            Count++;
        }

        #endregion

        #region properties

        public DirectoryFileTreeNode Root => _root;
        public int Count { get; private set; } = 0;

        #endregion

        #region methods

        #region tree operations

        /// <summary>
        /// Adds a single node to the parent node. 
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="value"></param>
        /// <param name="isDirectory"></param>
        public DirectoryFileTreeNode AddToNode(DirectoryFileTreeNode parentNode, FileReplay value)
        {
            var newNode = parentNode.AddChild(value);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Add a new empty directory node.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="directoryName"></param>
        public DirectoryFileTreeNode AddToNode(DirectoryFileTreeNode parentNode, string directoryName)
        {
            var newNode = parentNode.AddDir(directoryName);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Adds a new directory based node and initializes it with the specified terminal values.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="terminalValues"></param>
        public DirectoryFileTreeNode AddToNode(DirectoryFileTreeNode parentNode, string directoryName, IEnumerable<FileReplay> terminalValues)
        {
            var newNode = parentNode.AddDirWithChildren(directoryName, terminalValues);
            Count += terminalValues.Count() + 1;
            return newNode;
        }

        /// <summary>
        /// Add a subtree.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tree"></param>
        public void AddToNode(DirectoryFileTreeNode parentNode, DirectoryFileTree tree)
        {
            parentNode.AddTree(tree.Root);
            Count += tree.Count;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var child in this)
            {
                sb.AppendLine(child.ToString());
            }
            return sb.ToString();
        }

        #endregion

        #region enumerator

        public IEnumerator<DirectoryFileTreeNode> GetEnumerator()
        {
            return _root.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<DirectoryFileTreeNode> GetBreadthFirstEnumerator()
        {
            return _root.GetBreadthFirstEnumerator();
        }

        #endregion

        #endregion

        #endregion
    }

    public class DirectoryFileTreeSimple : IEnumerable<DirectoryFileTreeNodeSimple>
    {
        #region private

        #region fields

        private DirectoryFileTreeNodeSimple _root;

        #endregion

        #endregion

        #region public

        #region constructor

        public DirectoryFileTreeSimple(string rootName)
        {
            _root = new DirectoryFileTreeNodeSimple(rootName);
            Count++;
        }

        #endregion

        #region properties

        public DirectoryFileTreeNodeSimple Root => _root;
        public int Count { get; private set; } = 0;

        #endregion

        #region methods

        #region tree operations

        /// <summary>
        /// Adds a single node to the parent node. 
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="value"></param>
        /// <param name="isDirectory"></param>
        public DirectoryFileTreeNodeSimple AddToNode(DirectoryFileTreeNodeSimple parentNode, SimpleFile value)
        {
            var newNode = parentNode.AddChild(value);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Add a new empty directory node.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="directoryName"></param>
        public DirectoryFileTreeNodeSimple AddToNode(DirectoryFileTreeNodeSimple parentNode, string directoryName)
        {
            var newNode = parentNode.AddDir(directoryName);
            Count++;
            return newNode;
        }

        /// <summary>
        /// Adds a new directory based node and initializes it with the specified terminal values.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="terminalValues"></param>
        public DirectoryFileTreeNodeSimple AddToNode(DirectoryFileTreeNodeSimple parentNode, string directoryName, IEnumerable<SimpleFile> terminalValues)
        {
            var newNode = parentNode.AddDirWithChildren(directoryName, terminalValues);
            Count += terminalValues.Count() + 1;
            return newNode;
        }

        /// <summary>
        /// Add a subtree.
        /// </summary>
        /// <param name="parentNode"></param>
        /// <param name="tree"></param>
        public void AddToNode(DirectoryFileTreeNodeSimple parentNode, DirectoryFileTreeSimple tree)
        {
            parentNode.AddTree(tree.Root);
            Count += tree.Count;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            foreach (var child in this)
            {
                sb.AppendLine(child.ToString());
            }
            return sb.ToString();
        }

        #endregion

        #region enumerator

        public IEnumerator<DirectoryFileTreeNodeSimple> GetEnumerator()
        {
            return _root.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IEnumerator<DirectoryFileTreeNodeSimple> GetBreadthFirstEnumerator()
        {
            return _root.GetBreadthFirstEnumerator();
        }

        #endregion

        #endregion

        #endregion
    }
}
