/**
 * Auth :   liubo
 * Date :   2022-12-01 20:35:16
 * Comment: 测试下unity的Graph，完全手绘的方式。下次用UIToolkit。
 *
 * 参考
 * https://github.com/halak/unity-editor-icons
 * https://github.com/arimger/Unity-Editor-Toolbox
 * https://github.com/Unity-Technologies/com.unity.search.extensions
 *
 * 
 */


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GraphWindow : EditorWindow
{
    [MenuItem("Tools/TestGraph")]
    static void ShowGraphWindow()
    {
        GetWindow<GraphWindow>();
    }
    
    static class Colors
    {
        static readonly Color k_NodeHeaderDark = new Color(38f / 255f, 38f / 255f, 38f / 255f);
        static readonly Color k_NodeDescription = new Color(79 / 255f, 79 / 255f, 79 / 255f);

        public static Color nodeHeader => EditorGUIUtility.isProSkin ? k_NodeHeaderDark : Color.blue;
        public static Color nodeDescription => EditorGUIUtility.isProSkin ? k_NodeDescription : Color.white;
    }
    
    const float kInitialPosOffset = -0;
    const float kNodeWidth = 180.0f;
    const float kNodeHeight = 100.0f;
    const float kNodeHeaderRatio = 0.4f;
    const float kNodeHeaderHeight = kNodeHeight * kNodeHeaderRatio;
    const float kHalfNodeWidth = kNodeWidth / 2.0f;
    const float kBaseZoomMinLevel = 0.2f;
    const float kBaseZoomMaxLevel = 3.25f;
    const float kMenuBarHeight = 24f;
    const float kStatusBarHeight = 20f;
    const float kNodeMargin = 10.0f;
    const float kBorderRadius = 2.0f;
    const float kBorderWidth = 0f;
    const float kExpandButtonHeight = 20f;
    const float kElbowCornerRadius = 10f;
    private const float kConnectPointSize = 8;
    
    static readonly Color kWeakInColor = new Color(240f / 255f, 240f / 255f, 240f / 255f);
    static readonly Color kWeakOutColor = new Color(120 / 255f, 134f / 255f, 150f / 255f);
    static readonly Color kDirectInColor = new Color(146 / 255f, 196 / 255f, 109 / 255f);
    static readonly Color kDirectOutColor = new Color(83 / 255f, 150 / 255f, 153 / 255f);
    
    GUIStyle m_MenuBarStyle;
    GUIStyle m_StatusBarStyle;
    GUIStyle m_NodeDescriptionStyle;
    GUIStyleState m_NodeDescriptionStyleState;
    private GUIStyle BoxBorderStyle;
    
    float zoom = 1.0f;
    bool showStatus = true;
    string status = "status:xxxx";
    Vector2 pan = new Vector2(kInitialPosOffset, kInitialPosOffset);

    private Graph graphData;
    private Node selectedNode;
    
    // menu的位置
    Rect menuBarRect => new Rect(0, 0, rootVisualElement.worldBound.width, kMenuBarHeight);

    // status的位置
    Rect statusBarRect => new Rect(0, graphRect.yMax, rootVisualElement.worldBound.width, (showStatus ? kStatusBarHeight : 0f));
    
    // rootVisualElement.worldBound是窗口大小
    // graph的视图大小
    Rect graphRect => new Rect(0, menuBarRect.yMax, rootVisualElement.worldBound.width,
        rootVisualElement.worldBound.height - menuBarRect.height - (showStatus ? kStatusBarHeight : 0f));
    
    private void OnEnable()
    {
        titleContent = new GUIContent("Test Graph", EditorGUIUtility.FindTexture("Search Icon"));

        BoxBorderStyle = new GUIStyle();
        BoxBorderStyle.alignment = TextAnchor.MiddleLeft;
        
        m_MenuBarStyle  = new GUIStyle()
        {
            name = "quick-search-status-bar-background",
            // fixedHeight = kStatusBarHeight
        };
        
        m_StatusBarStyle = new GUIStyle()
        {
            name = "quick-search-status-bar-background",
            fixedHeight = kStatusBarHeight
        };

        m_NodeDescriptionStyleState = new GUIStyleState()
        {
            background = null,
            scaledBackgrounds = new Texture2D[] { null },
            textColor = Colors.nodeDescription
        };
        m_NodeDescriptionStyle = new GUIStyle()
        {
            name = "Label",
            fontSize = 8,
            normal = m_NodeDescriptionStyleState,
            hover = m_NodeDescriptionStyleState,
            active = m_NodeDescriptionStyleState,
            focused = m_NodeDescriptionStyleState,
            padding = new RectOffset(3, 2, 1, 1)
        };
    }

    private void OnGUI()
    {
        var evt = Event.current;

        DrawGrid();

        DrawMenu(evt);
        
        DrawGraph(evt);
        DrawStatusBar();
        
        // 叠一层Panel
        DrawPanelOpts(evt);
        
        // 右键菜单
        DrawRightMenu(evt);
        
        // 处理按键事件
        HandleEvent(evt);
    }

    /// <summary>
    /// 在Graph上层的一个Panel，放置一些功能按钮
    /// </summary>
    /// <param name="evt"></param>
    void DrawPanelOpts(Event evt)
    {
        var panelRect = new Rect(10, menuBarRect.yMax + 10, 200, 200);
        
        // 使用区域，光靠GUILayout.Vertical是无法指定位置的
        GUILayout.BeginArea(panelRect);
        using (new GUILayout.VerticalScope(m_MenuBarStyle, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true)))
        {
            GUILayout.Label("option1");
            GUILayout.Button("option2");
            GUILayout.TextField("option3");
        }
        GUILayout.EndArea();
    }

    void DrawGraph(Event evt)
    {
        var worldBoundRect = this.rootVisualElement.worldBound;
        EditorZoomArea.Begin(zoom, graphRect, worldBoundRect);
        DrawGraphView(evt);
        EditorZoomArea.End();
    }
    
    void DrawGraphView(Event evt)
    {
        if (graphData == null)
        {
            graphData = new Graph();
            graphData.MakeTest();
        }
        
        var viewportRect = new Rect(-pan.x, -pan.y, graphRect.width, graphRect.height).ScaleSizeBy(1f / zoom, -pan);
        if (evt.type == EventType.Repaint)
        {
            Handles.BeginGUI();
            
            // 绘制连线
            foreach (var edge in graphData.edges)
                DrawEdge(viewportRect, edge, GetNodeDependencyAnchorPoint(edge.Source) + pan, GetNodeReferenceAnchorPoint(edge.Target) + pan);
            
            Handles.EndGUI();
        }

        // 绘制Node
        BeginWindows();
        foreach (var it in graphData.NodeList)
        {
            DrawNode(evt, viewportRect, it);
        }
        EndWindows();
        
    }

    Vector2 GetNodeDependencyAnchorPoint(Node node)
    {
        var nodeRect = node.rect.PadBy(kBorderWidth);
        return new Vector2(nodeRect.xMax - kNodeMargin, nodeRect.yMax - kNodeMargin - kExpandButtonHeight / 2f);
    }

    Vector2 GetNodeReferenceAnchorPoint(Node node)
    {
        var nodeRect = node.rect.PadBy(kBorderWidth);
        return new Vector2(nodeRect.xMin + kNodeMargin, nodeRect.yMax - kNodeMargin - kExpandButtonHeight / 2f);
    }

    /// <summary>
    /// 绘制节点之间的连线
    /// </summary>
    /// <param name="viewportRect"></param>
    /// <param name="edge"></param>
    /// <param name="from"></param>
    /// <param name="to"></param>
    void DrawEdge(in Rect viewportRect, in Edge edge, in Vector2 from, in Vector2 to)
    {
        if (edge.hidden)
            return;
        
        var edgeScale = to - from;
        var edgeBounds = new Rect(
            Mathf.Min(from.x, to.x) - pan.x, Mathf.Min(from.y, to.y) - pan.y,
            Mathf.Abs(edgeScale.x), Mathf.Abs(edgeScale.y));
        
        if (!edgeBounds.Overlaps(viewportRect))
            return;
        
        var edgeColor = GetLinkColor(edge.linkType);
        bool selected = selectedNode == edge.Source || selectedNode == edge.Target;
        if (selected)
        {
            const float kHightlightFactor = 1.65f;
            edgeColor.r = Math.Min(edgeColor.r * kHightlightFactor, 1.0f);
            edgeColor.g = Math.Min(edgeColor.g * kHightlightFactor, 1.0f);
            edgeColor.b = Math.Min(edgeColor.b * kHightlightFactor, 1.0f);
        }
        
        Handles.DrawBezier(from, to,
            new Vector2(from.x + kHalfNodeWidth, from.y),
            new Vector2(to.x - kHalfNodeWidth, to.y),
            edgeColor, null, 5f);
        
    }
    Color GetLinkColor(in LinkType linkType)
    {
        switch (linkType)
        {
            case LinkType.Self:
                return Color.red;
            case LinkType.WeakIn:
                return kWeakInColor;
            case LinkType.WeakOut:
                return kWeakOutColor;
            case LinkType.DirectIn:
                return kDirectInColor;
            case LinkType.DirectOut:
                return kDirectOutColor;
        }

        return Color.red;
    }

    /// <summary>
    /// 绘制节点
    /// </summary>
    /// <param name="evt"></param>
    /// <param name="viewportRect"></param>
    /// <param name="node"></param>
    void DrawNode(Event evt, in Rect viewportRect, Node node)
    {
        var windowRect = new Rect(node.rect.position + pan, node.rect.size);
        if (!node.rect.Overlaps(viewportRect))
            return;

        var old = node.rect;
        
        node.rect = GUI.Window(node.id, windowRect, _ => DrawNodeWindow(windowRect, evt, node), string.Empty);
        
        node.rect.x -= pan.x;
        node.rect.y -= pan.y;
        
        if (node.pinned)
        {
            node.rect = old;
        }
    }

    void DrawNodeWindow(in Rect windowRect, Event evt, in Node node)
    {
        var nodeRect = new Rect(0, 0, windowRect.width, windowRect.height).PadBy(kBorderWidth);

        // Header
        var headerRect = new Rect(nodeRect.x, nodeRect.y, nodeRect.width, kNodeHeaderHeight);
        var borderRadius2 = new Vector4(kBorderRadius, kBorderRadius, 0, 0);
        GUI.DrawTexture(headerRect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, false, 0f, Colors.nodeHeader, Vector4.zero, borderRadius2);

        var searchArea = new Rect(headerRect.xMax - 24, headerRect.yMax - 24, 24, 24);
        if (GUI.Button(searchArea, EditorGUIUtility.FindTexture("Search Icon")))
        {
            
        }
        
        // 预览小图标
        var hasPreview = node.preview != null;
        var previewSize = kNodeHeaderHeight - 2 * kNodeMargin;
        if (evt.type == EventType.Repaint && hasPreview)
        {
            GUI.DrawTexture(new Rect(
                headerRect.x + kNodeMargin, headerRect.y + kNodeMargin,
                previewSize, previewSize), node.preview);
        }

        // 标题
        var titleHeight = 20f;
        var titleOffsetX = kNodeMargin;
        var titleWidth = headerRect.width - 2 * kNodeMargin;
        if (hasPreview)
        {
            titleOffsetX += previewSize + kNodeMargin;
            titleWidth = titleWidth - previewSize - kNodeMargin;
        }
        var nodeTitleRect = new Rect(titleOffsetX, kNodeMargin, titleWidth, titleHeight);
        GUI.Label(nodeTitleRect, node.title);

        // 描述文字
        var descriptionHeight = 16f;
        var descriptionOffsetX = kNodeMargin;
        var descriptionWidth = headerRect.width - 2 * kNodeMargin;
        if (hasPreview)
        {
            descriptionOffsetX += previewSize + kNodeMargin;
            descriptionWidth = titleWidth - previewSize - kNodeMargin;
        }
        var nodeDescriptionRect = new Rect(descriptionOffsetX, nodeTitleRect.yMax, descriptionWidth, descriptionHeight);
        GUI.Label(nodeDescriptionRect, "In Project", m_NodeDescriptionStyle);

        var objRect = new Rect(nodeRect.x+1, headerRect.yMax, nodeRect.width-3, 20);
        // 对象
        
        node.Obj = EditorGUI.ObjectField(objRect, node.Obj, typeof(UnityEngine.Object));
        
        // 对象的preview
        
        if (node.Obj != null)
        {
            var rectPreview = new Rect(nodeRect.x, headerRect.yMax+20, 100, 80);
            EditorGUI.DrawPreviewTexture(rectPreview, AssetPreview.GetAssetPreview(node.Obj) ?? AssetPreview.GetMiniThumbnail(node.Obj));
        }
        
        // Expand Dependencies
        var buttonStyle = GUI.skin.button;
        var expandDependenciesContent = new GUIContent($"{node.dependencyCount}");
        var buttonContentSize = buttonStyle.CalcSize(expandDependenciesContent);
        var buttonRect = new Rect(nodeRect.width - kNodeMargin - buttonContentSize.x, nodeRect.height - kNodeMargin - kExpandButtonHeight, buttonContentSize.x, kExpandButtonHeight);
        if (GUI.Button(buttonRect, expandDependenciesContent))
        {
            // TODO
        }
        

        // Expand References
        var expandReferencesContent = new GUIContent($"{node.referenceCount}");
        buttonContentSize = buttonStyle.CalcSize(expandReferencesContent);
        buttonRect = new Rect(nodeRect.x + kNodeMargin, nodeRect.height - kNodeMargin - kExpandButtonHeight, buttonContentSize.x, kExpandButtonHeight);
#if false        
        if (GUI.Button(buttonRect, expandReferencesContent))
        {
            // todo
        }
#else
        DrawLabelInBox(expandReferencesContent, buttonRect);
#endif

        // 绘制连接点
        var pointPng = EditorGUIUtility.FindTexture("Record Off");
        if (pointPng)
        {
            var point1Rect = new Rect(nodeRect.x, (buttonRect.yMin + buttonRect.yMax)/2-kConnectPointSize/2, kConnectPointSize, kConnectPointSize);
            GUI.DrawTexture(point1Rect, pointPng);
            var point2Rect = new Rect(nodeRect.xMax-kConnectPointSize, (buttonRect.yMin + buttonRect.yMax)/2-kConnectPointSize/2, kConnectPointSize, kConnectPointSize);
            GUI.DrawTexture(point2Rect, pointPng);
        }

        // 锁定位置
        buttonRect = new Rect(nodeRect.width - kBorderWidth * 2 - 16, kBorderWidth * 2, 16, 16);
        node.pinned = EditorGUI.Toggle(buttonRect, node.pinned, "IN LockButton");
        
        
        // 处理窗口事件
        if (evt.type == EventType.MouseDown && nodeRect.Contains(evt.mousePosition))
        {
            if (evt.button == 1) // 右键菜单
            {
                var menu = new GenericMenu();
            
                menu.AddItem(new GUIContent("node btn1"), false, (userdata) => { Debug.Log($"btn1:{((Node)userdata).id}");}, node);
                menu.AddItem(new GUIContent("node btn2"), false, (userdata) => { Debug.Log($"btn2:{((Node)userdata).id}");}, node);
            
                menu.ShowAsContext();
                evt.Use();
            }
            else if (true)
            {
                
            }
            else if (evt.button == 0) // 左键选中
            {
                var selectedObject = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(node.name);
                if (evt.clickCount == 1)
                {
                    selectedNode = node;
                    if (selectedObject)
                        EditorGUIUtility.PingObject(selectedObject.GetInstanceID());
                }
                else if (evt.clickCount == 2)
                {
                    Selection.activeObject = selectedObject;
                    evt.Use();
                }
            }
            else if (evt.button == 2)
            {
                node.pinned = !node.pinned;
                evt.Use();
            }
        }

        // 允许拖拽窗口
        GUI.DragWindow();
    }

    /// <summary>
    /// 绘制 边框包裹文字 的效果
    /// </summary>
    /// <param name="txt"></param>
    /// <param name="area"></param>
    void DrawLabelInBox(GUIContent txt, in Rect area)
    {
        var centeredStyle = GUI.skin.label;
        var old = centeredStyle.alignment; 
        centeredStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Box(area, "", EditorStyles.helpBox);
        GUI.Label(area, txt);
        
        centeredStyle.alignment = old;
    }

    /// <summary>
    /// 绘制棋盘格
    /// </summary>
    void DrawGrid()
    {
        DrawGrid(20, 0.2f, Color.gray);
        DrawGrid(100, 0.4f, Color.gray);
    }

    private void DrawGrid(float gridSpacing, float gridOpacity, Color gridColor)
    {
        int widthDivs = Mathf.CeilToInt(position.width / gridSpacing);
        int heightDivs = Mathf.CeilToInt(position.height / gridSpacing);

        Handles.BeginGUI();
        Handles.color = new Color(gridColor.r, gridColor.g, gridColor.b, gridOpacity);
        
        Vector3 newOffset = new Vector3(pan.x % gridSpacing, pan.y % gridSpacing, 0);

        for (int i = 0; i < widthDivs; i++)
        {
            Handles.DrawLine(new Vector3(gridSpacing * i, -gridSpacing, 0) + newOffset,
                new Vector3(gridSpacing * i, position.height, 0f) + newOffset);
        }

        for (int j = 0; j < heightDivs; j++)
        {
            Handles.DrawLine(new Vector3(-gridSpacing, gridSpacing * j, 0) + newOffset,
                new Vector3(position.width, gridSpacing * j, 0f) + newOffset);
        }

        Handles.color = Color.white;
        Handles.EndGUI();
    }
    
    /// <summary>
    /// 菜单栏
    /// </summary>
    /// <param name="evt"></param>
    void DrawMenu(Event evt)
    {
        // GUI.Box(menuBarRect, GUIContent.none, m_MenuBarStyle);
        using (new GUILayout.HorizontalScope(m_MenuBarStyle))
        {
            EditorGUI.BeginDisabledGroup(false);
            if (GUILayout.Button("<", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                Debug.Log("click btn: <");
            }
            EditorGUI.EndDisabledGroup();
            
            EditorGUI.BeginDisabledGroup(true);
            if (GUILayout.Button(">", EditorStyles.miniButton, GUILayout.MaxWidth(20)))
            {
                Debug.Log("click btn: >");
            }
            EditorGUI.EndDisabledGroup();
            
            // 空白区域
            GUILayout.FlexibleSpace();

            
            if (GUILayout.Button("ResetCamera", EditorStyles.miniButton))
            {
                zoom = 1;
                
                // 保证第一个节点，在中间
                if (graphData != null && graphData.NodeList.Count > 0)
                {
                    var node = selectedNode ?? graphData.NodeList[0];
                    pan.x = node.rect.x + rootVisualElement.worldBound.width/2 - node.rect.width/2;
                    pan.y = node.rect.y + rootVisualElement.worldBound.height/2 - node.rect.height/2;
                }
                else
                {
                    pan.x = rootVisualElement.worldBound.width/2;
                    pan.y = rootVisualElement.worldBound.height/2;
                }
            }

            if (EditorGUILayout.DropdownButton(EditorGUIUtility.TrTempContent("Columns"), FocusType.Passive))
            {
                var menu = new GenericMenu();
                
                menu.AddItem(new GUIContent("menu1"), false, () => { Debug.Log("menu1");});
                menu.AddItem(new GUIContent("menu2"), false, () => { Debug.Log("menu2");});

                menu.ShowAsContext();   
            }
        }
    }
        
    /// <summary>
    /// 状态栏
    /// </summary>
    void DrawStatusBar()
    {
        GUI.Box(statusBarRect, GUIContent.none, m_StatusBarStyle);

        status = $"pan=({pan.x},{pan.y}), zoom={zoom}, bound=({rootVisualElement.worldBound.width}, {rootVisualElement.worldBound.height})";
        
        if (!string.IsNullOrEmpty(status))
            GUI.Label(new Rect(4, statusBarRect.yMin, statusBarRect.width, statusBarRect.height), status);

        var graphInfoText = "graph status:yyy";
        var labelStyle = GUI.skin.label;
        var textSize = labelStyle.CalcSize(new GUIContent(graphInfoText));
        GUI.Label(new Rect(statusBarRect.xMax - textSize.x, statusBarRect.yMin, textSize.x, statusBarRect.height), graphInfoText);
    }

    /// <summary>
    /// 右键菜单
    /// </summary>
    /// <param name="evt"></param>
    void DrawRightMenu(Event evt)
    {
        if (evt.type == EventType.MouseDown && evt.button == 1)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("btn1"), false, () => { Debug.Log("btn1");});
            menu.AddItem(new GUIContent("btn2"), false, () => { Debug.Log("btn2");});
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("btns/btn3"), false, () => { Debug.Log("btn3");});
            menu.AddItem(new GUIContent("btns/btn4"), false, () => { Debug.Log("btn4");});
            
            menu.ShowAsContext();
            evt.Use();
        }
    }

    /// <summary>
    /// 处理缩放，平移
    /// </summary>
    /// <param name="e"></param>
    void HandleEvent(Event e)
    {
        if (e.type == EventType.MouseDrag && graphRect.Contains(e.mousePosition))
        {
            pan.x += e.delta.x / zoom;
            pan.y += e.delta.y / zoom;
            e.Use();
        }
        else if (e.type == EventType.ScrollWheel && graphRect.Contains(e.mousePosition))
        {
            var zoomDelta = 0.1f;
            float delta = e.delta.x + e.delta.y;
            zoomDelta = delta < 0 ? zoomDelta : -zoomDelta;

            // To make the zoom focus on the target point, you have to make sure that this
            // point stays the same (in local space) after the transformations.
            // To do this, you can solve a little linear algebra system.
            // Let:
            // - TPLi be the initial target point (local space)
            // - TPLf be the final target point (local space)
            // - TPV be the target point (view space/global space)
            // - P1 the pan before the transformation
            // - P2 the pan after the transformation
            // - Z1 the zoom level before the transformation
            // - Z2 the zoom level after the transformation
            // Solve this system:
            // Eq1: TPV = TPLi/Z1 - P1
            // Eq2: Z2 = Z1 + delta
            // Eq3: TPLf = (TPV + P2) * Z2
            // We know that at the end, TPLf == TPLi, delta is a constant that we know,
            // so we only need to find P2. By substituting Eq1 and Eq2 into Eq3, we get
            // TPLf = (TPLi/Z1 - P1 + P2) * Z2
            // 0 = TPLi*delta/Z1 - P1*Z2 + P2*Z2
            // P2 = P1 - TPLi*delta/(Z1*Z2)
            float oldZoom = zoom;
            var targetLocal = e.mousePosition;
            SetZoom(zoom + zoomDelta);
            var realDelta = zoom - oldZoom;
            pan -= (targetLocal * realDelta / (oldZoom * zoom));

            e.Use();
        }
    }
    void SetZoom(float targetZoom)
    {
        zoom = Mathf.Clamp(targetZoom, kBaseZoomMinLevel, kBaseZoomMaxLevel);
    }
}


