using System.Collections.Generic;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using AnimatorControllerParameterType = UnityEngine.AnimatorControllerParameterType;
#endif


namespace Visc
{
	public class CustomAnimEvent : EventAction
	{
		[SerializeField] private string _triggerOnStart;
		[SerializeField] private string _triggerAtEnd;
		[SerializeField] private bool _manualSetup;
		[SerializeField] private string _manualObjectName;
		[SerializeField] private string _manualTriggerName;

		protected override void OnStart(float startTime) { SetTrigger(_triggerOnStart); }
		protected override void OnStop() { SetTrigger(_triggerAtEnd); }

		private void SetTrigger(string trigger)
		{
			if (string.IsNullOrEmpty(trigger)) return;

			var actor = _manualSetup
				? GameObject.Find(_manualObjectName) : _actor; 

			Animator actorAnim = null;
			if (actor) actorAnim = actor.GetComponent<Animator>();
			if (actorAnim == null) return;

			for (var i = 0; i < actorAnim.parameterCount; i++)
			{
				var p = actorAnim.parameters[i];
				if(p.type == AnimatorControllerParameterType.Trigger && p.name == trigger)
					actorAnim.SetTrigger(trigger);
			}
		}

#if UNITY_EDITOR
		public override void DrawTimelineGui(Rect rect)
		{

			if (GuiStyle == null || GuiStyle.name != ToString())
			{
				GuiStyle = new GUIStyle(GUI.skin.box)
				{
					normal = { background = MakeTex(2, 2, new Color(0.5f, 0.5f, 1f, 0.5f)) },
					name = ToString()
				};
			}

			GUI.Box(rect, "Animate character with : \"" + _triggerOnStart + "\", \"" + _triggerAtEnd + "\""  + "\nStart time: " + _startTime, GuiStyle);
		}

		private int _selectedParam;
		private string[] _parameterNames;

		protected override void OnEditorGui()
		{
			_manualSetup = EditorGUILayout.Toggle("Manual setup", _manualSetup);

			if (_manualSetup)
			{
				_manualObjectName = EditorGUILayout.TextField("Find object by name", _manualObjectName);
				_triggerOnStart = EditorGUILayout.TextField("Trigger name on start", _triggerOnStart);
				_triggerAtEnd = EditorGUILayout.TextField("Trigger name at end", _triggerAtEnd);
				return;
			}

			Animator actorAnim = null;
			if (_actor != null)
			{
				actorAnim = _actor.GetComponent<Animator>();
				_manualObjectName = _actor.name;
			}

            if (actorAnim == null)
			{
				GUILayout.Label("Actor does not contain animator");
				return;
			}

			if (_parameterNames == null)
			{
				_selectedParam = 0;
				var parameters = new List<AnimatorControllerParameter>();
				for (var i = 0; i < actorAnim.parameterCount; i++)
				{
					if (actorAnim.parameters[i].type == AnimatorControllerParameterType.Trigger)
						parameters.Add(actorAnim.parameters[i]);
				}

				_parameterNames = new string[parameters.Count];
				for (var i = 0; i < parameters.Count; i++)
					_parameterNames[i] = parameters.ElementAt(i).name;
			}

			if (!_parameterNames.Any())
			{
				EditorGUILayout.LabelField("Animator contains no triggers");
				return;
			}

			_selectedParam = EditorGUILayout.Popup("Animator trigger", _selectedParam, _parameterNames);

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Trigger on start"))
				_triggerOnStart = _parameterNames[_selectedParam];
			if (GUILayout.Button("Trigger at end"))
				_triggerAtEnd = _parameterNames[_selectedParam];
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Trigger on start: " + (string.IsNullOrEmpty(_triggerOnStart) ? "none" : _triggerOnStart));
			if (GUILayout.Button("X", GUILayout.Width(20))) _triggerOnStart = string.Empty;
			EditorGUILayout.EndHorizontal();
			EditorGUILayout.BeginHorizontal();
			GUILayout.Label("Trigger at end: " + (string.IsNullOrEmpty(_triggerAtEnd) ? "none" : _triggerAtEnd));
			if (GUILayout.Button("X", GUILayout.Width(20))) _triggerAtEnd = string.Empty;
			EditorGUILayout.EndHorizontal();
		}
#endif
	}
}