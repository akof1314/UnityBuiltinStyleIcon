using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Experimental;
using UnityEditor.IMGUI.Controls;

namespace WuHuan
{
    internal class BuiltinStyleIconWindow : EditorWindow
    {
        static class Styles
        {
            public static GUIContent titleContent = new GUIContent("BuiltinStyleIconWindow");
            public static GUIContent styleContent = new GUIContent("Styles");
            public static GUIContent iconContent = new GUIContent("Icons");
            public static GUIStyle searchNoResult = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                name = "icon-search-no-result",
                fontSize = 20,
                wordWrap = true
            };
        }

        [MenuItem("Window/Builtin Style Icon Window")]
        static void ShowWindow()
        {
            var window = GetWindow<BuiltinStyleIconWindow>();
            window.titleContent = Styles.titleContent;
            window.minSize = new Vector2(300, 330);
            window.Show();
        }

        enum IconTab
        {
            Styles,
            Icons,
            All
        }

        class IconInfo
        {
            public string assetPath;
            public string darkPath;
            public bool isDarkAsset;
        }

        [Serializable]
        class TreeViewStateTab : TreeViewState
        {
            public IconTab iconTab { get { return m_IconTab; } }
            [SerializeField]
            private IconTab m_IconTab = IconTab.All;

            [SerializeField]
            private Vector2 m_ScrollPos1;
            [SerializeField]
            private Vector2 m_ScrollPos2;

            public void SetIconTab(IconTab tab)
            {
                if (tab == m_IconTab)
                {
                    return;
                }

                switch (m_IconTab)
                {
                    case IconTab.Styles:
                        {
                            m_ScrollPos1 = scrollPos;
                        }
                        break;
                    case IconTab.Icons:
                        {
                            m_ScrollPos2 = scrollPos;
                        }
                        break;
                }

                m_IconTab = tab;
                switch (m_IconTab)
                {
                    case IconTab.Styles:
                        {
                            scrollPos = m_ScrollPos1;
                        }
                        break;
                    case IconTab.Icons:
                        {
                            scrollPos = m_ScrollPos2;
                        }
                        break;
                }
            }
        }

        class IconTreeViewItem : TreeViewItem
        {
            public float titleWidth { get; private set; }
            public Vector2 size { get; private set; }
            public Vector2 sizeLabel { get; private set; }
            public GUIStyle style { get; set; }
            public Texture2D texture { get; set; }
            public bool isToggle { get; set; }
            public bool valToggle { get; set; }

            public IconTreeViewItem(string title, float titleWidth, Vector2 size, Vector2 sizeLabel) : base(0, 0, title)
            {
                this.titleWidth = titleWidth;
                this.size = size;
                this.sizeLabel = sizeLabel;
            }
        }

        class IconTreeView : TreeView
        {
            const int kDefaultSpacing = 6;

            private List<IconTreeViewItem> m_Models;
            private readonly GUIContent m_Content;

            public IconTreeView(TreeViewState state) : base(state)
            {
                showBorder = false;
                m_Content = new GUIContent();
            }

            protected override TreeViewItem BuildRoot()
            {
                var root = new TreeViewItem { id = -1, depth = -1, displayName = "Root" };

                if (m_Models.Count > 0)
                {
                    for (var i = 0; i < m_Models.Count; i++)
                    {
                        var item = m_Models[i];
                        root.AddChild(item);
                    }
                }

                return root;
            }

            protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
            {
                return base.BuildRows(root);
            }