class EditorZoomArea
{
    private static Matrix4x4 _prevGuiMatrix;
    private static Rect s_WorldBoundRect;

    public static Rect Begin(in float zoomScale, in Rect screenCoordsArea, in Rect worldBoundRect)
    {
        s_WorldBoundRect = worldBoundRect;

        // End the group Unity begins automatically for an EditorWindow to clip out the window tab.
        // This allows us to draw outside of the size of the EditorWindow.
        GUI.EndGroup();

        Rect clippedArea = screenCoordsArea.ScaleSizeBy(1.0f / zoomScale, screenCoordsArea.TopLeft());
        clippedArea.x += worldBoundRect.x;
        clippedArea.y += worldBoundRect.y;
        GUI.BeginGroup(clippedArea);

        _prevGuiMatrix = GUI.matrix;
        Matrix4x4 translation = Matrix4x4.TRS(clippedArea.TopLeft(), Quaternion.identity, Vector3.one);
        Matrix4x4 scale = Matrix4x4.Scale(new Vector3(zoomScale, zoomScale, 1.0f));
        
        // 缩放
        GUI.matrix = translation * scale * translation.inverse * GUI.matrix;

        return clippedArea;
    }

    public static void End()
    {
        GUI.matrix = _prevGuiMatrix;
        GUI.EndGroup();
        GUI.BeginGroup(s_WorldBoundRect);
    }
}

