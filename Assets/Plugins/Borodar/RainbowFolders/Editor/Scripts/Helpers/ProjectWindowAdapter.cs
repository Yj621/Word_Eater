using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Borodar.RainbowFolders
{
    [InitializeOnLoad]
    public static class ProjectWindowAdapter
    {
        private const string EDITOR_WINDOW_TYPE = "UnityEditor.ProjectBrowser";

        private const BindingFlags STATIC_PRIVATE = BindingFlags.Static | BindingFlags.NonPublic;        
        private const BindingFlags STATIC_PUBLIC = BindingFlags.Static | BindingFlags.Public;
        private const BindingFlags INSTANCE_PRIVATE = BindingFlags.Instance | BindingFlags.NonPublic;
        private const BindingFlags INSTANCE_PUBLIC = BindingFlags.Instance | BindingFlags.Public;

        // --- Reflection targets (lazy) ---
        private static MethodInfo ALL_PROJECT_BROWSERS_METHOD;
        private static MethodInfo PROJECT_BROWSER_INITIALIZED_METHOD;

        private static FieldInfo PROJECT_VIEW_MODE_FIELD;
        private static FieldInfo PROJECT_ASSET_TREE_FIELD;
        private static FieldInfo PROJECT_FOLDER_TREE_FIELD;
        private static FieldInfo CONTROLLER_DRAG_SELECTION_FIELD;
        #if UNITY_2021_1_OR_NEWER
        private static FieldInfo INTEGER_CACHE_LIST_FIELD;
        #endif
        private static PropertyInfo CONTROLLER_DATA_PROPERTY;
        private static PropertyInfo CONTROLLER_STATE_PROPERTY;
        private static PropertyInfo CONTROLLER_GUI_CALLBACK_PROPERTY;
        private static MethodInfo CONTROLLER_HAS_FOCUS_METHOD;
        private static PropertyInfo STATE_SELECTED_IDS_PROPERTY;
        private static MethodInfo TWO_COLUMN_ITEMS_METHOD;
        private static MethodInfo ONE_COLUMN_ITEMS_METHOD;

        private static FieldInfo PROJECT_OBJECT_LIST_FIELD;
        private static FieldInfo PROJECT_LOCAL_ASSETS_FIELD;
        private static PropertyInfo OBJECT_LIST_REPAINT_CALLBACK;
        private static FieldInfo OBJECT_LIST_ICON_EVENT;
        private static PropertyInfo ASSETS_LIST_MODE_PROPERTY;
        private static FieldInfo LIST_FILTERED_HIERARCHY_FIELD;
        private static PropertyInfo FILTERED_HIERARCHY_RESULTS_PROPERTY;

        private static FieldInfo FILTER_RESULT_ID_FIELD;
        private static FieldInfo FILTER_RESULT_IS_FOLDER_FIELD;
        private static PropertyInfo FILTER_RESULT_ICON_PROPERTY;

        private static bool _initialized;
        private static bool _initTried;

        static ProjectWindowAdapter()
        {
            // 에디터가 완전히 올라온 뒤 한 번 시도
            EditorApplication.update += DelayedInitOnce;
        }

        private static void DelayedInitOnce()
        {
            EditorApplication.update -= DelayedInitOnce;
            TryInit();
        }

        private static void EnsureInit()
        {
            if (_initialized || _initTried) return;
            TryInit();
        }

        private static void TryInit()
        {
            _initTried = true;
            try
            {
                var assembly = typeof(EditorWindow).Assembly;

                // Project Browser
                var projectWindowType = assembly.GetType(EDITOR_WINDOW_TYPE);
                if (projectWindowType == null) return;

                ALL_PROJECT_BROWSERS_METHOD = projectWindowType.GetMethod("GetAllProjectBrowsers", STATIC_PUBLIC);
                PROJECT_BROWSER_INITIALIZED_METHOD = projectWindowType.GetMethod("Initialized", INSTANCE_PUBLIC)
                                                     ?? projectWindowType.GetMethod("get_Initialized", INSTANCE_PUBLIC);

                // First Column
                PROJECT_VIEW_MODE_FIELD   = projectWindowType.GetField("m_ViewMode",   INSTANCE_PRIVATE);
                PROJECT_ASSET_TREE_FIELD  = projectWindowType.GetField("m_AssetTree",  INSTANCE_PRIVATE);
                PROJECT_FOLDER_TREE_FIELD = projectWindowType.GetField("m_FolderTree", INSTANCE_PRIVATE);

                var treeViewControllerType = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewController");
                if (treeViewControllerType != null)
                {
                    CONTROLLER_DRAG_SELECTION_FIELD = treeViewControllerType.GetField("m_DragSelection", INSTANCE_PRIVATE)
                                                   ?? treeViewControllerType.GetField("m_DragSelectionInt", INSTANCE_PRIVATE);
                    #if UNITY_2021_1_OR_NEWER
                    var integerCacheType = treeViewControllerType.GetNestedType("IntegerCache", INSTANCE_PRIVATE)
                                        ?? treeViewControllerType.GetNestedType("IntCache", INSTANCE_PRIVATE);
                    if (integerCacheType != null)
                        INTEGER_CACHE_LIST_FIELD = integerCacheType.GetField("m_List", INSTANCE_PRIVATE)
                                                ?? integerCacheType.GetField("m_Values", INSTANCE_PRIVATE);
                    #endif
                    CONTROLLER_DATA_PROPERTY      = treeViewControllerType.GetProperty("data",  INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                 ?? treeViewControllerType.GetProperty("m_Data", INSTANCE_PRIVATE);
                    CONTROLLER_STATE_PROPERTY     = treeViewControllerType.GetProperty("state", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                 ?? treeViewControllerType.GetProperty("m_State", INSTANCE_PRIVATE);
                    CONTROLLER_GUI_CALLBACK_PROPERTY = treeViewControllerType.GetProperty("onGUIRowCallback", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                    ?? treeViewControllerType.GetProperty("rowGUI", INSTANCE_PRIVATE);
                    CONTROLLER_HAS_FOCUS_METHOD   = treeViewControllerType.GetMethod("HasFocus", INSTANCE_PUBLIC | INSTANCE_PRIVATE);
                }

                var treeViewState = assembly.GetType("UnityEditor.IMGUI.Controls.TreeViewState");
                if (treeViewState != null)
                {
                    STATE_SELECTED_IDS_PROPERTY = treeViewState.GetProperty("selectedIDs", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                               ?? treeViewState.GetProperty("m_SelectedIDs", INSTANCE_PRIVATE);
                }

                var oneColumnTreeViewDataType = assembly.GetType("UnityEditor.ProjectBrowserColumnOneTreeViewDataSource")
                                            ?? assembly.GetType("UnityEditor.ProjectBrowserColumnOneTreeViewDataSourceLegacy");
                TWO_COLUMN_ITEMS_METHOD = oneColumnTreeViewDataType?.GetMethod("GetRows", INSTANCE_PUBLIC | INSTANCE_PRIVATE);

                var twoColumnTreeViewDataType = assembly.GetType("UnityEditor.AssetsTreeViewDataSource")
                                            ?? assembly.GetType("UnityEditor.ProjectBrowserColumnTwoTreeViewDataSource");
                ONE_COLUMN_ITEMS_METHOD = twoColumnTreeViewDataType?.GetMethod("GetRows", INSTANCE_PUBLIC | INSTANCE_PRIVATE);

                // Second Column
                PROJECT_OBJECT_LIST_FIELD = projectWindowType.GetField("m_ListArea", INSTANCE_PRIVATE);

                var objectListType = assembly.GetType("UnityEditor.ObjectListArea");
                if (objectListType != null)
                {
                    PROJECT_LOCAL_ASSETS_FIELD  = objectListType.GetField("m_LocalAssets", INSTANCE_PRIVATE);
                    OBJECT_LIST_REPAINT_CALLBACK = objectListType.GetProperty("repaintCallback", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                ?? objectListType.GetProperty("onRepaint", INSTANCE_PRIVATE);
                    OBJECT_LIST_ICON_EVENT       = objectListType.GetField("postAssetIconDrawCallback", STATIC_PRIVATE)
                                                ?? objectListType.GetField("s_PostAssetIconDraw", STATIC_PRIVATE);

                    var localGroupType = objectListType.GetNestedType("LocalGroup", INSTANCE_PRIVATE)
                                      ?? objectListType.GetNestedType("LocalGroupInternal", INSTANCE_PRIVATE);
                    if (localGroupType != null)
                    {
                        ASSETS_LIST_MODE_PROPERTY   = localGroupType.GetProperty("ListMode", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                    ?? localGroupType.GetProperty("isListMode", INSTANCE_PRIVATE);
                        LIST_FILTERED_HIERARCHY_FIELD = localGroupType.GetField("m_FilteredHierarchy", INSTANCE_PRIVATE)
                                                      ?? localGroupType.GetField("m_Filtered", INSTANCE_PRIVATE);
                    }
                }

                var filteredHierarchyType = assembly.GetType("UnityEditor.FilteredHierarchy");
                if (filteredHierarchyType != null)
                {
                    FILTERED_HIERARCHY_RESULTS_PROPERTY = filteredHierarchyType.GetProperty("results", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                       ?? filteredHierarchyType.GetProperty("m_Results", INSTANCE_PRIVATE);

                    var filterResultType = filteredHierarchyType.GetNestedType("FilterResult", BindingFlags.Public | BindingFlags.NonPublic)
                                        ?? assembly.GetType("UnityEditor.FilterResult");
                    if (filterResultType != null)
                    {
                        FILTER_RESULT_ID_FIELD        = filterResultType.GetField("instanceID", INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                      ?? filterResultType.GetField("id", INSTANCE_PRIVATE);
                        FILTER_RESULT_IS_FOLDER_FIELD = filterResultType.GetField("isFolder",   INSTANCE_PUBLIC | INSTANCE_PRIVATE)
                                                      ?? filterResultType.GetField("folder",     INSTANCE_PRIVATE);
                        FILTER_RESULT_ICON_PROPERTY   = filterResultType.GetProperty("icon", INSTANCE_PUBLIC | INSTANCE_PRIVATE);
                    }
                }

                // 안전: 룰셋 콜백도 중복 구독 방지 + 예외 방지
                ProjectRuleset.OnRulesetChange -= ApplyDefaultIconsToSecondColumn;
                ProjectRuleset.OnRulesetChange += ApplyDefaultIconsToSecondColumn;

                _initialized = true;
            }
            catch (Exception e)
            {
                // 에디터 죽이지 말고 기능만 OFF
                Debug.LogWarning("[RainbowFolders] ProjectWindowAdapter init failed (safe-off): " + e.Message);
                _initialized = false;
            }
        }

        // -------------------------------------------------------
        // Public
        // -------------------------------------------------------

        [SuppressMessage("ReSharper", "ReturnTypeCanBeEnumerable.Global")]
        public static IReadOnlyList<EditorWindow> GetAllProjectWindows()
        {
            EnsureInit();
            if (ALL_PROJECT_BROWSERS_METHOD == null) return Array.Empty<EditorWindow>();
            var browsersList = ALL_PROJECT_BROWSERS_METHOD.Invoke(null, null);
            return browsersList as IReadOnlyList<EditorWindow> ?? Array.Empty<EditorWindow>();
        }

        public static EditorWindow GetFirstProjectWindow()
        {
            return GetAllProjectWindows().FirstOrDefault();
        }

        public static object GetAssetTreeController(EditorWindow window)
        {
            EnsureInit();
            return PROJECT_ASSET_TREE_FIELD?.GetValue(window);
        }

        public static object GetFolderTreeController(EditorWindow window)
        {
            EnsureInit();
            return PROJECT_FOLDER_TREE_FIELD?.GetValue(window);
        }

        public static object GetTreeViewState(object treeViewController)
        {
            EnsureInit();
            return CONTROLLER_STATE_PROPERTY?.GetValue(treeViewController);
        }

        public static bool HasChildren(EditorWindow window, int assetId)
        {
            var treeViewItems = GetFirstColumnItems(window);
            if (treeViewItems == null) return false;

            var treeViewItem = treeViewItems.FirstOrDefault(item => item.id == assetId);
            return treeViewItem != null && treeViewItem.hasChildren;
        }

        public static bool IsItemSelected(object treeViewController, object state, int assetId)
        {
            EnsureInit();

            #if UNITY_2021_1_OR_NEWER
            var dragSelectionField = CONTROLLER_DRAG_SELECTION_FIELD?.GetValue(treeViewController);
            var dragSelection = (List<int>)(INTEGER_CACHE_LIST_FIELD?.GetValue(dragSelectionField));
            #else
            var dragSelection = (List<int>) CONTROLLER_DRAG_SELECTION_FIELD?.GetValue(treeViewController);
            #endif

            if (dragSelection != null && dragSelection.Count > 0)
            {
                return dragSelection.Contains(assetId);
            }
            else
            {
                var selectedIds = (List<int>) STATE_SELECTED_IDS_PROPERTY?.GetValue(state);
                return selectedIds != null && selectedIds.Contains(assetId);
            }
        }

        public static bool HasFocus(object treeViewController)
        {
            EnsureInit();
            if (CONTROLLER_HAS_FOCUS_METHOD == null) return false;
            return (bool) (CONTROLLER_HAS_FOCUS_METHOD.Invoke(treeViewController, null) ?? false);
        }

        public static ViewMode GetProjectViewMode(EditorWindow window)
        {
            EnsureInit();
            if (PROJECT_VIEW_MODE_FIELD == null) return ViewMode.TwoColumns; // 안전 기본값
            return (ViewMode) PROJECT_VIEW_MODE_FIELD.GetValue(window);
        }

        public static bool ProjectWindowInitialized(EditorWindow window)
        {
            EnsureInit();
            if (PROJECT_BROWSER_INITIALIZED_METHOD == null) return true; // 초기화 못 캐치하면 true로 안전 동작
            return (bool) (PROJECT_BROWSER_INITIALIZED_METHOD.Invoke(window, null) ?? true);
        }

        public static object GetObjectListArea(EditorWindow window)
        {
            EnsureInit();
            return PROJECT_OBJECT_LIST_FIELD?.GetValue(window);
        }

        public static void ReplaceIconsInListArea(object objectListArea, ProjectRuleset ruleset)
        {
            EnsureInit();
            if (objectListArea == null || PROJECT_LOCAL_ASSETS_FIELD == null) return;

            var localAssets = PROJECT_LOCAL_ASSETS_FIELD.GetValue(objectListArea);
            if (localAssets == null) return;

            var inListMode = InListMode(localAssets);
            var filteredHierarchy = LIST_FILTERED_HIERARCHY_FIELD?.GetValue(localAssets);
            var items = FILTERED_HIERARCHY_RESULTS_PROPERTY?.GetValue(filteredHierarchy, null) as IEnumerable<object>;
            if (items == null) return;

            foreach (var item in items)
            {
                if (!ListItemIsFolder(item)) continue;
                var id = GetInstanceIdFromListItem(item);
                var path = AssetDatabase.GetAssetPath(id);
                var rule = ruleset.GetRuleByPath(path, true);
                if (rule == null || !rule.HasIcon()) continue;

                Texture2D iconTex = null;
                if (rule.HasCustomIcon())
                {
                    iconTex = inListMode ? rule.SmallIcon : rule.LargeIcon;
                }
                else
                {
                    var icons = ProjectIconsStorage.GetIcons(rule.IconType);
                    if (icons != null)
                        iconTex = inListMode ? icons.Item2 : icons.Item1;
                }

                if (iconTex != null) SetIconForListItem(item, iconTex);
            }
        }

        // -------------------------------------------------------
        // Callbacks
        // -------------------------------------------------------

        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        public static void AddOnGUIRowCallback(object treeViewController, Action<int, Rect> action)
        {
            EnsureInit();
            if (CONTROLLER_GUI_CALLBACK_PROPERTY == null) return;
            var value = (Action<int, Rect>) CONTROLLER_GUI_CALLBACK_PROPERTY.GetValue(treeViewController);
            CONTROLLER_GUI_CALLBACK_PROPERTY.SetValue(treeViewController, action + value);
        }

        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        public static void RemoveOnGUIRowCallback(object treeViewController, Action<int, Rect> action)
        {
            EnsureInit();
            if (CONTROLLER_GUI_CALLBACK_PROPERTY == null) return;
            var value = (Action<int, Rect>) CONTROLLER_GUI_CALLBACK_PROPERTY.GetValue(treeViewController);
            CONTROLLER_GUI_CALLBACK_PROPERTY.SetValue(treeViewController, value - action);
        }

        public static void AddRepaintCallback(object objectListArea, Action repaintCallback)
        {
            EnsureInit();
            if (OBJECT_LIST_REPAINT_CALLBACK == null) return;
            var value = (Action) OBJECT_LIST_REPAINT_CALLBACK.GetValue(objectListArea);
            OBJECT_LIST_REPAINT_CALLBACK.SetValue(objectListArea, value + repaintCallback);
        }

        [SuppressMessage("ReSharper", "DelegateSubtraction")]
        public static void RemoveRepaintCallback(object objectListArea, Action repaintCallback)
        {
            EnsureInit();
            if (OBJECT_LIST_REPAINT_CALLBACK == null) return;
            var value = (Action) OBJECT_LIST_REPAINT_CALLBACK.GetValue(objectListArea);
            OBJECT_LIST_REPAINT_CALLBACK.SetValue(objectListArea, value - repaintCallback);
        }

        public static void AddPostAssetIconDrawCallback(Type target, string method)
        {
            EnsureInit();
            if (OBJECT_LIST_ICON_EVENT == null) return;
            var tempDelegate = Delegate.CreateDelegate(OBJECT_LIST_ICON_EVENT.FieldType, target, method);
            var value = (Delegate) OBJECT_LIST_ICON_EVENT.GetValue(null);
            OBJECT_LIST_ICON_EVENT.SetValue(null, Delegate.Combine(tempDelegate, value));
        }

        // -------------------------------------------------------
        // Helpers
        // -------------------------------------------------------

        [SuppressMessage("ReSharper", "InvertIf")]
        private static IEnumerable<TreeViewItem> GetFirstColumnItems(EditorWindow window)
        {
            EnsureInit();

            var oneColumnTree = PROJECT_ASSET_TREE_FIELD?.GetValue(window);
            if (oneColumnTree != null)
            {                
                var treeViewData = CONTROLLER_DATA_PROPERTY?.GetValue(oneColumnTree, null);
                var treeViewItems = (IEnumerable<TreeViewItem>) ONE_COLUMN_ITEMS_METHOD?.Invoke(treeViewData, null);
                return treeViewItems;
            }

            var twoColumnTree = PROJECT_FOLDER_TREE_FIELD?.GetValue(window);
            if (twoColumnTree != null)
            {                
                var treeViewData = CONTROLLER_DATA_PROPERTY?.GetValue(twoColumnTree, null);
                var treeViewItems = (IEnumerable<TreeViewItem>) TWO_COLUMN_ITEMS_METHOD?.Invoke(treeViewData, null);
                return treeViewItems;
            }

            return null;
        }

        private static IEnumerable<object> GetSecondColumnItems(EditorWindow window, bool onlyInListMode = false)
        {
            EnsureInit();
            var assetsList = PROJECT_OBJECT_LIST_FIELD?.GetValue(window);
            if (assetsList == null) return null;
            
            var localAssets = PROJECT_LOCAL_ASSETS_FIELD?.GetValue(assetsList);                
            if (localAssets == null) return null;
            if (onlyInListMode && !InListMode(localAssets)) return null;
                
            var filteredHierarchy = LIST_FILTERED_HIERARCHY_FIELD?.GetValue(localAssets);
            var results = FILTERED_HIERARCHY_RESULTS_PROPERTY?.GetValue(filteredHierarchy, null) as IEnumerable<object>;
            return results;
        }

        private static void ApplyDefaultIconsToSecondColumn()
        {
            EnsureInit();
            foreach (var window in GetAllProjectWindows())
            {
                var listItems = GetSecondColumnItems(window);
                if (listItems == null) continue;

                foreach (var item in listItems) SetIconForListItem(item, null);
                window.Repaint();
            }
        }

        private static bool InListMode(object localAssets)
        {
            return (bool) (ASSETS_LIST_MODE_PROPERTY?.GetValue(localAssets, null) ?? false);
        }

        private static int GetInstanceIdFromListItem(object listItem)
        {
            return (int) (FILTER_RESULT_ID_FIELD?.GetValue(listItem) ?? 0);
        }

        private static void SetIconForListItem(object listItem, Texture2D icon)
        {
            FILTER_RESULT_ICON_PROPERTY?.SetValue(listItem, icon, null);
        }

        private static bool ListItemIsFolder(object listItem)
        {
            return (bool) (FILTER_RESULT_IS_FOLDER_FIELD?.GetValue(listItem) ?? false);
        }

        // -------------------------------------------------------
        // Nested
        // -------------------------------------------------------

        public enum ViewMode
        {
            OneColumn,
            TwoColumns,
        }
    }
}
