﻿namespace UnityDropdown.Editor
{
    using System.Collections.Generic;
    using SolidUtilities.Editor.Helpers;
    using SolidUtilities.Editor.Helpers.EditorIconsRelated;
    using SolidUtilities;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;

    /// <summary>
    /// A node in the selection tree. It may be a folder or an item that represents <see cref="System.Type"/>.
    /// </summary>
    public abstract class SelectionNode
    {
        protected readonly string _name;
        private readonly SelectionNode _parentNode;
        private bool _expanded;

        private Rect _rect;
        public Rect Rect => _rect;

        public string SearchName { get; }

        /// <summary>
        /// If the node is folder, this shows whether is is expanded or closed. If the node is type item, setting this
        /// will do nothing, and its value is always false.
        /// </summary>
        public bool Expanded
        {
            get => IsFolder && _expanded;
            set => _expanded = value;
        }

        public bool IsFolder => _ChildNodes.Count != 0;

        public bool IsRoot => _parentNode == null;

        public bool IsSelected => ParentTree.SelectedNode == this;

        protected abstract SelectionTree ParentTree { get; }

        protected abstract IReadOnlyCollection<SelectionNode> _ChildNodes { get; }

        private bool IsHoveredOver => _rect.Contains(Event.current.mousePosition);

        protected SelectionNode(SelectionNode parentNode, string name, string searchName)
        {
            _parentNode = parentNode;
            Assert.IsNotNull(name);
            _name = name;
            SearchName = searchName;
        }

        public IEnumerable<SelectionNode> GetParentNodesRecursive(
            bool includeSelf)
        {
            if (includeSelf)
                yield return this;

            if (IsRoot)
                yield break;

            foreach (SelectionNode node in _parentNode.GetParentNodesRecursive(true))
                yield return node;
        }

        public virtual void DrawSelfAndChildren(int indentLevel, Rect visibleRect)
        {
            Draw(indentLevel, visibleRect);
            if ( ! Expanded)
                return;

            foreach (SelectionNode childItem in _ChildNodes)
                childItem.DrawSelfAndChildren(indentLevel + 1, visibleRect);
        }

        /// <summary>Reserves a space for the rect but does not draw its content.</summary>
        /// <returns>True if there is no need to draw the contents.</returns>
        protected bool ReserveSpaceAndStop()
        {
            Rect rect = GUILayoutUtility.GetRect(0f, DropdownStyle.NodeHeight);

            if (Event.current.type == EventType.Layout)
                return true;

            if (Event.current.type == EventType.Repaint || _rect.width == 0f)
                _rect = rect;

            return false;
        }

        protected void DrawNodeContent(int indentLevel, int raiseText = 0)
        {
            if (IsSelected)
            {
                EditorGUI.DrawRect(_rect, DropdownStyle.SelectedColor);
            }
            else if (IsHoveredOver)
            {
                EditorGUI.DrawRect(_rect, DropdownStyle.HighlightedColor);
            }

            Rect indentedNodeRect = _rect;
            indentedNodeRect.xMin += DropdownStyle.GlobalOffset + indentLevel * DropdownStyle.IndentWidth;
            indentedNodeRect.y -= raiseText;

            if (IsFolder)
            {
                Rect triangleRect = GetTriangleRect(indentedNodeRect);
                DrawTriangleIcon(triangleRect);
            }

            DrawLabel(indentedNodeRect);

            DrawSeparator();
        }

        protected abstract void SetSelfSelected();

        protected void HandleMouseEvents()
        {
            bool leftMouseButtonWasPressed = Event.current.type == EventType.MouseDown
                                             && IsHoveredOver
                                             && Event.current.button == 0;

            if ( ! leftMouseButtonWasPressed)
                return;

            if (IsFolder)
            {
                Expanded = !Expanded;
            }
            else
            {
                SetSelfSelected();
                ParentTree.FinalizeSelection();
            }

            Event.current.Use();
        }

        private void Draw(int indentLevel, Rect visibleRect)
        {
            if (ReserveSpaceAndStop())
                return;

            if (_rect.y > 1000f && NodeIsOutsideOfVisibleRect(visibleRect))
                return;

            if (Event.current.type == EventType.Repaint)
                DrawNodeContent(indentLevel);

            HandleMouseEvents();
        }

        private bool NodeIsOutsideOfVisibleRect(Rect visibleRect) =>
            _rect.y + _rect.height < visibleRect.y || _rect.y > visibleRect.y + visibleRect.height;

        private static Rect GetTriangleRect(Rect nodeRect)
        {
            Rect triangleRect = nodeRect.AlignMiddleVertically(DropdownStyle.IconSize);
            triangleRect.width = DropdownStyle.IconSize;
            triangleRect.x -= DropdownStyle.IconSize;
            return triangleRect;
        }

        private void DrawTriangleIcon(Rect triangleRect)
        {
            EditorIcon triangleIcon = Expanded ? EditorIcons.TriangleDown : EditorIcons.TriangleRight;

            Texture2D tintedIcon = IsHoveredOver
                ? triangleIcon.Highlighted
                : triangleIcon.Active;

            tintedIcon.Draw(triangleRect);
        }

        private void DrawLabel(Rect indentedNodeRect)
        {
            Rect labelRect = indentedNodeRect.AlignMiddleVertically(DropdownStyle.LabelHeight);
            string label = ParentTree.DrawInSearchMode ? SearchName : _name;
            GUIStyle style = IsSelected ? DropdownStyle.SelectedLabelStyle : DropdownStyle.DefaultLabelStyle;
            GUI.Label(labelRect, label, style);
        }

        private void DrawSeparator()
        {
            var lineRect = new Rect(_rect.x, _rect.y - 1f, _rect.width, 1f);
            EditorGUI.DrawRect(lineRect, DropdownStyle.DarkSeparatorLine);
            ++lineRect.y;
            EditorGUI.DrawRect(lineRect, DropdownStyle.LightSeparatorLine);
        }
    }
}