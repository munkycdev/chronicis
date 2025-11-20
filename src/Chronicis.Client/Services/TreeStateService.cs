using Chronicis.Shared.DTOs;

namespace Chronicis.Client.Services
{
    /// <summary>
    /// Manages the state of the article tree including selection and expansion.
    /// This service is scoped to maintain state during user session.
    /// </summary>
    public class TreeStateService
    {
        private int? _selectedArticleId;
        private readonly HashSet<int> _expandedNodes = new();

        public event Action? OnStateChanged;

        /// <summary>
        /// Currently selected article ID.
        /// </summary>
        public int? SelectedArticleId
        {
            get => _selectedArticleId;
            set
            {
                if (_selectedArticleId != value)
                {
                    _selectedArticleId = value;
                    NotifyStateChanged();
                }
            }
        }

        /// <summary>
        /// Set of article IDs that are currently expanded in the tree.
        /// </summary>
        public IReadOnlySet<int> ExpandedNodes => _expandedNodes;

        /// <summary>
        /// Toggle expansion state of a node.
        /// </summary>
        public void ToggleExpansion(int articleId)
        {
            if (_expandedNodes.Contains(articleId))
            {
                _expandedNodes.Remove(articleId);
            }
            else
            {
                _expandedNodes.Add(articleId);
            }
            NotifyStateChanged();
        }

        /// <summary>
        /// Expand a specific node.
        /// </summary>
        public void ExpandNode(int articleId)
        {
            if (_expandedNodes.Add(articleId))
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// Collapse a specific node.
        /// </summary>
        public void CollapseNode(int articleId)
        {
            if (_expandedNodes.Remove(articleId))
            {
                NotifyStateChanged();
            }
        }

        /// <summary>
        /// Check if a node is expanded.
        /// </summary>
        public bool IsExpanded(int articleId) => _expandedNodes.Contains(articleId);

        private void NotifyStateChanged() => OnStateChanged?.Invoke();
    }
}
