using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Unity Editor Window — Scene Manager
/// Lists all scenes in Build Settings with Open, Additive, and Remove controls.
/// Place this file anywhere inside an Editor folder in your project.
/// </summary>
public class SceneManagerWindow : EditorWindow
{
    // ─── Colours ──────────────────────────────────────────────────────────────
    private static readonly Color ColBackground      = new Color(0.13f, 0.14f, 0.16f);
    private static readonly Color ColHeader          = new Color(0.10f, 0.11f, 0.13f);
    private static readonly Color ColRowEven         = new Color(0.17f, 0.18f, 0.20f);
    private static readonly Color ColRowOdd          = new Color(0.19f, 0.20f, 0.23f);
    private static readonly Color ColRowHover        = new Color(0.23f, 0.30f, 0.40f);
    private static readonly Color ColRowActive       = new Color(0.18f, 0.42f, 0.62f);
    private static readonly Color ColAccentBlue      = new Color(0.20f, 0.55f, 0.95f);
    private static readonly Color ColAccentGreen     = new Color(0.22f, 0.75f, 0.50f);
    private static readonly Color ColAccentOrange    = new Color(0.95f, 0.55f, 0.18f);
    private static readonly Color ColAccentRed       = new Color(0.90f, 0.28f, 0.28f);
    private static readonly Color ColTextPrimary     = new Color(0.92f, 0.94f, 0.97f);
    private static readonly Color ColTextMuted       = new Color(0.55f, 0.60f, 0.68f);
    private static readonly Color ColTextLink        = new Color(0.40f, 0.75f, 1.00f);
    private static readonly Color ColTextLinkHover   = new Color(0.65f, 0.90f, 1.00f);
    private static readonly Color ColDivider         = new Color(0.25f, 0.27f, 0.32f);
    private static readonly Color ColBadgeEnabled    = new Color(0.18f, 0.55f, 0.30f);
    private static readonly Color ColBadgeDisabled   = new Color(0.38f, 0.20f, 0.18f);

    // ─── Styles ───────────────────────────────────────────────────────────────
    private GUIStyle _styleLinkLabel;
    private GUIStyle _styleHeaderLabel;
    private GUIStyle _styleSubLabel;
    private GUIStyle _styleBtnOpen;
    private GUIStyle _styleBtnAdditive;
    private GUIStyle _styleBtnClose;
    private GUIStyle _styleBadge;
    private GUIStyle _styleIndexLabel;
    private GUIStyle _styleToolbarBtn;
    private bool     _stylesBuilt;

    // ─── State ────────────────────────────────────────────────────────────────
    private Vector2 _scroll;
    private int     _hoveredRow  = -1;
    private double  _lastRepaint;
    private string  _searchQuery = "";
    private bool    _showDisabled = true;

    // ─── Textures ─────────────────────────────────────────────────────────────
    private Texture2D _texRowEven;
    private Texture2D _texRowOdd;
    private Texture2D _texRowHover;
    private Texture2D _texRowActive;
    private Texture2D _texHeader;
    private Texture2D _texBackground;
    private Texture2D _texBtnBlue;
    private Texture2D _texBtnGreen;
    private Texture2D _texBtnRed;
    private Texture2D _texBtnBlueHov;
    private Texture2D _texBtnGreenHov;
    private Texture2D _texBtnRedHov;
    private Texture2D _texBadgeEnabled;
    private Texture2D _texBadgeDisabled;
    private Texture2D _texDivider;

    // ─── Constants ────────────────────────────────────────────────────────────
    private const float RowHeight       = 36f;
    private const float HeaderHeight    = 52f;
    private const float ToolbarHeight   = 38f;
    private const float ColIndexW       = 36f;
    private const float ColBtnOpenW     = 58f;
    private const float ColBtnAdditiveW = 72f;
    private const float ColBtnCloseW    = 30f;
    private const float ColBadgeW       = 64f;
    private const float Padding         = 8f;
    private const float BtnH            = 22f;

