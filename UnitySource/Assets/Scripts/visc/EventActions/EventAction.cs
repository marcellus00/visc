using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Visc
{
	public abstract class EventAction : ScriptableObject
	{
		public const string ActionName = "Generic event action";

		[SerializeField] protected string _description;
		[SerializeField] protected GameObject _actor;
		[SerializeField] protected float _startTime;
		[SerializeField] protected float _duration = 1f;
		[SerializeField] protected int _editingTrack;

		protected GUIStyle GuiStyle;

		public int EditingTrack
		{
			get { return _editingTrack; }
			set { _editingTrack = value >= 0 ? value : 0; }
		}

		public GameObject Actor { get { return _actor; } }
		public string Description { get { return _description; } }
		public float StartTime { get { return _startTime; } set { _startTime = value >= 0f ? value : 0f; } }
		public float Duration { get { return _duration; } set { _duration = value >= 0.1f ? value : 0.1f; } }
		public float EndTime { get { return _startTime + _duration; } }

		public bool NowPlaying { get; protected set; }

		public void ActionStart(float starTime)
		{
			Debug.Log("[EventSystem] Started event " + _description);
			NowPlaying = true;
			OnStart(starTime);
		}

		public void ActionUpdate(ref float timeSinceActionStart) { OnUpdate(ref timeSinceActionStart); }

		public void Stop()
		{
			Debug.Log("[EventSystem] Finished event " + _description);
			NowPlaying = false;
			OnStop();
		}

		public virtual void DrawTimelineGui(Rect rect)
		{
			if (GuiStyle == null || string.IsNullOrEmpty(GuiStyle.name))
				GuiStyle = new GUIStyle(GUI.skin.box);

			GUI.Box(rect, ToString() + " " + _description + "\nStart time: " + _startTime + "; Duartion: " + _duration + "; End time: " + EndTime, GuiStyle);
		}

		public void DrawEditorGui()
		{
			#if UNITY_EDITOR
			_description = EditorGUILayout.TextField("Description", _description);
			_startTime = EditorGUILayout.FloatField("Start time", _startTime);
			_duration = EditorGUILayout.FloatField("Duration", _duration);
			_actor = EditorGUILayout.ObjectField("Actor", _actor, typeof(GameObject), true) as GameObject;

			OnEditorGui();
			#endif
		}

		protected virtual void OnEditorGui() { }
		protected virtual void OnStart(float startTime) { }
		protected virtual void OnUpdate(ref float currentTime) { }
		protected virtual void OnStop() { }

		public bool CheckIntersection(EventAction action)
		{
			return CheckIntersection(action, action.EditingTrack, action.StartTime, action.EndTime);
		}

		public bool CheckIntersection(EventAction action, float track, float startTime, float endTime)
		{
			if (action == this) return false;
			var sameEditingTrack = track == EditingTrack;
			var intersectsIn = StartTime < endTime && startTime < EndTime;

			return (sameEditingTrack && intersectsIn);
		}
		
		public static Texture2D MakeTex(int width, int height, Color col)
		{
			var pix = new Color[width * height];
			for (var i = 0; i < pix.Length; ++i)
				pix[i] = col;
			var result = new Texture2D(width, height);
			result.SetPixels(pix);
			result.Apply();
			return result;
		}
	}
}
