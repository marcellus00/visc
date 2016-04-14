#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
using System.Collections.Generic;
using UnityEditor.SceneManagement;

namespace Visc
{
	public class ScenarioEditorWindow : EditorWindow
	{
		private int BoxHeight = 35;
		private const float HandleWidth = 10f;

		private Scenario _currentScenario;

		private Scenario CurrentScenario
		{
			get {return _currentScenario;}
			set { _currentScenario = value;}
		}

		private EventAction _draggedAction;
		private  readonly List<EventAction> _selectedActions = new List<EventAction>(); 

		private float _startPositionX;
		private bool _endHandleDragged;
		private bool _startHandleDragged;
		private bool _batchSelect;
		
		private TempActionValues _draggedActionTempValues;
		private readonly Dictionary<EventAction, TempActionValues> _selectedActionsTempValues =
			new Dictionary<EventAction, TempActionValues>();

		private Type[] _eventActionTypes;

		private static int _myControlId;

		private float _vScrollPosition = 0f;
		private float _hScrollPosition = 0f;

		private GenericMenu _addActionsMenu;
		private EventActionEditor _eventActionEditor;

		[MenuItem("Window/Scenario editor %#L")]
		public static ScenarioEditorWindow ShowWindow()
		{
			var window = GetWindow(typeof (ScenarioEditorWindow), false, "Scenario Editor Window") as ScenarioEditorWindow;
            _myControlId = window.GetInstanceID();
			return window;
		}

		public void SetScenario(Scenario scenario)
		{
			CurrentScenario = scenario;
		}
		
		private float _visibleDuration;

		private void OnGUI()
		{
			if (CurrentScenario != null)
			{
				if (_eventActionTypes == null)
					_eventActionTypes =
						(from System.Type type in System.Reflection.Assembly.GetExecutingAssembly().GetTypes()
							where type.IsSubclassOf(typeof (EventAction))
							select type).ToArray();

				var actions = CurrentScenario.Actions;

				var newBatchSelect = Event.current.command || Event.current.control;
				if (_batchSelect != newBatchSelect)
				{
					_batchSelect = newBatchSelect;
					Repaint();
				}

				if (Event.current.type == EventType.ScrollWheel)
				{
					CurrentScenario.VisibleScale = CurrentScenario.VisibleScale + (Mathf.Sign(Event.current.delta.y)*0.1f);
					Repaint();
				}

				GUILayout.BeginHorizontal();

				if(Application.isPlaying)
					if(GUILayout.Button("PLAY"))
						_currentScenario.Execute();

				GUILayout.BeginHorizontal();
				CurrentScenario.VisibleScale = EditorGUILayout.Slider("Scale", CurrentScenario.VisibleScale, 0.1f, 100f);
				CurrentScenario.MaximumDuration = EditorGUILayout.FloatField("Max duration (seconds)",
					CurrentScenario.MaximumDuration);
				GUILayout.EndHorizontal();
				GUILayout.BeginHorizontal();
				CurrentScenario.MaximumTracks = EditorGUILayout.IntField("Max tracks", CurrentScenario.MaximumTracks);
				BoxHeight = EditorGUILayout.IntSlider("Track height", BoxHeight, 20, 50);

				if (_draggedAction == null)
				{
					var newVisibleDuration = CurrentScenario.MaximumDuration/CurrentScenario.VisibleScale;
					var newScale = newVisibleDuration*CurrentScenario.VisibleScale/_visibleDuration;
					_visibleDuration = newVisibleDuration;
					CurrentScenario.VisibleScale = newScale;
				}

				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				CurrentScenario.PlayOnce = EditorGUILayout.Toggle("Play once", CurrentScenario.PlayOnce);
				GUILayout.EndHorizontal();

				if (GUILayout.Button("Save"))
					EditorSceneManager.MarkAllScenesDirty();

				GUILayout.EndHorizontal();

				var lastRect = GUILayoutUtility.GetLastRect();

				if (lastRect.yMax <= 1f)
					return;

				var trackOffset = Mathf.FloorToInt(_vScrollPosition/BoxHeight);

				PerformDrag(lastRect.yMax, _visibleDuration, trackOffset);
				DrawActions(actions, lastRect.yMax, _visibleDuration, CurrentScenario.VisibleOffset, trackOffset);
				DrawTimeline(lastRect.yMax);

				var vScrollVisible = CurrentScenario.MaximumTracks*BoxHeight > position.height;
				var tempMaxDuration = vScrollVisible ? CurrentScenario.MaximumDuration + 0.1f : CurrentScenario.MaximumDuration;
				var hScrollVisible = _visibleDuration < tempMaxDuration;

				if (vScrollVisible)
					_vScrollPosition =
						GUI.VerticalScrollbar(
							new Rect(position.width - 15f, lastRect.yMax, 15f, position.height - lastRect.yMax - (hScrollVisible ? 15f : 0f)),
							_vScrollPosition, position.height - lastRect.yMax - BoxHeight, 0f, CurrentScenario.MaximumTracks*BoxHeight);
				else
					_vScrollPosition = 0f;

				if (hScrollVisible)
				{
					_hScrollPosition =
						GUI.HorizontalScrollbar(new Rect(0f, position.height - 15f, position.width - (vScrollVisible ? 15f : 0f), 15f),
							_hScrollPosition, position.width, 0f, tempMaxDuration*position.width/_visibleDuration);
					CurrentScenario.VisibleOffset = _hScrollPosition;
				}
				else
					_hScrollPosition = 0f;

				if (Event.current.type == EventType.MouseDown && Event.current.button == 1)
				{
					if (_addActionsMenu == null)
					{
						_addActionsMenu = new GenericMenu();
						foreach (var t in _eventActionTypes)
							_addActionsMenu.AddItem(new GUIContent(t.ToString()), false, CreateContextItem, t);
					}
					_addActionsMenu.ShowAsContext();
					Event.current.Use();
				}
			}
			else
			{
				_eventActionTypes = null;
				GUILayout.Label("Select scenario");
			}
		}

