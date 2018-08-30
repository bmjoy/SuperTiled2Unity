﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace SuperTiled2Unity.Editor
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TmxAssetImporter))]
    class TmxAssetImporterEditor : TiledAssetImporterEditor<TmxAssetImporter>
    {
        // Serialized properties
        private SerializedProperty m_AnimationFramerate;
        private readonly GUIContent m_AnimationFramerateContent = new GUIContent("Animation Frame Rate", "How many frames of tile animation play per second.");

        private SerializedProperty m_TilesAsObjects;
        private readonly GUIContent m_TilesAsObjectsContent = new GUIContent("Tiles as Objects", "Place each tile as separate game object. Uses more resources but gives you more control. This is ignored for Isometric maps that are forced to use game objects.");

        private SerializedProperty m_CustomImporterClassName;

        private string[] m_CustomImporterNames;
        private string[] m_CustomImporterTypes;
        private int m_SelectedCustomImporter;

        protected override string EditorLabel
        {
            get { return "Tiled Map Importer (.tmx files)"; }
        }

        protected override string EditorDefinition
        {
            get { return "This imports Tiled map files (*.tmx) and creates a prefab of your map to be added to your scenes."; }
        }

        public override void OnEnable()
        {
            CacheSerializedProperites();
            EnumerateCustomImporterClasses();
            base.OnEnable();
        }

        protected override void InternalOnInspectorGUI()
        {
            EditorGUILayout.LabelField("Tiled Map Importer Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_AnimationFramerate, m_AnimationFramerateContent);
            ShowTiledAssetGui();

            EditorGUI.BeginDisabledGroup(TargetAssetImporter.IsIsometric);
            EditorGUILayout.PropertyField(m_TilesAsObjects, m_TilesAsObjectsContent);
            EditorGUI.EndDisabledGroup();

            if (TargetAssetImporter.IsIsometric)
            {
                using (new GuiScopedIndent())
                {
                    EditorGUILayout.HelpBox("Note: Isometric maps are forced to display tiles as objects.", MessageType.None);
                }
            }

            EditorGUILayout.Space();
            ShowCustomImporterGui();

            ApplyRevertGUI();
        }

        protected override void ResetValues()
        {
            base.ResetValues();
            CacheSerializedProperites();
        }

        protected override void Apply()
        {
            // Set any limits on properties
            m_AnimationFramerate.floatValue = Clamper.ClampAnimationFramerate(m_AnimationFramerate.floatValue);
            base.Apply();
        }

        private void CacheSerializedProperites()
        {
            m_AnimationFramerate = serializedObject.FindProperty("m_AnimationFramerate");
            Assert.IsNotNull(m_AnimationFramerate);

            m_TilesAsObjects = serializedObject.FindProperty("m_TilesAsObjects");
            Assert.IsNotNull(m_TilesAsObjects);

            m_CustomImporterClassName = serializedObject.FindProperty("m_CustomImporterClassName");
            Assert.IsNotNull(m_CustomImporterClassName);
        }

        private void EnumerateCustomImporterClasses()
        {
            var importerNames = new List<string>();
            var importerTypes = new List<string>();

            var baseType = typeof(CustomTmxImporter);
            var customTypes = Assembly.GetAssembly(baseType).GetTypes().Where(t => t.IsClass && !t.IsAbstract && t.IsSubclassOf(baseType)).OrderBy(t => t.GetDisplayName());

            foreach (var t in customTypes)
            {
                importerNames.Add(t.GetDisplayName());
                importerTypes.Add(t.FullName);
            }

            importerNames.Insert(0, "None");
            importerTypes.Insert(0, string.Empty);

            m_CustomImporterNames = importerNames.ToArray();
            m_CustomImporterTypes = importerTypes.ToArray();

            m_SelectedCustomImporter = importerTypes.IndexOf(m_CustomImporterClassName.stringValue);
            if (m_SelectedCustomImporter == -1)
            {
                m_SelectedCustomImporter = 0;
                m_CustomImporterClassName.stringValue = string.Empty;
            }
        }

        private void ShowCustomImporterGui()
        {
            EditorGUILayout.LabelField("Custom Importer Settings", EditorStyles.boldLabel);
            var selected = EditorGUILayout.Popup("Custom Importer", m_SelectedCustomImporter, m_CustomImporterNames);

            if (selected != m_SelectedCustomImporter)
            {
                m_SelectedCustomImporter = selected;
                m_CustomImporterClassName.stringValue = m_CustomImporterTypes.ElementAtOrDefault(selected);
            }

            EditorGUILayout.HelpBox("Custom Importers are an advanced feature that require scripting. Create a class inherited from CustomTmxImporter and select it from the list above.", MessageType.None);
        }
    }
}