// Helper Rect extension methods
static class RectExtensions
{
    public static Vector2 TopLeft(this Rect rect)
    {
        return new Vector2(rect.xMin, rect.yMin);
    }

    public static Rect ScaleSizeBy(this Rect rect, float scale)
    {
        return rect.ScaleSizeBy(scale, rect.center);
    }

    public static Rect ScaleSizeBy(this Rect rect, float scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;
        result.xMin *= scale;
        result.xMax *= scale;
        result.yMin *= scale;
        result.yMax *= scale;
        result.x += pivotPoint.x;
        result.y += pivotPoint.y;
        return result;
    }

    public static Rect ScaleSizeBy(this Rect rect, Vector2 scale)
    {
        return rect.ScaleSizeBy(scale, rect.center);
    }

    public static Rect ScaleSizeBy(this Rect rect, Vector2 scale, Vector2 pivotPoint)
    {
        Rect result = rect;
        result.x -= pivotPoint.x;
        result.y -= pivotPoint.y;
        result.xMin *= scale.x;
        result.xMax *= scale.x;
        result.yMin *= scale.y;
        result.yMax *= scale.y;
        result.x += pivotPoint.x;
        result.y += pivotPoint.y;
        return result;
    }

    public static Rect OffsetBy(this Rect rect, Vector2 offset)
    {
        return new Rect(rect.position + offset, rect.size);
    }

