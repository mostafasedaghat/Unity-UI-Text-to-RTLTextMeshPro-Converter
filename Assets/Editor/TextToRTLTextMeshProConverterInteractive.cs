using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using RTLTMPro; // استفاده از RTL TextMeshPro
using TMPro;
using System.Collections.Generic;

public class TextToRTLTextMeshProConverter : EditorWindow
{
    private List<Text> foundTexts = new List<Text>();
    private List<bool> selectedTexts = new List<bool>();
    private Vector2 scrollPos;
    private TMP_FontAsset selectedFont;
    private bool removeOldComponents = true;
    private bool enableAutoSize = true;
    private bool fullAnchorStretch = true;

    [MenuItem("Tools/Convert UI.Text to RTLTextMeshPro")]
    static void Init()
    {
        TextToRTLTextMeshProConverter window = (TextToRTLTextMeshProConverter)EditorWindow.GetWindow(typeof(TextToRTLTextMeshProConverter));
        window.titleContent = new GUIContent("Text → RTL TMP");
        window.Show();
        window.FindAllTexts();
    }

    void FindAllTexts()
    {
        foundTexts.Clear();
        selectedTexts.Clear();
        Text[] texts = GameObject.FindObjectsOfType<Text>(true);

        foreach (var text in texts)
        {
            if (text.GetComponent<RTLTextMeshPro>() == null)
            {
                foundTexts.Add(text);
                selectedTexts.Add(true);
            }
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Select a Font Asset:", EditorStyles.boldLabel);
        selectedFont = (TMP_FontAsset)EditorGUILayout.ObjectField(selectedFont, typeof(TMP_FontAsset), false);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Select All"))
        {
            for (int i = 0; i < selectedTexts.Count; i++) selectedTexts[i] = true;
        }
        if (GUILayout.Button("Deselect All"))
        {
            for (int i = 0; i < selectedTexts.Count; i++) selectedTexts[i] = false;
        }
        if (GUILayout.Button("Refresh List"))
        {
            FindAllTexts();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Texts Found:", EditorStyles.boldLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        for (int i = 0; i < foundTexts.Count; i++)
        {
            if (foundTexts[i] == null) continue;

            EditorGUILayout.BeginHorizontal();
            selectedTexts[i] = EditorGUILayout.Toggle(selectedTexts[i], GUILayout.Width(20));
            if (GUILayout.Button(foundTexts[i].gameObject.name, GUILayout.Width(200)))
            {
                Selection.activeGameObject = foundTexts[i].gameObject;
            }
            EditorGUILayout.LabelField(foundTexts[i].text);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        removeOldComponents = EditorGUILayout.Toggle("Remove Old Text Components", removeOldComponents);
        enableAutoSize = EditorGUILayout.Toggle("Enable Auto Size Font", enableAutoSize);
        fullAnchorStretch = EditorGUILayout.Toggle("Anchor Full Stretch (Presets)", fullAnchorStretch);

        EditorGUILayout.Space();
        if (GUILayout.Button("Convert Selected Texts"))
        {
            ConvertSelected();
        }
    }

    void ConvertSelected()
    {
        for (int i = 0; i < foundTexts.Count; i++)
        {
            if (!selectedTexts[i] || foundTexts[i] == null) continue;

            Text oldText = foundTexts[i];
            GameObject go = oldText.gameObject;
            RectTransform rectTransform = go.GetComponent<RectTransform>();

            // Save properties
            string originalText = oldText.text;
            Color color = oldText.color;
            int fontSize = (int)oldText.fontSize;
            TextAnchor alignment = oldText.alignment;
            bool raycastTarget = oldText.raycastTarget;

            Outline outline = go.GetComponent<Outline>();
            Shadow shadow = go.GetComponent<Shadow>();

            // Create new GameObject with prefixed name
            string newName = "Rtl_" + go.name;
            GameObject rtlTextGO = new GameObject(newName, typeof(RTLTextMeshPro));
            rtlTextGO.transform.SetParent(go.transform);
            rtlTextGO.transform.localPosition = Vector3.zero;

            RTLTextMeshPro tmp = rtlTextGO.GetComponent<RTLTextMeshPro>();

            tmp.text = originalText;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.raycastTarget = raycastTarget;
            tmp.isRightToLeftText = true;
            tmp.enableAutoSizing = enableAutoSize;

            if (selectedFont != null)
                tmp.font = selectedFont;

            switch (alignment)
            {
                case TextAnchor.UpperLeft: tmp.alignment = TextAlignmentOptions.TopLeft; break;
                case TextAnchor.UpperCenter: tmp.alignment = TextAlignmentOptions.Top; break;
                case TextAnchor.UpperRight: tmp.alignment = TextAlignmentOptions.TopRight; break;
                case TextAnchor.MiddleLeft: tmp.alignment = TextAlignmentOptions.Left; break;
                case TextAnchor.MiddleCenter: tmp.alignment = TextAlignmentOptions.Center; break;
                case TextAnchor.MiddleRight: tmp.alignment = TextAlignmentOptions.Right; break;
                case TextAnchor.LowerLeft: tmp.alignment = TextAlignmentOptions.BottomLeft; break;
                case TextAnchor.LowerCenter: tmp.alignment = TextAlignmentOptions.Bottom; break;
                case TextAnchor.LowerRight: tmp.alignment = TextAlignmentOptions.BottomRight; break;
            }

            RectTransform rtlRectTransform = rtlTextGO.GetComponent<RectTransform>();
            rtlRectTransform.position = rectTransform.position;
            rtlRectTransform.rotation = rectTransform.rotation;
            rtlRectTransform.localScale = rectTransform.localScale;

            if (fullAnchorStretch)
            {
                rtlRectTransform.anchorMin = Vector2.zero;
                rtlRectTransform.anchorMax = Vector2.one;
                rtlRectTransform.offsetMin = Vector2.zero;
                rtlRectTransform.offsetMax = Vector2.zero;
            }

            if (removeOldComponents)
            {
                if (outline != null)
                {
                    var tmpOutline = rtlTextGO.AddComponent<Outline>();
                    tmpOutline.effectColor = outline.effectColor;
                    tmpOutline.effectDistance = outline.effectDistance;
                    tmpOutline.useGraphicAlpha = outline.useGraphicAlpha;
                    DestroyImmediate(outline);
                }

                if (shadow != null)
                {
                    var tmpShadow = rtlTextGO.AddComponent<Shadow>();
                    tmpShadow.effectColor = shadow.effectColor;
                    tmpShadow.effectDistance = shadow.effectDistance;
                    tmpShadow.useGraphicAlpha = shadow.useGraphicAlpha;
                    DestroyImmediate(shadow);
                }

                DestroyImmediate(oldText);
            }

            EditorUtility.SetDirty(go);
        }

        Debug.Log("✅ Conversion complete!");
        FindAllTexts();
    }
}
