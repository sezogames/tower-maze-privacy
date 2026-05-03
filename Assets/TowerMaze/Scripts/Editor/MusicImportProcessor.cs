#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TowerMaze.EditorTools
{
    /// <summary>
    /// Auto-applies mobile-friendly import settings to every audio file dropped
    /// under Assets/Resources/TowerMaze/Music/. Triggers in two ways:
    ///   1. AssetPostprocessor: any new music file added to the folder gets the
    ///      settings on first import.
    ///   2. Tools → TowerMaze → Reimport Music Folder: forces re-import on the
    ///      existing files so retroactive changes take effect.
    /// Settings applied (per the music spec for TowerMaze):
    ///   - Load Type: Compressed In Memory (small RAM footprint at runtime)
    ///   - Compression Format: Vorbis (best compression for music)
    ///   - Quality: 60% (good enough for casual mobile, ~80% size reduction)
    ///   - Force To Mono: ON (music doesn't need stereo, halves file size)
    ///   - Sample Rate: Optimize Sample Rate (Unity picks the smallest viable)
    ///   - Override per platform: Standalone / iOS / Android all use same settings
    /// </summary>
    public sealed class MusicImportProcessor : AssetPostprocessor
    {
        private const string MusicFolder = "Assets/Resources/TowerMaze/Music/";

        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith(MusicFolder)) return;
            AudioImporter importer = (AudioImporter)assetImporter;
            ApplyMobileSettings(importer);
        }

        [MenuItem("Tools/TowerMaze/Reimport Music Folder")]
        public static void ReimportMusicFolder()
        {
            string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { MusicFolder.TrimEnd('/') });
            if (guids.Length == 0)
            {
                Debug.LogWarning("[MusicImportProcessor] No audio clips found under " + MusicFolder);
                return;
            }

            List<string> changed = new List<string>(guids.Length);
            try
            {
                AssetDatabase.StartAssetEditing();
                foreach (string guid in guids)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    AudioImporter importer = AssetImporter.GetAtPath(path) as AudioImporter;
                    if (importer == null) continue;
                    ApplyMobileSettings(importer);
                    importer.SaveAndReimport();
                    changed.Add(Path.GetFileName(path));
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            Debug.Log($"[MusicImportProcessor] Re-imported {changed.Count} clip(s) with mobile settings: {string.Join(", ", changed)}");
        }

        private static void ApplyMobileSettings(AudioImporter importer)
        {
            importer.forceToMono = true;
            importer.loadInBackground = false;

            // preloadAudioData moved into AudioImporterSampleSettings as a
            // per-platform value in modern Unity; the importer-level property
            // is obsolete. Setting it here in the struct propagates to default
            // and per-platform overrides.
            AudioImporterSampleSettings settings = new AudioImporterSampleSettings
            {
                loadType = AudioClipLoadType.CompressedInMemory,
                compressionFormat = AudioCompressionFormat.Vorbis,
                quality = 0.60f,
                sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate,
                preloadAudioData = false,
            };

            importer.defaultSampleSettings = settings;
            // Override for the major platforms so the editor preview matches what
            // ships on device. Unity silently falls back to default if a target
            // is missing, so calling SetOverrideSampleSettings on each is safe.
            importer.SetOverrideSampleSettings("Standalone", settings);
            importer.SetOverrideSampleSettings("iOS", settings);
            importer.SetOverrideSampleSettings("Android", settings);
        }
    }
}
#endif