    public static Rect PadBy(this Rect rect, float padding)
    {
        return rect.PadBy(new Vector4(padding, padding, padding, padding));
    }

    public static Rect PadBy(this Rect rect, Vector4 padding)
    {
        return new Rect(rect.x + padding.x, rect.y + padding.y, rect.width - padding.x - padding.z,
            rect.height - padding.y - padding.w);
    }

    public static bool HorizontalOverlaps(this Rect rect, Rect other)
    {
        return other.xMax > rect.xMin && other.xMin < rect.xMax;
    }

    public static bool VerticalOverlaps(this Rect rect, Rect other)
    {
        return other.yMax > rect.yMin && other.yMin < rect.yMax;
    }
}

enum LinkType : uint
{
    Self,
    WeakIn,
    WeakOut,
    DirectIn,
    DirectOut
}
class Node
{
    // Common data
    public int id;
    public int index;
    public string name;
    public string typeName;

    int m_DependencyCount = -1;
    public int dependencyCount
    {
        get
        {
            return 0;
        }
    }

    int m_ReferenceCount = -1;
    public int referenceCount
    {
        get
        {
            return 0;
        }
    }

    // UI data
    public Rect rect;
    public bool previewFetched = false;
    public Texture cachedPreview = null;
    public Texture preview
    {
        get
        {
            if (!previewFetched)
            {
                cachedPreview = null;
                previewFetched = true;
            }
            if (cachedPreview)
                return cachedPreview;
            cachedPreview = null;
            return AssetDatabase.GetCachedIcon(name);
        }
    }
    public LinkType linkType;
    public string title;
    public string tooltip;

