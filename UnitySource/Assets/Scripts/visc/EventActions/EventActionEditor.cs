#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Visc
{
	public class EventActionEditor : EditorWindow
	{	
		private EventAction _currentAction;
		
		public static void ShowWindow()
		{
			GetWindow(typeof(EventActionEditor));
		}

		public void SetCurrentAction(EventAction action)
		{
			_currentAction = action;
		}

		private void OnGUI()
		{
			if (_currentAction != null)
			{
				if (Event.current.type == EventType.keyDown && Event.current.keyCode == KeyCode.Return)
				{
					if(!Application.isPlaying)
						EditorSceneManager.MarkAllScenesDirty();
					Close();
				}

				_currentAction.DrawEditorGui();

				if(!Application.isPlaying && GUILayout.Button("Save"))
					EditorSceneManager.MarkAllScenesDirty();
			}
			else
			{
				GUILayout.Label("Select action");
			}
		}
	}
}
#endif