﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExpressionsMenu = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu;
using ExpressionControl = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu.Control;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using VRC.SDK3.Avatars.ScriptableObjects;
using System.Reflection.Emit;

using System.Text.RegularExpressions;

[CustomEditor(typeof(VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionsMenu))]
public class VRCExpressionsMenuEditor : Editor
{
    static string[] ToggleStyles = { "Pip-Slot", "Animation" };

    static string[] colorNames = new string[] {
        "black", "blue", "green", "lightblue", "grey", "orange",
        "purple", "red", "white", "yellow"
    };

    static readonly Dictionary<string, string> colorLookUp = new Dictionary<string, string>()
    {
        { "black", "#000000"}, { "blue", "#0000FF"}, {"green", "#00FF00"},
        { "lightblue", "#C6ECEE"}, {"grey", "#CECECE"}, {"orange", "#FF992C"},
        { "purple", "#8B00FF"}, { "red", "#FF0000"}, { "white", "#FFFFFF"},
        { "yellow", "#FFFF00" }
    };

    const string reColor = "<color=\"(\\w*)\">";
    const string reHexColor = "<color=(#([A-Z]|[0-9]){6})>";
    //const string reSize = "<size=((\\d)*.(\\d)*)>";
    //const float DEFAULT_TEXT_SIZE = 22.5f;


    List<UnityEngine.Object> foldoutList = new List<UnityEngine.Object>();

    public void OnDisable()
    {
        SelectAvatarDescriptor(null);
    }
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SelectAvatarDescriptor();

        if (activeDescriptor == null)
        {
            EditorGUILayout.HelpBox("No active avatar descriptor found in scene.", MessageType.Error);
        }
        EditorGUILayout.Space();

        //Controls
        EditorGUI.BeginDisabledGroup(activeDescriptor == null);
        EditorGUILayout.LabelField("Controls");
        EditorGUI.indentLevel += 1;
        {
            var controls = serializedObject.FindProperty("controls");
            for (int i = 0; i < controls.arraySize; i++)
            {
                var control = controls.GetArrayElementAtIndex(i);
                DrawControl(controls, control as SerializedProperty, i);
            }

            //Add
            EditorGUI.BeginDisabledGroup(controls.arraySize >= ExpressionsMenu.MAX_CONTROLS);
            if (GUILayout.Button("Add Control"))
            {
                var menu = serializedObject.targetObject as ExpressionsMenu;

                var control = new ExpressionControl();
                control.name = "New Control";
                menu.controls.Add(control);
            }
            EditorGUI.EndDisabledGroup();
        }
        EditorGUI.indentLevel -= 1;
        EditorGUI.EndDisabledGroup();

