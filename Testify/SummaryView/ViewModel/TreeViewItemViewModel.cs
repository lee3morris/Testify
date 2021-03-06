﻿using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;

namespace Leem.Testify.SummaryView.ViewModel
{
    /// <summary>
    /// Base class for all ViewModel classes displayed by TreeViewItems.
    /// This acts as an adapter between a raw data object and a TreeViewItem.
    /// </summary>
    public class TreeViewItemViewModel : INotifyPropertyChanged
    {
        #region Data

        private static readonly TreeViewItemViewModel DummyChild = new TreeViewItemViewModel();

        private readonly ObservableCollection<TreeViewItemViewModel> _children;
        private readonly TreeViewItemViewModel _parent;

        private bool _isExpanded;
        private bool _isSelected;
        private bool _shouldShowSummary;

        private string _type;

        #endregion Data

        #region Constructors

        public event EventHandler<CoverageChangedEventArgs> CoverageChanged;

        public TreeViewItemViewModel(TreeViewItemViewModel parent, bool lazyLoadChildren)
        {
            _parent = parent;

            _children = new ObservableCollection<TreeViewItemViewModel>();

            if (lazyLoadChildren)
                _children.Add(DummyChild);
        }

        // This is used to create the DummyChild instance.
        internal TreeViewItemViewModel()
        {
            _children = new ObservableCollection<TreeViewItemViewModel>();
        }

        #endregion Constructors

        #region Presentation Members

        #region Children

        /// <summary>
        /// Returns the logical child items of this object.
        /// </summary>
        public ObservableCollection<TreeViewItemViewModel> Children
        {
            get { return _children; }
        }

        #endregion Children

        #region HasLoadedChildren

        /// <summary>
        /// Returns true if this object's Children have not yet been populated.
        /// </summary>
        public bool HasDummyChild
        {
            get { return this.Children.Count == 1 && this.Children[0] == DummyChild; }
        }

        #endregion HasLoadedChildren

        #region IsExpanded

        /// <summary>
        /// Gets/sets whether the TreeViewItem
        /// associated with this object is expanded.
        /// </summary>
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set
            {
                if (value != _isExpanded)
                {
                    _isExpanded = value;
                    this.OnPropertyChanged("IsExpanded");
                }

                // Expand all the way up to the root.
                if (_isExpanded && _parent != null)
                    _parent.IsExpanded = true;

                // Lazy load the child items, if necessary.
                if (this.HasDummyChild)
                {
                    this.Children.Remove(DummyChild);
                    this.LoadChildren();
                }
            }
        }

        #endregion IsExpanded

        #region IsSelected

        /// <summary>
        /// Gets/sets whether the TreeViewItem
        /// associated with this object is selected.
        /// </summary>
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (value != _isSelected)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        #endregion IsSelected

        #region LoadChildren

        /// <summary>
        /// Invoked when the child items need to be loaded on demand.
        /// Subclasses can override this to populate the Children collection.
        /// </summary>
        protected virtual void LoadChildren()
        {
        }

        #endregion LoadChildren

        #region Parent

        public TreeViewItemViewModel Parent
        {
            get { return _parent; }
        }

        #endregion Parent

        protected virtual void OnCoverageChanged()
        {
            var args = new CoverageChangedEventArgs { DisplaySequenceCoverage = DisplaySequenceCoverage };
            if (CoverageChanged != null)
            {
                CoverageChanged(this, args);
            }
        }

        private bool _displaySequenceCoverage;

        public bool DisplaySequenceCoverage
        {
            get
            {
                return _displaySequenceCoverage;
            }
            set
            {
                _displaySequenceCoverage = value;
                OnCoverageChanged();
            }
        }

        public bool DisplayBranchCoverage
        {
            get
            {
                return !_displaySequenceCoverage;
            }
            set
            {
                _displaySequenceCoverage = !value;

            }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public bool ShouldShowSummary
        {
            get { return _shouldShowSummary; }
            set { _shouldShowSummary = value; }
        }

        public ImageSource Icon { get; set; }

        #endregion Presentation Members

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged Members

        public System.Threading.SynchronizationContext UiContext { get; set; }
    }
}