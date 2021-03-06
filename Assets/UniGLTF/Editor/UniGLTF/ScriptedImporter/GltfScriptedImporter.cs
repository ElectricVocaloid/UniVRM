﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;


namespace UniGLTF
{
    [ScriptedImporter(1, "glb")]
    public class GltfScriptedImporter : ScriptedImporter
    {
        [SerializeField]
        Axises m_reverseAxis = default;

        const string TextureDirName = "Textures";
        const string MaterialDirName = "Materials";

        public override void OnImportAsset(AssetImportContext ctx)
        {
            Debug.Log("OnImportAsset to " + ctx.assetPath);

            try
            {
                // Parse
                var parser = new GltfParser();
                parser.ParsePath(ctx.assetPath);

                // Build Unity Model
                var externalObjectMap = GetExternalObjectMap()
                .Select(kv => (kv.Key.name, kv.Value))
                ;

                var context = new ImporterContext(parser, null, externalObjectMap);
                context.InvertAxis = m_reverseAxis;
                context.Load();
                context.ShowMeshes();

                // Texture
                foreach (var info in context.TextureFactory.Textures)
                {
                    if (!info.IsUsed)
                    {
                        continue;
                    }
                    if (!info.IsExternal)
                    {
                        var texture = info.Texture;
                        ctx.AddObjectToAsset(texture.name, texture);
                    }
                }

                // Material
                foreach (var info in context.MaterialFactory.Materials)
                {
                    if (!info.UseExternal)
                    {
                        var material = info.Asset;
                        ctx.AddObjectToAsset(material.name, material);
                    }
                }

                // Mesh
                foreach (var mesh in context.Meshes.Select(x => x.Mesh))
                {
                    ctx.AddObjectToAsset(mesh.name, mesh);
                }

                // Animation
                foreach (var clip in context.AnimationClips)
                {
                    ctx.AddObjectToAsset(clip.name, clip);
                }

                // Root
                ctx.AddObjectToAsset(context.Root.name, context.Root);
                ctx.SetMainObject(context.Root);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
            }
        }

        public void ExtractMaterialsAndTextures()
        {
            this.ExtractTextures(TextureDirName, () =>
            {
                this.ExtractAssets<UnityEngine.Material>(MaterialDirName, ".mat");
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            });
        }

        public void SetExternalUnityObject<T>(UnityEditor.AssetImporter.SourceAssetIdentifier sourceAssetIdentifier, T obj) where T : UnityEngine.Object
        {
            this.AddRemap(sourceAssetIdentifier, obj);
            AssetDatabase.WriteImportSettingsIfDirty(this.assetPath);
            AssetDatabase.ImportAsset(this.assetPath, ImportAssetOptions.ForceUpdate);
        }
    }
}
