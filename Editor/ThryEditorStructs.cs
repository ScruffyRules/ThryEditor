﻿using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Thry
{
    public class CRect
    {
        public Rect r;
        public CRect(Rect r)
        {
            this.r = r;
        }
    }

    public struct InputEvent
    {
        public bool HadMouseDownRepaint;
        public bool HadMouseDown;
        public bool MouseClick;

        public bool is_alt_down;

        public bool is_drag_drop_event;
        public bool is_drop_event;

        public Vector2 mouse_position;
    }

    public abstract class ShaderPart
    {
        public int xOffset = 0;
        public GUIContent content;
        public System.Object property_data = null;
        public PropertyOptions options;
        public bool reference_properties_exist = false;

        public ShaderPart(int xOffset, string displayName, PropertyOptions options)
        {
            this.xOffset = xOffset;
            this.options = options;
            this.content = new GUIContent(displayName, options.tooltip);
            this.reference_properties_exist = options.reference_properties != null && options.reference_properties.Length > 0;
        }

        public abstract void DrawInternal(GUIContent content, CRect rect = null);
        public abstract void CopyFromMaterial(Material m);

        public void Draw(CRect rect = null, GUIContent content = null)
        {
            bool is_enabled = DrawingData.is_enabled;
            if (options.condition_enable != null && is_enabled)
            {
                DrawingData.is_enabled = options.condition_enable.Test();
                EditorGUI.BeginDisabledGroup(!DrawingData.is_enabled);
            }
            if (options.condition_show.Test())
            {
                if (content == null)
                    content = this.content;
                DrawInternal(content, rect);
                Helper.testAltClick(DrawingData.lastGuiObjectHeaderRect, this);
            }
            if (options.condition_enable != null && is_enabled)
            {
                DrawingData.is_enabled = true;
                EditorGUI.EndDisabledGroup();
            }
        }
    }

    public class ShaderGroup : ShaderPart
    {
        public List<ShaderPart> parts = new List<ShaderPart>();

        public ShaderGroup() : base(0, "", new PropertyOptions())
        {

        }

        public ShaderGroup(PropertyOptions options) : base(0, "", new PropertyOptions())
        {
            this.options = options;
        }

        public ShaderGroup(MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, PropertyOptions options) : base(xOffset, displayName, options)
        {

        }

        public void addPart(ShaderPart part)
        {
            parts.Add(part);
        }

        public override void CopyFromMaterial(Material m)
        {
            foreach (ShaderPart p in parts)
                p.CopyFromMaterial(m);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null)
        {
            foreach (ShaderPart part in parts)
            {
                part.Draw();
            }
        }
    }

    public class ShaderHeader : ShaderGroup
    {
        public ThryEditorHeader guiElement;

        public ShaderHeader() : base()
        {

        }

        public ShaderHeader(MaterialProperty prop, MaterialEditor materialEditor, string displayName, int xOffset, PropertyOptions options) : base(prop, materialEditor, displayName, xOffset, options)
        {
            this.guiElement = new ThryEditorHeader(prop);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null)
        {
            ThryEditor.currentlyDrawing.currentProperty = this;
            guiElement.Foldout(xOffset, content, ThryEditor.currentlyDrawing.gui);
            Rect headerRect = DrawingData.lastGuiObjectHeaderRect;
            if (guiElement.getState())
            {
                EditorGUILayout.Space();
                foreach (ShaderPart part in parts)
                {
                    part.Draw();
                }
                EditorGUILayout.Space();
            }
            DrawingData.lastGuiObjectHeaderRect = headerRect;
        }
    }

    public class ShaderProperty : ShaderPart
    {
        public MaterialProperty materialProperty;
        public bool drawDefault;

        public float setFloat;
        public bool updateFloat;

        public bool forceOneLine = false;

        public ShaderProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(xOffset, displayName, options)
        {
            this.materialProperty = materialProperty;
            drawDefault = false;
            this.forceOneLine = forceOneLine;
        }

        public override void CopyFromMaterial(Material m)
        {
            UnityHelper.CopyPropertyValueFromMaterial(materialProperty, m);
        }

        public override void DrawInternal(GUIContent content, CRect rect = null)
        {
            PreDraw();
            ThryEditor.currentlyDrawing.currentProperty = this;
            if (rect != null)
                DrawingData.lastGuiObjectHeaderRect = rect.r;
            else
                DrawingData.lastGuiObjectHeaderRect = new Rect(-1, -1, -1, -1);
            int oldIndentLevel = EditorGUI.indentLevel;
            EditorGUI.indentLevel = xOffset + 1;

            if (drawDefault)
                DrawDefault();
            else if (forceOneLine)
                ThryEditor.currentlyDrawing.editor.ShaderProperty(GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle), this.materialProperty, content);
            else if (rect != null)
                ThryEditor.currentlyDrawing.editor.ShaderProperty(rect.r, this.materialProperty, content);
            else
                ThryEditor.currentlyDrawing.editor.ShaderProperty(this.materialProperty, content);

            EditorGUI.indentLevel = oldIndentLevel;
            if (DrawingData.lastGuiObjectHeaderRect.x == -1) DrawingData.lastGuiObjectHeaderRect = GUILayoutUtility.GetLastRect();
        }

        public virtual void PreDraw() { }

        public virtual void DrawDefault() { }
    }

    public class TextureProperty : ShaderProperty
    {
        public bool showFoldoutProperties = false;
        public bool hasFoldoutProperties = false;
        public bool hasScaleOffset = false;

        public TextureProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool hasScaleOffset, bool forceThryUI) : base(materialProperty, displayName, xOffset, options, false)
        {
            drawDefault = forceThryUI;
            this.hasScaleOffset = hasScaleOffset;
            this.hasFoldoutProperties = hasScaleOffset || reference_properties_exist;
        }

        public override void PreDraw()
        {
            DrawingData.currentTexProperty = this;
        }

        public override void DrawDefault()
        {
            Rect pos = GUILayoutUtility.GetRect(content, Styles.vectorPropertyStyle);
            GuiHelper.drawConfigTextureProperty(pos, materialProperty, content, ThryEditor.currentlyDrawing.editor, hasFoldoutProperties);
            DrawingData.lastGuiObjectHeaderRect = pos;
        }
    }

    public class InstancingProperty : ShaderProperty
    {
        public InstancingProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            ThryEditor.currentlyDrawing.editor.EnableInstancingField();
        }
    }
    public class GIProperty : ShaderProperty
    {
        public GIProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            ThryEditor.currentlyDrawing.editor.LightmapEmissionFlagsProperty(xOffset, true);
        }
    }
    public class DSGIProperty : ShaderProperty
    {
        public DSGIProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            ThryEditor.currentlyDrawing.editor.DoubleSidedGIField();
        }
    }
    public class LocaleProperty : ShaderProperty
    {
        public LocaleProperty(MaterialProperty materialProperty, string displayName, int xOffset, PropertyOptions options, bool forceOneLine) : base(materialProperty, displayName, xOffset, options, forceOneLine)
        {
            drawDefault = true;
        }

        public override void DrawDefault()
        {
            GuiHelper.DrawLocaleSelection(this.content, ThryEditor.currentlyDrawing.gui.locale.available_locales, ThryEditor.currentlyDrawing.gui.locale.selected_locale_index);
        }
    }
}