    // ─────────────────────────────────────────────────────────────────────────
    [MenuItem("Tools/Scene Manager %&s")]
    public static void Open()
    {
        var win = GetWindow<SceneManagerWindow>(false, "Scene Manager");
        win.minSize = new Vector2(520, 300);
        win.Show();
    }

    // ─── Lifecycle ────────────────────────────────────────────────────────────
    private void OnEnable()
    {
        _stylesBuilt = false;
        EditorApplication.update += OnEditorUpdate;
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        DestroyTextures();
    }

    private void OnEditorUpdate()
    {
        // Repaint at ~30 fps for hover animations without hammering the CPU
        if (EditorApplication.timeSinceStartup - _lastRepaint > 0.033)
        {
            _lastRepaint = EditorApplication.timeSinceStartup;
            Repaint();
        }
    }

    // ─── Build styles & textures ─────────────────────────────────────────────
    private void BuildStyles()
    {
        if (_stylesBuilt) return;

        _texBackground    = MakeTex(ColBackground);
        _texHeader        = MakeTex(ColHeader);
        _texRowEven       = MakeTex(ColRowEven);
        _texRowOdd        = MakeTex(ColRowOdd);
        _texRowHover      = MakeTex(ColRowHover);
        _texRowActive     = MakeTex(ColRowActive);
        _texBtnBlue       = MakeTex(ColAccentBlue  * 0.85f);
        _texBtnGreen      = MakeTex(ColAccentGreen * 0.85f);
        _texBtnRed        = MakeTex(ColAccentRed   * 0.75f);
        _texBtnBlueHov    = MakeTex(ColAccentBlue);
        _texBtnGreenHov   = MakeTex(ColAccentGreen);
        _texBtnRedHov     = MakeTex(ColAccentRed);
        _texBadgeEnabled  = MakeTex(ColBadgeEnabled);
        _texBadgeDisabled = MakeTex(ColBadgeDisabled);
        _texDivider       = MakeTex(ColDivider);

        // Link label
        _styleLinkLabel = new GUIStyle(EditorStyles.label)
        {
            normal    = { textColor = ColTextLink },
            hover     = { textColor = ColTextLinkHover },
            focused   = { textColor = ColTextLinkHover },
            fontStyle = FontStyle.Normal,
            fontSize  = 12,
            wordWrap  = false,
            clipping  = TextClipping.Clip
        };

        // Header title
        _styleHeaderLabel = new GUIStyle(EditorStyles.boldLabel)
        {
            normal   = { textColor = ColTextPrimary },
            fontSize = 15,
            alignment = TextAnchor.MiddleLeft
        };

        // Sub / muted label
        _styleSubLabel = new GUIStyle(EditorStyles.miniLabel)
        {
            normal = { textColor = ColTextMuted },
            fontSize = 10
        };

        // Index badge
        _styleIndexLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            normal  = { textColor = ColTextMuted },
            fontSize = 10,
            alignment = TextAnchor.MiddleCenter
        };

        // Open button
        _styleBtnOpen = MakeButtonStyle(_texBtnBlue, _texBtnBlueHov, ColTextPrimary, 11);

        // Additive button
        _styleBtnAdditive = MakeButtonStyle(_texBtnGreen, _texBtnGreenHov, ColTextPrimary, 11);

        // Close / remove button
        _styleBtnClose = MakeButtonStyle(_texBtnRed, _texBtnRedHov, ColTextPrimary, 12);

        // Status badge
        _styleBadge = new GUIStyle(GUI.skin.box)
        {
            normal    = { background = _texBadgeEnabled, textColor = Color.white },
            hover     = { background = _texBadgeEnabled, textColor = Color.white },
            active    = { background = _texBadgeEnabled, textColor = Color.white },
            fontSize  = 9,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            border    = new RectOffset(3, 3, 3, 3),
            padding   = new RectOffset(4, 4, 2, 2),
            margin    = new RectOffset(0, 0, 0, 0)
        };

        // Toolbar button
        _styleToolbarBtn = new GUIStyle(EditorStyles.miniButton)
        {
            normal  = { textColor = ColTextMuted },
            hover   = { textColor = ColTextPrimary },
            active  = { textColor = ColTextPrimary },
            padding = new RectOffset(8, 8, 3, 3),
            fontSize = 11
        };

