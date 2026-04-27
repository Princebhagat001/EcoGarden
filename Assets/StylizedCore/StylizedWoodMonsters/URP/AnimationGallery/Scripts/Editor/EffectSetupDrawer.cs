using UnityEngine;
using UnityEditor;
using StylizedCore.StylizedWoodMonsters.AnimationGallery.Effects;

    /// <summary>
    /// Custom drawer for `AnimationEffects.EffectSetup`.
    /// Shows only fields relevant to the selected effect type.
    /// </summary>
namespace StylizedCore.StylizedWoodMonsters.AnimationGallery.Editor
{
    [CustomPropertyDrawer(typeof(AnimationEffects.EffectSetup), true)]
    public class EffectSetupDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;

            Rect r = new Rect(position.x, position.y, position.width, line);

            // Foldout header
            property.isExpanded = EditorGUI.Foldout(r, property.isExpanded, label, true);
            r.y += line + gap;

            if (property.isExpanded)
            {
                EditorGUI.indentLevel++;

                var animationName = property.FindPropertyRelative("animationName");
                var effectType = property.FindPropertyRelative("effectType");
                var effectPrefab = property.FindPropertyRelative("effectPrefab");
                var shockwavePrefab = property.FindPropertyRelative("shockwavePrefab");
                var spawnPoint = property.FindPropertyRelative("spawnPoint");
                var delay = property.FindPropertyRelative("delay");
                var coneAngle = property.FindPropertyRelative("coneAngle");
                var coneDistance = property.FindPropertyRelative("coneDistance");

                // animationName
                EditorGUI.PropertyField(r, animationName);
                r.y += line + gap;

                // effectType
                EditorGUI.PropertyField(r, effectType);
                r.y += line + gap;

                // effectPrefab is relevant for all types
                EditorGUI.PropertyField(r, effectPrefab);
                r.y += line + gap;

                // spawnPoint useful for AoE and ConeAoE and Summon (keeps it visible)
                EditorGUI.PropertyField(r, spawnPoint);
                r.y += line + gap;

                // Automatically align spawnPoint.forward to +Z when assigned (editor-only change)
                if (spawnPoint.objectReferenceValue != null)
                {
                    Transform t = spawnPoint.objectReferenceValue as Transform;
                    if (t != null)
                    {
                        Quaternion desired = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                        if (Quaternion.Angle(t.rotation, desired) > 0.5f)
                        {
                            Undo.RecordObject(t, "Align Spawn Forward to +Z");
                            t.rotation = desired;
                            EditorUtility.SetDirty(t);
                        }
                    }
                }

                // delay
                EditorGUI.PropertyField(r, delay);
                r.y += line + gap;

                int tIndex = effectType.enumValueIndex;
                // EffectType: AoE = 0, Summon = 1, ConeAoE = 2 (matches enum order)

                // shockwavePrefab only for AoE and ConeAoE
                if (tIndex == (int)AnimationEffects.EffectType.AoE || tIndex == (int)AnimationEffects.EffectType.ConeAoE)
                {
                    EditorGUI.PropertyField(r, shockwavePrefab);
                    r.y += line + gap;
                }

                // cone fields only for ConeAoE
                if (tIndex == (int)AnimationEffects.EffectType.ConeAoE)
                {
                    EditorGUI.PropertyField(r, coneAngle);
                    r.y += line + gap;

                    EditorGUI.PropertyField(r, coneDistance);
                    r.y += line + gap;
                }

                EditorGUI.indentLevel--;
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float line = EditorGUIUtility.singleLineHeight;
            float gap = EditorGUIUtility.standardVerticalSpacing;

            float height = line + gap; // foldout

            if (property.isExpanded)
            {
                // animationName, effectType, effectPrefab, spawnPoint, delay
                height += (line + gap) * 5;

                var effectType = property.FindPropertyRelative("effectType");
                int t = effectType.enumValueIndex;

                if (t == (int)AnimationEffects.EffectType.AoE || t == (int)AnimationEffects.EffectType.ConeAoE)
                    height += (line + gap); // shockwave

                if (t == (int)AnimationEffects.EffectType.ConeAoE)
                    height += (line + gap) * 2; // coneAngle + coneDistance
            }

            return height;
        }
    }
}


