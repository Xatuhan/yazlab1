using UnityEngine;
using UnityEditor;
using System.IO;

public class AutoURPMaterialCreator : Editor
{
    [MenuItem("Assets/Create URP Material From Textures", false, 10)]
    private static void CreateMaterialFromSelectedTextures()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects.Length == 0) return;

        // Varsayılan URP Lit Shader'ı al
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        if (urpShader == null)
        {
            Debug.LogError("URP Lit Shader bulunamadı!");
            return;
        }

        // İlk seçili dosyanın yolundan materyal adı belirle
        string firstPath = AssetDatabase.GetAssetPath(selectedObjects[0]);
        string directory = Path.GetDirectoryName(firstPath);
        string matName = "M_" + Path.GetFileNameWithoutExtension(firstPath).Split('_')[0];

        Material mat = new Material(urpShader);
        string savePath = Path.Combine(directory, matName + ".mat");

        foreach (Object obj in selectedObjects)
        {
            if (obj is Texture2D texture)
            {
                string name = texture.name.ToLower();

                // İsim sonundaki harflere göre otomatik eşleştirme
                if (name.Contains("_b") || name.Contains("albedo") || name.Contains("diffuse") || name.Contains("basecolor"))
                {
                    mat.SetTexture("_BaseMap", texture);
                }
                else if (name.Contains("_n") || name.Contains("normal") || name.Contains("nrm"))
                {
                    mat.SetTexture("_BumpMap", texture);
                    // Normal map import ayarını düzelt
                    TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                    if (importer != null && importer.textureType != TextureImporterType.NormalMap)
                    {
                        importer.textureType = TextureImporterType.NormalMap;
                        importer.SaveAndReimport();
                    }
                }
                else if (name.Contains("_m") || name.Contains("metallic"))
                {
                    mat.SetTexture("_MetallicGlossMap", texture);
                }
                else if (name.Contains("_e") || name.Contains("emission"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetTexture("_EmissionMap", texture);
                    mat.SetColor("_EmissionColor", Color.white);
                }
                else if (name.Contains("_ao") || name.Contains("occlusion"))
                {
                    mat.SetTexture("_OcclusionMap", texture);
                }
            }
        }

        AssetDatabase.CreateAsset(mat, savePath);
        AssetDatabase.SaveAssets();
        Debug.Log($"Otomatik URP Materyali Oluşturuldu: {savePath}");
    }
}