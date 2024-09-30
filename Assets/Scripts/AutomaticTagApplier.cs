using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

// -=-=-=- //

[InitializeOnLoad]
public class SpriteTagger {
	// Configuration for sprite name conditions and corresponding tags
	private static readonly Dictionary<System.Predicate<string>, string> tagConfig = new Dictionary<System.Predicate<string>, string> {
		{ name => name.StartsWith("collision"), "Platform" },
		{ name => name.StartsWith("trapezoid_"), "Platform" },
		{ name => name.StartsWith("trigger"), "Trigger" },
		/*
		{ name => name.StartsWith("hunter_"), "Spawn" },
		{ name => name.Contains("_building"), "Backdrop" },
		*/

		// Default case
		{ name => true, "Image" }
	};

	static SpriteTagger() {
		EditorApplication.hierarchyChanged += OnHierarchyChanged;
	}

	private static void OnHierarchyChanged() {
		// Iterate through all GameObjects in the scene
		foreach (GameObject go in GameObject.FindObjectsOfType<GameObject>()) {
			// Check if the GameObject is a prefab instance
			if (PrefabUtility.IsPartOfPrefabInstance(go)) {
				// Skip prefab instances
				continue;
			}

			// Check if it has a SpriteRenderer
			SpriteRenderer spriteRenderer = go.GetComponent<SpriteRenderer>();

			if (spriteRenderer != null) {
				// Check if the GameObject's tag is not already set
				if (go.tag == "Untagged") {
					// Assign the tag based on the texture name
					if (spriteRenderer.sprite != null) {
						string spriteName = spriteRenderer.sprite.name.ToLower();

						// Find the appropriate tag based on the configuration
						foreach (var entry in tagConfig) {
							if (entry.Key(spriteName)) {
								go.tag = entry.Value;
								// Exit once set
								break;
							}
						}
					}
				}
			}
		}
	}
}