    // Layouting data
    public bool pinned;
    public bool expandedDependencies;
    public bool expandedReferences;
    public bool expanded => expandedDependencies && expandedReferences;
    public float mass = 1.0f;

    public UnityEngine.Object Obj;
    
    public void SetPosition(float x, float y)
    {
        rect.x = x;
        rect.y = y;
    }
}

class Edge
{
    public Edge(string id, Node source, Node target, LinkType linkType, float length = 1.0f)
    {
        ID = id;
        Source = source;
        Target = target;
        Directed = false;
        this.linkType = linkType;
        this.length = length;
    }

    public readonly string ID;
    public bool hidden = false;
    public LinkType linkType;
    public float length;
    public Node Source;
    public Node Target;
    public bool Directed;
}

class Graph
{
    private List<Node> nodeList = new();
    public List<Node> NodeList => nodeList;

    public List<Edge> edges = new();

    public void MakeTest()
    {
        nodeList = new List<Node>();
        
        var node1 = new Node();
        node1.id = 1;
        node1.name = "node1";
        node1.typeName = "node1-Type";
        node1.title = node1.name;
        node1.rect = new Rect(0, 0, 128, 158);
        nodeList.Add(node1);
        
        var node2 = new Node();
        node2.id = 2;
        node2.name = "node2";
        node2.typeName = "node2-Type";
        node2.title = node2.name;
        node2.rect = new Rect(200, 200, 128, 158);
        nodeList.Add(node2);

        edges = new();
        
        var edge1 = new Edge("3", node1, node2, LinkType.DirectOut);
        edges.Add(edge1);
    }
}