		private void PerformDrag(float offset, float visibleDuration, int trackOffset)
		{
			if (_draggedAction != null && GUIUtility.hotControl == _myControlId && Event.current.rawType == EventType.MouseUp)
			{
				if (CurrentScenario.Actions.Any (action => _draggedAction.CheckIntersection (action)))
				{
					_draggedAction.StartTime = _draggedActionTempValues.StartTime;
					_draggedAction.Duration = _draggedActionTempValues.Duration;
					_draggedAction.EditingTrack = _draggedActionTempValues.EditingTrack;
				}
				if(!Application.isPlaying)
					EditorSceneManager.MarkAllScenesDirty();
                _draggedAction = null;
				//Event.current.Use ();
			}

			if (_draggedAction != null)
			{
				DragAction(_draggedAction, visibleDuration, offset, trackOffset);
			}
		}

		private void DragAction(EventAction draggedAction, float visibleDuration, float offset, int trackOffset)
		{
			var newStartTime = draggedAction.StartTime;
			var newDuration = draggedAction.Duration;
			var newEditingTrack = draggedAction.EditingTrack;

			var diff = (Event.current.mousePosition.x - _startPositionX) * visibleDuration / position.width;
			if (Mathf.Abs(diff) >= 0.01f)
			{
				if (_endHandleDragged)
					newDuration = RoundToPoint1(draggedAction.Duration + diff);
				else if (_startHandleDragged)
				{
					newStartTime = RoundToPoint1(draggedAction.StartTime + diff);
					newDuration = draggedAction.Duration + draggedAction.StartTime - newStartTime;
				}
				else
					newStartTime = RoundToPoint1(draggedAction.StartTime + diff);

				_startPositionX = Event.current.mousePosition.x;
			}

			if (!_startHandleDragged && !_endHandleDragged)
			{
				var trackBorder = (draggedAction.EditingTrack + 1) * BoxHeight + offset;
				if (Event.current.mousePosition.y > trackBorder || Event.current.mousePosition.y < trackBorder - BoxHeight)
				{
					newEditingTrack = Mathf.FloorToInt((Event.current.mousePosition.y - offset) / BoxHeight + trackOffset);
					newEditingTrack = Mathf.Clamp(newEditingTrack, 0, newEditingTrack);
				}
			}

			if ((draggedAction.StartTime != newStartTime || draggedAction.Duration != newDuration ||
				draggedAction.EditingTrack != newEditingTrack))
			{
				if (newStartTime + newDuration > CurrentScenario.MaximumDuration)
					CurrentScenario.MaximumDuration = newStartTime + newDuration;

				foreach (var selectedAction in _selectedActions.Except(new[] {draggedAction}))
				{
					selectedAction.StartTime -= draggedAction.StartTime - newStartTime;
					selectedAction.Duration -= draggedAction.Duration - draggedAction.Duration;
					selectedAction.EditingTrack += newEditingTrack - draggedAction.EditingTrack;
				}

				draggedAction.StartTime = newStartTime;
				draggedAction.Duration = newDuration;
				draggedAction.EditingTrack = newEditingTrack;
				Repaint();
			}
		}

