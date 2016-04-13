using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Visc;
using UnityEngine;

namespace Visc
{
	public class Scenario : MonoBehaviour
	{
		[SerializeField] private List<EventAction> _actions = new List<EventAction>();
		[SerializeField] private  int _maximumTracks = 5;
		[SerializeField] private float _maximumDuration = 1f;

		private float _visibleScale = 1f;

		public bool InProgress { get { return _coroutine != null; }}

		public float CurrentTime { get { return _time; } }

		public bool PlayOnce;

		public float OverallDuration { get { return _actions.Max (action => action.EndTime); } }
		
		public float VisibleOffset = 0f;

		public float MaximumDuration
		{
			get { return _maximumDuration;}
			set
			{
				_maximumDuration = value;
				if (_actions.Any())
				{
					var minMax = _actions.Max(action => action.EndTime);
					_maximumDuration = value < minMax ? minMax : value;
				}
			}
		}
		public int MaximumTracks
		{
			get {return _maximumTracks;}
			set
			{				
				_maximumTracks = value < 1 ? 1 : value;
				if (_actions.Any ())
				{
					var minMax = _actions.Max (action => action.EditingTrack) + 1;
					_maximumTracks = value < minMax ? minMax : value;
				}
			}
		}
		public float VisibleScale 
		{
			get { return _visibleScale; }			
			set { _visibleScale = Mathf.Clamp (value, 0.1f, value);	}
		}

		private Coroutine _coroutine;
		private float _time;
		private bool _canPlay = true;
		private Action _callback;

		public List<EventAction> Actions { get { return _actions.ToList(); } } 

		public void Execute(Action callback = null)
		{
			if (!_canPlay)
			{
				if(callback!= null) callback();
				Debug.Log("Scenario can play only once");
				return;
			}

			if (_coroutine != null)
				Debug.Log("[EventSystem] Scenario " + gameObject.name + " already in action. Use Restart() to restart the scenario.");
			else
			{
				_callback = callback;
				_coroutine = StartCoroutine(ExecuteScenario());
			}
		}

		public void Stop()
		{
			if (_coroutine == null) return;
			StopCoroutine(_coroutine);
			_coroutine = null;
		}

		public void Restart()
		{
			Stop();
			Execute();
        }

		public void AddAction(EventAction action)
		{
			_actions.Add (action);
		}

		public void RemoveAction(EventAction action)
		{
			_actions.Remove (action);
		}

		private IEnumerator ExecuteScenario()
		{
			Debug.Log("[EventSystem] Started execution of " + gameObject.name);
			_time = 0f;

			var totalDuration = _actions.Any () ? _actions.Max (action => action.EndTime) : 0f;
			Debug.Log ("[EventSystem] Scenario total duration " + totalDuration);

			var isPlaying = true;

			while (isPlaying)
			{
				for (var i = 0; i < _actions.Count; i++)
				{
					var action = _actions.ElementAt(i);

					if (_time >= action.StartTime && _time < action.EndTime)
					{
						if (action.NowPlaying)
							action.ActionUpdate(ref _time);
						else
							action.ActionStart(_time);
					}
					else if (_time >= action.EndTime)
					{
						if (!action.NowPlaying) continue;
						action.Stop();
					}
				}

				if(_time >= totalDuration)
					isPlaying = false;
				
				_time += Time.deltaTime;

				yield return null;
			}

			foreach (var eventAction in _actions.Where(eventAction => eventAction.NowPlaying))
				eventAction.Stop();

			_coroutine = null;

			if(_callback != null)
				_callback();

			Debug.Log("[EventSystem] Finished executing " + gameObject.name);

			_canPlay = !PlayOnce;
		}
	}
}
