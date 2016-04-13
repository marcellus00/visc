using UnityEditor;
using UnityEngine;

namespace Visc
{
	[CustomEditor(typeof(Scenario))]
	public class ScenarioEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			if (GUILayout.Button("Open scenario editor"))
				ScenarioEditorWindow.ShowWindow();
		}
	}
}