		private void DrawActions(List<EventAction> actions, float offset, float duration, float hOffset, int _trackOffset)
		{
			var maxVisibleTracks = position.height/BoxHeight + 1;
			var maxTracks = CurrentScenario.MaximumTracks;
			for(var i = 0; i < (maxTracks < maxVisibleTracks ? maxTracks : maxVisibleTracks); i++)
			{
				var trackStyle = new GUIStyle(GUI.skin.box)
				{
					normal = { background = EventAction.MakeTex(2, 2, new Color(0f, 0f, 0f, 0.1f)) },
					name = "Track Style"
				};
				GUI.Box (new Rect (0, offset + BoxHeight * i, position.width, BoxHeight + 1), string.Empty, trackStyle);
			}
				

			foreach (var action in actions)
			{			
				if(action.EditingTrack < _trackOffset || action.EditingTrack >= _trackOffset + maxVisibleTracks) continue;
				var horizontalPosStart = position.width * (action.StartTime / duration) - hOffset;
				var horizontalPosEnd = position.width * (action.EndTime / duration) - hOffset;
				var width = horizontalPosEnd - horizontalPosStart;
				var boxRect = new Rect (horizontalPosStart + HandleWidth, offset + BoxHeight * (action.EditingTrack - _trackOffset), width - HandleWidth * 2, BoxHeight);
				EditorGUIUtility.AddCursorRect (boxRect, MouseCursor.Pan);

				var boxStartHandleRect = new Rect (horizontalPosStart, offset + BoxHeight * (action.EditingTrack - _trackOffset), HandleWidth, BoxHeight);
				EditorGUIUtility.AddCursorRect (boxStartHandleRect, MouseCursor.ResizeHorizontal);
				GUI.Box (boxStartHandleRect, "<");

				var boxEndHandleRect = new Rect (horizontalPosEnd - HandleWidth, offset + BoxHeight * (action.EditingTrack - _trackOffset), HandleWidth, BoxHeight);
				EditorGUIUtility.AddCursorRect (boxEndHandleRect, MouseCursor.ResizeHorizontal);
				GUI.Box (boxEndHandleRect, ">");

				action.DrawTimelineGui (boxRect);

				var startHandle = boxStartHandleRect.Contains (Event.current.mousePosition);
				var endHandle = boxEndHandleRect.Contains (Event.current.mousePosition);
				var mainHandle = boxRect.Contains (Event.current.mousePosition);

				var alreadySelected = _selectedActions.Contains(action);

				if (!_batchSelect &&
					(Event.current.type == EventType.MouseDown || Event.current.type == EventType.ContextClick ) && (startHandle || mainHandle || endHandle))
				{
					if (Event.current.button == 0)
					{
						switch (Event.current.clickCount)
						{
						case 1:
							_draggedAction = action;
							_startPositionX = Event.current.mousePosition.x;
							_endHandleDragged = endHandle;
							_startHandleDragged = startHandle;
							GUIUtility.hotControl = _myControlId;

							_draggedActionTempValues.StartTime = action.StartTime;
							_draggedActionTempValues.Duration = action.Duration;
							_draggedActionTempValues.EditingTrack = action.EditingTrack;
							break;
						case 2:
							_eventActionEditor = GetWindow (typeof(EventActionEditor), true, "Action editor") as EventActionEditor;
							_eventActionEditor.SetCurrentAction (action);
							break;
						}
					} else if (Event.current.button == 1 || Event.current.type == EventType.ContextClick)
					{
						var actionMenu = new GenericMenu();
						actionMenu.AddItem(new GUIContent("Remove"), false, RemoveContextItem, action);
						actionMenu.ShowAsContext ();
						Event.current.Use();
					}
				}
				else if (mainHandle && _batchSelect && Event.current.type == EventType.MouseUp)
				{
					if (alreadySelected)
						_selectedActions.Remove(action);
					else
						_selectedActions.Add(action);

					Repaint();
				}

				if (_batchSelect || alreadySelected)
				{
					var toggleRect = new Rect(boxRect.xMax - 20f, boxRect.y + 1f, 5f, 5f);
					GUI.Toggle(toggleRect, alreadySelected, string.Empty);
				}
			}
		}


		private void DrawTimeline(float yMax)
		{
			if (CurrentScenario.InProgress)
			{
				var horizontalPosStart = position.width * (CurrentScenario.CurrentTime / _visibleDuration) - CurrentScenario.VisibleOffset;
				var timeRect = new Rect(horizontalPosStart, yMax, 1f, position.height - yMax);
				GUI.Box(timeRect, string.Empty);
				Repaint();
			}
		}

		private void RemoveContextItem(object obj)
		{
			var action = obj as EventAction;
			if (action == null) return;
			CurrentScenario.RemoveAction (action);
			ScriptableObject.DestroyImmediate(action);
			Repaint ();
		}

		private void CreateContextItem(object obj)
		{
			var action = obj as Type;
			if (action == null) return;

			CurrentScenario.AddAction (ScriptableObject.CreateInstance(action) as EventAction);
			Repaint ();
		}
			
		private void OnSelectionChange()
		{
			CurrentScenario = Selection.gameObjects.Length == 1 ?
				Selection.activeGameObject.GetComponent<Scenario>() : null;

			if(_eventActionEditor != null)
				_eventActionEditor.Close();

			if (CurrentScenario != null)
			{
				// разнесение пересекающихся событий на разные треки
				foreach (var action in CurrentScenario.Actions)
				{
					var newList = CurrentScenario.Actions.ToList();
					newList.Remove(action);

					foreach (var otherAction in newList)
					{
						if (otherAction.EditingTrack == action.EditingTrack &&
						   ((action.StartTime >= otherAction.StartTime && action.StartTime <= otherAction.EndTime) ||
						   (action.EndTime >= otherAction.StartTime && action.EndTime <= otherAction.EndTime)))
						{
							otherAction.EditingTrack++;
						}
					}
				}
			}

			Repaint();
		}


		private static float RoundToPoint1(float value)
		{
			return (float)Math.Round(value * 100, MidpointRounding.AwayFromZero) / 100;
		}

		private struct TempActionValues
		{
			public float StartTime;
			public float Duration;
			public int EditingTrack;
		}
	}
}
#endif