            protected override void RowGUI(RowGUIArgs args)
            {
                var item = args.item as IconTreeViewItem;
                if (item == null)
                {
                    base.RowGUI(args);
                    return;
                }

                var rowRect = args.rowRect;
                var headerRect = rowRect;
                headerRect.height = EditorStyles.toolbar.fixedHeight;
                GUI.Box(headerRect, GUIContent.none, EditorStyles.toolbar);

                var titleWidth = Mathf.Max(item.titleWidth, 250f);
                var titleButton = headerRect;
                titleButton.width = titleWidth;
                if (GUI.Button(titleButton, item.displayName, EditorStyles.toolbarButton))
                {
                    EditorGUIUtility.systemCopyBuffer = "\"" + item.displayName + "\"";
                }

                var styleRect = rowRect;
                styleRect.y += headerRect.height + kDefaultSpacing;
                styleRect.x += kDefaultSpacing;
                styleRect.height = item.size.y;
                styleRect.width = item.size.x;
                if (item.texture)
                {
                    EditorGUI.DrawTextureTransparent(styleRect, item.texture);
                }
                else
                {
                    GUI.Button(styleRect, GUIContent.none, item.style);
                }

                var minLeft = styleRect.width + kDefaultSpacing * 6;
                var adjustLeft = Mathf.Max(minLeft, titleWidth);
                if ((adjustLeft + item.sizeLabel.x + kDefaultSpacing * 2) > rowRect.width)
                {
                    adjustLeft = Mathf.Max(minLeft, rowRect.width - item.sizeLabel.x - kDefaultSpacing * 2);
                }
                styleRect.x += adjustLeft;
                styleRect.height = item.sizeLabel.y;
                styleRect.width = item.sizeLabel.x;

                m_Content.image = null;
                m_Content.text = item.displayName;
                if (!item.texture)
                {
                    if (item.isToggle)
                    {
                        item.valToggle = GUI.Toggle(styleRect, item.valToggle, m_Content, item.style);
                    }
                    else
                    {
                        GUI.Button(styleRect, m_Content, item.style);
                    }
                }
            }

            protected override float GetCustomRowHeight(int row, TreeViewItem item)
            {
                var iconItem = item as IconTreeViewItem;
                if (iconItem != null)
                {
                    return Mathf.Max(iconItem.size.y, iconItem.sizeLabel.y) + EditorStyles.toolbar.fixedHeight + kDefaultSpacing * 2;
                }
                return base.GetCustomRowHeight(row, item);
            }

            public void SetModels(List<IconTreeViewItem> models)
            {
                m_Models = models;
                Reload();
            }
        }

        [SerializeField]
        private TreeViewStateTab m_TreeViewState;
        private IconTreeView m_TreeView;
        private SearchField m_SearchField;
        private List<IconTreeViewItem> m_IconStyles;
        private List<IconTreeViewItem> m_IconIcons;

        private void OnGUI()
        {
            InitIfNeeded();
            DoToolbar();
            DoTreeView();
        }