        serializedObject.ApplyModifiedProperties();
    }
    void DrawControl(SerializedProperty controls, SerializedProperty control, int index)
    {
        var name = control.FindPropertyRelative("name");
        var icon = control.FindPropertyRelative("icon");
        var type = control.FindPropertyRelative("type");
        var parameter = control.FindPropertyRelative("parameter");
        var value = control.FindPropertyRelative("value");
        var subMenu = control.FindPropertyRelative("subMenu");

        var subParameters = control.FindPropertyRelative("subParameters");
        var labels = control.FindPropertyRelative("labels");

        var controlType = (ExpressionControl.ControlType)type.intValue;

        // decorator linting
        #region Edited By Cam
        var rawColor = Regex.Match(name.stringValue, reColor);
        if (rawColor.Success)
        {
            string color = rawColor.Groups[1].Value;
            string replaceWith = colorLookUp.ContainsKey(color)
                ? colorLookUp[color]
                : "#FFFFFF";
            name.stringValue = name.stringValue.Replace(rawColor.Value, $"<color={replaceWith}>");
        }

        var hexColor = Regex.Match(name.stringValue, reHexColor);
        bool bold = Regex.Match(name.stringValue, "<b>").Success;
        bool italics = Regex.Match(name.stringValue, "<i>").Success;

        if (!hexColor.Success)
        {
            name.stringValue = $"<color=#FFFFFF>{name.stringValue}";
            return;
        }

        //var size = Regex.Match(name.stringValue, reSize);
        //if(!size.Success) {
        //    name.stringValue = $"<size={DEFAULT_TEXT_SIZE}>{name.stringValue}";
        //    return;
        //}

        string friendlyName = name.stringValue
            .Replace(hexColor.Value, "")
            //.Replace(size.Value, "")
            .Replace("<b>", "")
            .Replace("<i>", "");


        //Foldout
        EditorGUI.BeginChangeCheck();
        control.isExpanded = EditorGUILayout.Foldout(control.isExpanded, friendlyName);
        #endregion Edited By Cam

        if (!control.isExpanded)
            return;

        //Box
        GUILayout.BeginVertical(GUI.skin.box);
        {
            //Up, Down, Delete
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Up", GUILayout.Width(64)))
            {
                if (index > 0)
                {
                    controls.MoveArrayElement(index, index - 1);
                    return;
                }
            }
            if (GUILayout.Button("Down", GUILayout.Width(64)))
            {
                if (index < controls.arraySize - 1)
                {
                    controls.MoveArrayElement(index, index + 1);
                    return;
                }
            }
            if (GUILayout.Button("Delete", GUILayout.Width(64)))
            {
                controls.DeleteArrayElementAtIndex(index);
                return;
            }
            GUILayout.EndHorizontal();

            //Generic params
            EditorGUI.indentLevel += 1;
            {
                #region Edited By Cam
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                friendlyName = EditorGUILayout.TextField("Name", friendlyName);
                GUILayout.Space(5);
                EditorGUI.indentLevel += 2;
                {
                    // color
                    Color curColor = Color.white;
                    string colorName = hexColor.Groups[1].Value;
                    ColorUtility.TryParseHtmlString(colorName, out curColor);
                    curColor = EditorGUILayout.ColorField(new GUIContent("Text Color"), curColor, true, false, false);
                    colorName = ColorUtility.ToHtmlStringRGB(curColor);

                    // decorators
                    bold = EditorGUILayout.Toggle("Bold?", bold);
                    italics = EditorGUILayout.Toggle("Italics?", italics);
                    string decorators = string.Empty + (bold ? "<b>" : "") + (italics ? "<i>" : "");

                    //name.stringValue.Replace(size.Groups[1].Value, fSize.ToString());
                    name.stringValue = $"{decorators}<color=#{colorName}>{friendlyName}";
                    GUILayout.Space(5);
                }
                EditorGUI.indentLevel -= 2;

                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
                #endregion Edited By Cam

                EditorGUILayout.BeginHorizontal();
                {
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Space(10);
                        EditorGUILayout.PropertyField(type);

                        //Type Info
                        switch (controlType)
                        {
                            case ExpressionControl.ControlType.Button:
                                EditorGUILayout.HelpBox("Click or hold to activate. The button remains active for a minimum 0.2s.\nWhile active the (Parameter) is set to (Value).\nWhen inactive the (Parameter) is reset to zero.", MessageType.Info);
                                break;
                            case ExpressionControl.ControlType.Toggle:
                                EditorGUILayout.HelpBox("Click to toggle on or off.\nWhen turned on the (Parameter) is set to (Value).\nWhen turned off the (Parameter) is reset to zero.", MessageType.Info);
                                break;
                            case ExpressionControl.ControlType.SubMenu:
                                EditorGUILayout.HelpBox("Opens another expression menu.\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
                                break;
                            case ExpressionControl.ControlType.TwoAxisPuppet:
                                EditorGUILayout.HelpBox("Puppet menu that maps the joystick to two parameters (-1 to +1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
                                break;
                            case ExpressionControl.ControlType.FourAxisPuppet:
                                EditorGUILayout.HelpBox("Puppet menu that maps the joystick to four parameters (0 to 1).\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
                                break;
                            case ExpressionControl.ControlType.RadialPuppet:
                                EditorGUILayout.HelpBox("Puppet menu that sets a value based on joystick rotation. (0 to 1)\nWhen opened the (Parameter) is set to (Value).\nWhen closed (Parameter) is reset to zero.", MessageType.Info);
                                break;
                        }
                    }

                    // icon
                    Texture2D iconImage = (Texture2D)icon.objectReferenceValue;
                    iconImage = (Texture2D)EditorGUILayout.ObjectField(
                        iconImage,
                        typeof(Texture2D),
                        false,
                        GUILayout.Width(120),
                        GUILayout.Height(90)
                    );
                    icon.objectReferenceValue = iconImage;

                    GUILayout.Space(10);
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(10);
                //Param
                switch (controlType)
                {
                    case ExpressionControl.ControlType.Button:
                    case ExpressionControl.ControlType.Toggle:
                    case ExpressionControl.ControlType.SubMenu:
                    case ExpressionControl.ControlType.TwoAxisPuppet:
                    case ExpressionControl.ControlType.FourAxisPuppet:
                    case ExpressionControl.ControlType.RadialPuppet:
                        DrawParameterDropDown(parameter, "Parameter");
                        DrawParameterValue(parameter, value);
                        break;
                }
                
                EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

                //Style
                /*if (controlType == ExpressionsControl.ControlType.Toggle)
				{
					style.intValue = EditorGUILayout.Popup("Visual Style", style.intValue, ToggleStyles);
				}*/

                //Sub menu
                if (controlType == ExpressionControl.ControlType.SubMenu)
                {
                    EditorGUILayout.PropertyField(subMenu);
                }

                //Puppet Parameter Set
                switch (controlType)
                {
                    case ExpressionControl.ControlType.TwoAxisPuppet:
                        subParameters.arraySize = 2;
                        labels.arraySize = 4;
                            
                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(0), "Parameter Horizontal", false);
                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(1), "Parameter Vertical", false);

                        DrawLabel(labels.GetArrayElementAtIndex(0), "Label Up");
                        DrawLabel(labels.GetArrayElementAtIndex(1), "Label Right");
                        DrawLabel(labels.GetArrayElementAtIndex(2), "Label Down");
                        DrawLabel(labels.GetArrayElementAtIndex(3), "Label Left");
                        break;
                    case ExpressionControl.ControlType.FourAxisPuppet:
                        subParameters.arraySize = 4;
                        labels.arraySize = 4;

                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(0), "Parameter Up", false);
                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(1), "Parameter Right", false);
                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(2), "Parameter Down", false);
                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(3), "Parameter Left", false);

                        DrawLabel(labels.GetArrayElementAtIndex(0), "Label Up");
                        DrawLabel(labels.GetArrayElementAtIndex(1), "Label Right");
                        DrawLabel(labels.GetArrayElementAtIndex(2), "Label Down");
                        DrawLabel(labels.GetArrayElementAtIndex(3), "Label Left");

                        break;
                    case ExpressionControl.ControlType.RadialPuppet:
                        subParameters.arraySize = 1;
                        labels.arraySize = 0;

                        DrawParameterDropDown(subParameters.GetArrayElementAtIndex(0), "Paramater Rotation", false);
                        break;
                    default:
                        subParameters.arraySize = 0;
                        labels.arraySize = 0;
                        break;
                }
            }
            EditorGUI.indentLevel -= 1;
        }
        GUILayout.EndVertical();
    }

    void DrawLabel(SerializedProperty subControl, string name)
    {
        var nameProp = subControl.FindPropertyRelative("name");
        var icon = subControl.FindPropertyRelative("icon");

        EditorGUILayout.LabelField(name);
        EditorGUI.indentLevel += 2;

        EditorGUILayout.PropertyField(nameProp);
       /* using (new EditorGUILayout.VerticalScope("box"))
        {
            Texture2D iconImage = (Texture2D)icon.objectReferenceValue;
            iconImage = (Texture2D)EditorGUILayout.ObjectField(iconImage, typeof(Texture2D), false, GUILayout.Width(90), GUILayout.Height(90));
            icon.objectReferenceValue = iconImage;
        }*/

        EditorGUILayout.PropertyField(icon);

        EditorGUI.indentLevel -= 2;
    }

    void DrawInfoHover(string text)
    {
        GUILayout.Button(new GUIContent("?", text), GUILayout.MaxWidth(32));
    }
    void DrawInfo(string text)
    {
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        GUILayout.Label(text, GUI.skin.textArea, GUILayout.MaxWidth(400));
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    VRC.SDK3.Avatars.Components.VRCAvatarDescriptor activeDescriptor = null;
    string[] parameterNames;
    void SelectAvatarDescriptor()
    {
        var descriptors = GameObject.FindObjectsOfType<VRC.SDK3.Avatars.Components.VRCAvatarDescriptor>();
        if (descriptors.Length > 0)
        {
            //Compile list of names
            string[] names = new string[descriptors.Length];
            for (int i = 0; i < descriptors.Length; i++)
                names[i] = descriptors[i].gameObject.name;

            //Select
            var currentIndex = System.Array.IndexOf(descriptors, activeDescriptor);
            var nextIndex = EditorGUILayout.Popup("Active Avatar", currentIndex, names);
            if (nextIndex < 0)
                nextIndex = 0;
            if (nextIndex != currentIndex)
                SelectAvatarDescriptor(descriptors[nextIndex]);
        }
        else
            SelectAvatarDescriptor(null);
    }
    void SelectAvatarDescriptor(VRC.SDK3.Avatars.Components.VRCAvatarDescriptor desc)
    {
        if (desc == activeDescriptor)
            return;

        activeDescriptor = desc;
        if (activeDescriptor != null)
        {
            //Init stage parameters
            int paramCount = desc.GetExpressionParameterCount();
            parameterNames = new string[paramCount + 1];
            parameterNames[0] = "[None]";
            for (int i = 0; i < paramCount; i++)
            {
                var param = desc.GetExpressionParameter(i);
                string name = "[None]";
                if (param != null && !string.IsNullOrEmpty(param.name))
                    name = string.Format("{0}, {1}", param.name, param.valueType.ToString(), i + 1);
                parameterNames[i + 1] = name;
            }
        }
        else
        {
            parameterNames = null;
        }
    }
    int GetExpressionParametersCount()
    {
        if (activeDescriptor != null && activeDescriptor.expressionParameters != null && activeDescriptor.expressionParameters.parameters != null)
            return activeDescriptor.expressionParameters.parameters.Length;
        return 0;
    }
    ExpressionParameters.Parameter GetExpressionParameter(int i)
    {
        if (activeDescriptor != null)
            return activeDescriptor.GetExpressionParameter(i);
        return null;
    }
    void DrawParameterDropDown(SerializedProperty parameter, string name, bool allowBool = true)
    {
        var parameterName = parameter.FindPropertyRelative("name");
        VRCExpressionParameters.Parameter param = null;
        string value = parameterName.stringValue;

        bool parameterFound = false;
        EditorGUILayout.BeginHorizontal();
        {
            if (activeDescriptor != null)
            {
                //Dropdown
                int currentIndex;
                if (string.IsNullOrEmpty(value))
                {
                    currentIndex = -1;
                    parameterFound = true;
                }
                else
                {
                    currentIndex = -2;
                    for (int i = 0; i < GetExpressionParametersCount(); i++)
                    {
                        var item = activeDescriptor.GetExpressionParameter(i);
                        if (item.name == value)
                        {
                            param = item;
                            parameterFound = true;
                            currentIndex = i;
                            break;
                        }
                    }
                }

                //Dropdown
                EditorGUI.BeginChangeCheck();
                using (new EditorGUILayout.HorizontalScope())
                {
                    // only variable width for "Parameter Rotation" //
                    float width = name.Equals("Parameter") ? 90 : 150;
                    EditorGUILayout.LabelField(name, GUILayout.Width(width));
                    currentIndex = EditorGUILayout.Popup(/*name, */currentIndex + 1, parameterNames);
                }

                if (EditorGUI.EndChangeCheck())
                {
                    if (currentIndex == 0)
                        parameterName.stringValue = "";
                    else
                        parameterName.stringValue = GetExpressionParameter(currentIndex - 1).name;
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.Popup(0, new string[0]);
                EditorGUI.EndDisabledGroup();
            }

            //Text field
            parameterName.stringValue = EditorGUILayout.TextField(parameterName.stringValue, GUILayout.MaxWidth(200));
        }
        EditorGUILayout.EndHorizontal();

        if (!parameterFound)
        {
            EditorGUILayout.HelpBox("Parameter not found on the active avatar descriptor.", MessageType.Warning);
        }

        if (!allowBool && param != null && param.valueType == ExpressionParameters.ValueType.Bool)
        {
            EditorGUILayout.HelpBox("Bool parameters not valid for this choice.", MessageType.Error);
        }
    }
    void DrawParameterValue(SerializedProperty parameter, SerializedProperty value)
    {
        string paramName = parameter.FindPropertyRelative("name").stringValue;
        if (!string.IsNullOrEmpty(paramName))
        {
            var paramDef = FindExpressionParameterDef(paramName);
            if (paramDef != null)
            {
                if (paramDef.valueType == ExpressionParameters.ValueType.Int)
                {
                    value.floatValue = EditorGUILayout.IntField("Value", Mathf.Clamp((int)value.floatValue, 0, 255));
                }
                else if (paramDef.valueType == ExpressionParameters.ValueType.Float)
                {
                    value.floatValue = EditorGUILayout.FloatField("Value", Mathf.Clamp(value.floatValue, -1f, 1f));
                }
                else if (paramDef.valueType == ExpressionParameters.ValueType.Bool)
                {
                    value.floatValue = 1f;
                }
            }
            else
            {
                EditorGUI.BeginDisabledGroup(true);
                value.floatValue = EditorGUILayout.FloatField("Value", value.floatValue);
                EditorGUI.EndDisabledGroup();
            }
        }
    }

    ExpressionParameters.Parameter FindExpressionParameterDef(string name)
    {
        if (activeDescriptor == null || string.IsNullOrEmpty(name))
            return null;

        //Find
        int length = GetExpressionParametersCount();
        for (int i = 0; i < length; i++)
        {
            var item = GetExpressionParameter(i);
            if (item != null && item.name == name)
                return item;
        }
        return null;
    }
}