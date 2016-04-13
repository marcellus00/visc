using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Visc
{
	public class SimpleTranspositionEvent : EventAction
	{
		[SerializeField] private Transform _transformFrom;
		[SerializeField] private Transform _transformTo;

		private Vector3 _from;
		private Vector3 _to;

		private float _start;
		private float _journeyLength;
		private float _speed;

		protected override void OnStart(float startTime)
		{
			_start = startTime;

			if (_transformFrom == null || _transformTo == null) return;
			_from = _transformFrom.position;
			_to = _transformTo.position;
			_journeyLength = Vector3.Distance(_from, _to);
			_speed = _journeyLength / Duration;
		}

		protected override void OnUpdate(ref float currentTime)
		{
			if (_actor == null) return;
			var coveredDistance = (currentTime - _start) * _speed;
			var journeyFraction = coveredDistance / _journeyLength;
			_actor.transform.position = Vector3.Lerp(_from, _to, journeyFraction);
		}

		protected override void OnStop()
		{
			if (_actor == null) return;
			_actor.transform.position = _to;
		}

		protected override void OnEditorGui()
		{
#if UNITY_EDITOR
			_transformFrom = EditorGUILayout.ObjectField("From", _transformFrom, typeof(Transform), true) as Transform;
			_transformTo = EditorGUILayout.ObjectField("To", _transformTo, typeof(Transform), true) as Transform;
#endif

		}
	}
}