        private void DoToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(m_TreeViewState.iconTab == IconTab.Styles, Styles.styleContent, EditorStyles.toolbarButton, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                SetStylesTab();
            }
            EditorGUI.BeginChangeCheck();
            GUILayout.Toggle(m_TreeViewState.iconTab == IconTab.Icons, Styles.iconContent, EditorStyles.toolbarButton, GUILayout.Width(100));
            if (EditorGUI.EndChangeCheck())
            {
                SetIconsTab();
            }
            GUILayout.FlexibleSpace();
            m_TreeView.searchString = m_SearchField.OnToolbarGUI(m_TreeView.searchString);
            EditorGUILayout.EndHorizontal();
        }

        private void DoTreeView()
        {
            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
            m_TreeView.OnGUI(rect);

            if (m_TreeView.hasSearch && m_TreeView.GetRows().Count == 0)
            {
                GUI.Box(rect, string.Format("No results for \"{0}\"", m_TreeView.searchString), Styles.searchNoResult);
            }
        }

        private void InitIfNeeded()
        {
            if (m_SearchField != null)
            {
                return;
            }

            m_TreeViewState = m_TreeViewState ?? new TreeViewStateTab();
            m_TreeView = new IconTreeView(m_TreeViewState);
            m_SearchField = new SearchField();
            if (m_TreeViewState.iconTab == IconTab.Icons)
            {
                SetIconsTab();
            }
            else
            {
                SetStylesTab();
            }
        }

        private void SetStylesTab()
        {
            if (m_IconStyles == null)
            {
                m_IconStyles = new List<IconTreeViewItem>();
                GUIContent tempContent = new GUIContent();

                int id = 1;
                foreach (GUIStyle style in GUI.skin)
                {
                    tempContent.text = "";
                    var size = style.CalcSize(tempContent);
                    tempContent.text = style.name;
                    var sizeLabel = style.CalcSize(tempContent);
                    var isToggle = style.name.Contains("Toggle") || style.name.Contains("toggle");

                    m_IconStyles.Add(new IconTreeViewItem(style.name, 250f, size, sizeLabel)
                    {
                        style = style,
                        isToggle = isToggle,
                        id = id++
                    });
                }
            }

            m_TreeViewState.SetIconTab(IconTab.Styles);
            m_TreeView.SetModels(m_IconStyles);
        }

        private void SetIconsTab()
        {
            if (m_IconIcons == null)
            {
                m_IconIcons = new List<IconTreeViewItem>();
                GUIContent tempContent = new GUIContent();
                GUIStyle style = EditorStyles.toolbarButton;

                var generatedIconsPath = EditorResources.generatedIconsPath;
                var iconsPath = EditorResources.iconsPath;
                var ab = GetEditorAssetBundle();
                var assetNames = ab.GetAllAssetNames();
                var textureByName = new Dictionary<string, IconInfo>();

                foreach (var assetName in assetNames)
                {
                    if (assetName.StartsWith(generatedIconsPath, StringComparison.OrdinalIgnoreCase) ||
                        assetName.StartsWith(iconsPath, StringComparison.OrdinalIgnoreCase))
                    {
                        textureByName.Add(assetName, new IconInfo
                        {
                            assetPath = assetName
                        });
                    }
                }

                foreach (var kv in textureByName)
                {
                    var assetName = kv.Key;
                    var pos = assetName.LastIndexOf("/d_", StringComparison.Ordinal);
                    if (pos > -1)
                    {
                        var lightPath = assetName.Substring(0, pos + 1) + assetName.Substring(pos + 3);

                        IconInfo lightInfo;
                        if (textureByName.TryGetValue(lightPath, out lightInfo))
                        {
                            lightInfo.darkPath = assetName;

                            kv.Value.isDarkAsset = true;
                        }
                    }
                }

                int id = 10000001;
                bool isProSkin = EditorGUIUtility.isProSkin;
                foreach (var assetName in assetNames)
                {
                    IconInfo iconInfo;
                    if (textureByName.TryGetValue(assetName, out iconInfo))
                    {
                        if (!iconInfo.isDarkAsset)
                        {
                            bool isLoadDark = isProSkin && !string.IsNullOrEmpty(iconInfo.darkPath);
                            var tex = ab.LoadAsset<Texture2D>(isLoadDark ? iconInfo.darkPath : iconInfo.assetPath);
                            if (tex)
                            {
                                var size = new Vector2(tex.width, tex.height);
                                tempContent.text = isLoadDark ? tex.name.Substring(2) : tex.name;
                                var titleWidth = style.CalcSize(tempContent).x;

                                m_IconIcons.Add(new IconTreeViewItem(tempContent.text, titleWidth, size, Vector2.zero)
                                {
                                    texture = tex,
                                    id = id++
                                });
                            }
                        }
                    }
                }
            }

            m_TreeViewState.SetIconTab(IconTab.Icons);
            m_TreeView.SetModels(m_IconIcons);
        }

        private static AssetBundle GetEditorAssetBundle()
        {
            var methodInfo = typeof(EditorGUIUtility).GetMethod("GetEditorAssetBundle", BindingFlags.NonPublic | BindingFlags.Static);
            return methodInfo.Invoke(null, new object[] { }) as AssetBundle;
        }
    }
}