        _stylesBuilt = true;
    }

    private static GUIStyle MakeButtonStyle(Texture2D normal, Texture2D hover, Color textCol, int fontSize)
    {
        var s = new GUIStyle(GUI.skin.button)
        {
            normal  = { background = normal, textColor = textCol },
            hover   = { background = hover,  textColor = Color.white },
            active  = { background = hover,  textColor = Color.white },
            focused = { background = normal, textColor = textCol },
            border  = new RectOffset(4, 4, 4, 4),
            padding = new RectOffset(6, 6, 3, 3),
            fontSize = fontSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter
        };
        return s;
    }

    private void DestroyTextures()
    {
        Texture2D[] textures = {
            _texBackground, _texHeader, _texRowEven, _texRowOdd,
            _texRowHover, _texRowActive, _texBtnBlue, _texBtnGreen,
            _texBtnRed, _texBtnBlueHov, _texBtnGreenHov, _texBtnRedHov,
            _texBadgeEnabled, _texBadgeDisabled, _texDivider
        };
        foreach (var t in textures)
            if (t != null) DestroyImmediate(t);
    }

    private static Texture2D MakeTex(Color col)
    {
        var t = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        t.SetPixel(0, 0, col);
        t.Apply();
        t.hideFlags = HideFlags.DontSave;
        return t;
    }

    // ─── GUI ─────────────────────────────────────────────────────────────────
    private void OnGUI()
    {
        BuildStyles();

        // Overall background
        EditorGUI.DrawRect(new Rect(0, 0, position.width, position.height), ColBackground);

        float y = 0;

        // ── Header ──
        y = DrawHeader(y);

        // ── Toolbar ──
        y = DrawToolbar(y);

        // ── Column labels ──
        y = DrawColumnHeaders(y);

        // ── Scene rows ──
        DrawSceneRows(y);
    }

    private float DrawHeader(float y)
    {
        var headerRect = new Rect(0, y, position.width, HeaderHeight);
        EditorGUI.DrawRect(headerRect, ColHeader);

        // Icon + title
        var iconRect  = new Rect(Padding, y + 12, 28, 28);
        var titleRect = new Rect(iconRect.xMax + 8, y + 10, 200, 18);
        var subRect   = new Rect(iconRect.xMax + 8, y + 28, 300, 14);

        // Draw a simple scene icon via built-in Unity icon
        var sceneIcon = EditorGUIUtility.IconContent("SceneAsset Icon");
        if (sceneIcon != null && sceneIcon.image != null)
            GUI.DrawTexture(iconRect, sceneIcon.image, ScaleMode.ScaleToFit);

        GUI.Label(titleRect, "Scene Manager", _styleHeaderLabel);

        int total   = EditorBuildSettings.scenes.Length;
        int enabled = System.Array.FindAll(EditorBuildSettings.scenes, s => s.enabled).Length;
        GUI.Label(subRect, $"{enabled} enabled  ·  {total} total in Build Settings", _styleSubLabel);

        // Divider
        EditorGUI.DrawRect(new Rect(0, y + HeaderHeight - 1, position.width, 1), ColDivider);

        return y + HeaderHeight;
    }

    private float DrawToolbar(float y)
    {
        var toolbarRect = new Rect(0, y, position.width, ToolbarHeight);
        EditorGUI.DrawRect(toolbarRect, new Color(0.12f, 0.13f, 0.15f));

        float x = Padding;

        // Search field
        float searchW = Mathf.Max(160, position.width - 300);
        var searchRect = new Rect(x, y + 8, searchW, 22);
        _searchQuery = EditorGUI.TextField(searchRect, _searchQuery, EditorStyles.toolbarSearchField);
        x += searchW + 8;

        // Show Disabled toggle
        float toggleW = 100;
        var toggleRect = new Rect(x, y + 10, toggleW, 18);
        _showDisabled = GUI.Toggle(toggleRect, _showDisabled, " Show Disabled", EditorStyles.miniButton);
        x += toggleW + 8;

        // Refresh button
        float refreshW = 70;
        var refreshRect = new Rect(x, y + 9, refreshW, 20);
        if (GUI.Button(refreshRect, "↻  Refresh", _styleToolbarBtn))
            Repaint();
        x += refreshW + 8;

        // Open Build Settings button
        float bsW = 110;
        var bsRect = new Rect(position.width - bsW - Padding, y + 9, bsW, 20);
        if (GUI.Button(bsRect, "⚙  Build Settings", _styleToolbarBtn))
            EditorWindow.GetWindow(System.Type.GetType("UnityEditor.BuildPlayerWindow,UnityEditor"));

        EditorGUI.DrawRect(new Rect(0, y + ToolbarHeight - 1, position.width, 1), ColDivider);

        return y + ToolbarHeight;
    }

    private float DrawColumnHeaders(float y)
    {
        float colH = 24f;
        EditorGUI.DrawRect(new Rect(0, y, position.width, colH), ColHeader);

        float x = 0;

        // #
        EditorGUI.LabelField(new Rect(x, y + 4, ColIndexW, colH), "#",
            new GUIStyle(_styleSubLabel) { alignment = TextAnchor.MiddleCenter });
        x += ColIndexW;

        // Scene Name
        EditorGUI.LabelField(new Rect(x + Padding, y + 4, 120, colH), "SCENE NAME", _styleSubLabel);

        // Right-aligned columns
        float rightX = position.width - Padding;
        rightX -= ColBtnCloseW;
        EditorGUI.LabelField(new Rect(rightX, y + 4, ColBtnCloseW, colH), "REM",
            new GUIStyle(_styleSubLabel) { alignment = TextAnchor.MiddleCenter });
        rightX -= ColBtnAdditiveW + 4;
        EditorGUI.LabelField(new Rect(rightX, y + 4, ColBtnAdditiveW, colH), "ADDITIVE",
            new GUIStyle(_styleSubLabel) { alignment = TextAnchor.MiddleCenter });
        rightX -= ColBtnOpenW + 4;
        EditorGUI.LabelField(new Rect(rightX, y + 4, ColBtnOpenW, colH), "OPEN",
            new GUIStyle(_styleSubLabel) { alignment = TextAnchor.MiddleCenter });
        rightX -= ColBadgeW + 4;
        EditorGUI.LabelField(new Rect(rightX, y + 4, ColBadgeW, colH), "STATUS",
            new GUIStyle(_styleSubLabel) { alignment = TextAnchor.MiddleCenter });

        EditorGUI.DrawRect(new Rect(0, y + colH - 1, position.width, 1), ColDivider);

        return y + colH;
    }

    private void DrawSceneRows(float startY)
    {
        var scenes = EditorBuildSettings.scenes;

        // Filter
        var filtered = new List<(EditorBuildSettingsScene scene, int originalIndex)>();
        for (int i = 0; i < scenes.Length; i++)
        {
            var sc = scenes[i];
            if (!_showDisabled && !sc.enabled) continue;
            string name = Path.GetFileNameWithoutExtension(sc.path);
            if (!string.IsNullOrEmpty(_searchQuery) &&
                !name.ToLower().Contains(_searchQuery.ToLower()) &&
                !sc.path.ToLower().Contains(_searchQuery.ToLower()))
                continue;
            filtered.Add((sc, i));
        }

        float totalHeight = filtered.Count * RowHeight;
        var scrollViewRect = new Rect(0, startY, position.width, position.height - startY);
        var contentRect    = new Rect(0, 0, position.width - 14f, totalHeight);

        _scroll = GUI.BeginScrollView(scrollViewRect, _scroll, contentRect);

        Event e = Event.current;

        for (int listIdx = 0; listIdx < filtered.Count; listIdx++)
        {
            var (scene, origIdx) = filtered[listIdx];
            float rowY    = listIdx * RowHeight;
            var   rowRect = new Rect(0, rowY, position.width - 14f, RowHeight);

            bool isActive = false;
            try
            {
                string scenePath = scene.path;
                for (int s = 0; s < SceneManager.sceneCount; s++)
                {
                    if (SceneManager.GetSceneAt(s).path == scenePath)
                    { isActive = true; break; }
                }
            }
            catch { /* safe */ }

            bool isHovered = rowRect.Contains(e.mousePosition);
            if (isHovered) _hoveredRow = listIdx;
            else if (_hoveredRow == listIdx) _hoveredRow = -1;

            // Row background
            Texture2D rowTex = isActive   ? _texRowActive  :
                               isHovered  ? _texRowHover   :
                               listIdx % 2 == 0 ? _texRowEven : _texRowOdd;
            GUI.DrawTexture(rowRect, rowTex, ScaleMode.StretchToFill);

            // Left border accent for active scenes
            if (isActive)
                EditorGUI.DrawRect(new Rect(0, rowY, 3, RowHeight), ColAccentBlue);
            else if (!scene.enabled)
                EditorGUI.DrawRect(new Rect(0, rowY, 3, RowHeight), new Color(0.4f, 0.4f, 0.4f, 0.4f));

            DrawRow(scene, origIdx, listIdx, rowY, isActive, isHovered, scenes, e);

            // Divider
            EditorGUI.DrawRect(new Rect(ColIndexW, rowY + RowHeight - 1, rowRect.width - ColIndexW, 1),
                new Color(0.22f, 0.24f, 0.28f, 0.6f));
        }

        GUI.EndScrollView();

        // Empty state
        if (filtered.Count == 0)
        {
            var emptyRect = new Rect(0, startY, position.width, position.height - startY);
            GUI.Label(emptyRect,
                scenes.Length == 0
                    ? "No scenes in Build Settings.\nOpen Build Settings and add scenes."
                    : "No scenes match the current filter.",
                new GUIStyle(EditorStyles.centeredGreyMiniLabel)
                {
                    fontSize  = 12,
                    normal    = { textColor = ColTextMuted },
                    wordWrap  = true,
                    alignment = TextAnchor.MiddleCenter
                });
        }
    }

    private void DrawRow(
        EditorBuildSettingsScene scene,
        int origIdx,
        int listIdx,
        float rowY,
        bool isActive,
        bool isHovered,
        EditorBuildSettingsScene[] allScenes,
        Event e)
    {
        float x        = 0;
        float centerY  = rowY + (RowHeight - BtnH) * 0.5f;
        float nameCenterY = rowY + (RowHeight - 16) * 0.5f;

        // ── Index ──
        EditorGUI.LabelField(
            new Rect(x, rowY, ColIndexW, RowHeight),
            origIdx.ToString(),
            new GUIStyle(_styleIndexLabel) { normal = { textColor = isActive ? ColAccentBlue : ColTextMuted } }
        );
        x += ColIndexW;

        // ── Scene name hyperlink ──
        string sceneName = Path.GetFileNameWithoutExtension(scene.path);
        if (string.IsNullOrEmpty(sceneName)) sceneName = "(unknown)";

        float rightEdge    = position.width - 14f - Padding;
        float usedRight    = ColBtnCloseW + 4 + ColBtnAdditiveW + 4 + ColBtnOpenW + 4 + ColBadgeW + 4;
        float nameCellW    = rightEdge - usedRight - x - Padding;
        var   nameCellRect = new Rect(x + Padding, nameCenterY, nameCellW, 16);

        var linkStyle = new GUIStyle(_styleLinkLabel);
        if (!scene.enabled)
        {
            linkStyle.normal.textColor  = ColTextMuted;
            linkStyle.hover.textColor   = ColTextMuted * 1.2f;
            linkStyle.fontStyle         = FontStyle.Italic;
        }

        // Measure to know if path fits or we show tooltip
        GUIContent linkContent = new GUIContent(sceneName, scene.path);

        // Hover highlight glow on link
        if (isHovered && scene.enabled)
            linkStyle.normal.textColor = ColTextLinkHover;

        if (GUI.Button(nameCellRect, linkContent, linkStyle))
        {
            // Ping the scene asset in the Project window
            var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scene.path);
            if (asset != null)
            {
                EditorGUIUtility.PingObject(asset);
                Selection.activeObject = asset;
            }
        }

        // Path sub-label (only if row is hovered or active and there's room)
        if ((isHovered || isActive) && RowHeight >= 36f)
        {
            var pathRect = new Rect(x + Padding, rowY + 20, nameCellW, 12);
            EditorGUI.LabelField(pathRect,
                TruncatePath(scene.path, nameCellW),
                _styleSubLabel);
        }

        // ── Right-side controls ──
        float rx = rightEdge;

        // Remove (×) button
        rx -= ColBtnCloseW;
        var closeRect = new Rect(rx, centerY, ColBtnCloseW, BtnH);
        if (scene.enabled)
        {
            if (GUI.Button(closeRect, "✕", _styleBtnClose))
                RemoveScene(origIdx, allScenes);
        }
        else
        {
            // Re-enable button for disabled scenes
            var reAddStyle = new GUIStyle(_styleBtnClose)
            {
                normal  = { background = MakeTex(new Color(0.3f, 0.3f, 0.35f)), textColor = ColTextMuted },
                hover   = { background = _texBtnRedHov, textColor = Color.white },
                active  = { background = _texBtnRedHov, textColor = Color.white },
                focused = { background = MakeTex(new Color(0.3f, 0.3f, 0.35f)), textColor = ColTextMuted }
            };
            if (GUI.Button(closeRect, "✕", reAddStyle))
                RemoveScene(origIdx, allScenes);
        }
        rx -= 4;

        // Additive button
        rx -= ColBtnAdditiveW;
        var additiveRect = new Rect(rx, centerY, ColBtnAdditiveW, BtnH);
        EditorGUI.BeginDisabledGroup(!scene.enabled);
        if (GUI.Button(additiveRect, "+ Additive", _styleBtnAdditive))
            OpenScene(scene.path, OpenSceneMode.Additive);
        EditorGUI.EndDisabledGroup();
        rx -= 4;

        // Open button
        rx -= ColBtnOpenW;
        var openRect = new Rect(rx, centerY, ColBtnOpenW, BtnH);
        EditorGUI.BeginDisabledGroup(!scene.enabled);
        if (GUI.Button(openRect, "▶ Open", _styleBtnOpen))
            OpenScene(scene.path, OpenSceneMode.Single);
        EditorGUI.EndDisabledGroup();
        rx -= 4;

        // Status badge
        rx -= ColBadgeW;
        var badgeRect = new Rect(rx, centerY, ColBadgeW, BtnH);
        var badgeStyle = new GUIStyle(_styleBadge);
        if (scene.enabled)
        {
            badgeStyle.normal.background = _texBadgeEnabled;
            badgeStyle.normal.textColor  = new Color(0.6f, 1.0f, 0.7f);
            GUI.Box(badgeRect, "● ENABLED", badgeStyle);
        }
        else
        {
            badgeStyle.normal.background = _texBadgeDisabled;
            badgeStyle.normal.textColor  = new Color(1.0f, 0.55f, 0.55f);
            GUI.Box(badgeRect, "○ DISABLED", badgeStyle);
        }
    }

    // ─── Actions ─────────────────────────────────────────────────────────────
    private static void OpenScene(string path, OpenSceneMode mode)
    {
        if (string.IsNullOrEmpty(path)) return;

        if (mode == OpenSceneMode.Single && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        }
        else if (mode == OpenSceneMode.Additive)
        {
            EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
        }
    }

    private static void RemoveScene(int index, EditorBuildSettingsScene[] current)
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Remove Scene",
            $"Remove scene at index {index} from Build Settings?\n\nThis does NOT delete the file.",
            "Remove", "Cancel");

        if (!confirm) return;

        var newList = new List<EditorBuildSettingsScene>(current);
        newList.RemoveAt(index);
        EditorBuildSettings.scenes = newList.ToArray();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    private static string TruncatePath(string path, float availableWidth)
    {
        if (availableWidth < 80) return "";

        // Rough char-width estimate at 9px per char for miniLabel
        int maxChars = Mathf.Max(10, (int)(availableWidth / 6.5f));
        if (path.Length <= maxChars) return path;
        return "…" + path.Substring(path.Length - maxChars + 1);
    }
}
