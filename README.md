# visc
Extendable visual scenario editor for Unity

Created for use in Unity 5.

visc is an easy and customisable tool for creation of time-based rich and action packed scenarios for your Unity game, allowing you to control and modify events, actions and actors.

![timeline](https://github.com/marcellus00/visc/blob/master/screenshots/timeline.png?raw=true)

Basically it's a timeline editor with custom actions. Current version contains basic ones like 'move an object from point to point' or 'trigger an animation event', but you can easily create your own, like 'make character sing', 'center camera on stage' or 'do a barrel roll'

![editor](https://github.com/marcellus00/visc/blob/master/screenshots/eventactioneditor.png?raw=true)

Extend the class "EventAction", override methods OnEditorGui, OnStart, OnUpdate, OnStop and you're good to go!
Here's the example of custom event action from another project, that controls the behaviour of a camera:

```
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Platformer
{
	public class CameraTargetControl : EventAction
	{
		[SerializeField] private bool _turnOffTargetingAtStart;
		[SerializeField] private bool _turnOnTargetingAtEnd;
		[SerializeField] private bool _targetActorInstedOfPlayerAtStart;
		[SerializeField] private bool _targetPlayerInTheEnd;

		protected override void OnStart(float startTime)
		{
			if(_turnOffTargetingAtStart) GameManager.CameraController.SetTarget(null);
			else if (_targetActorInstedOfPlayerAtStart) GameManager.CameraController.SetTarget(_actor.transform);
		}

		protected override void OnStop()
		{
			if(_turnOnTargetingAtEnd || _targetPlayerInTheEnd) GameManager.CameraController.SetTarget(GameManager.PlayerController.transform);
		}


#if UNITY_EDITOR
		protected override void OnEditorGui()
		{
			_turnOffTargetingAtStart = EditorGUILayout.Toggle("Camera targeting off", _turnOffTargetingAtStart);

			if (_turnOffTargetingAtStart)
				_turnOnTargetingAtEnd = EditorGUILayout.Toggle("Targeting on in the end", _turnOnTargetingAtEnd);
			else
			{
				_turnOnTargetingAtEnd = false;
				_targetActorInstedOfPlayerAtStart = EditorGUILayout.Toggle("Target actor", _targetActorInstedOfPlayerAtStart);
				if (_targetActorInstedOfPlayerAtStart)
					_targetPlayerInTheEnd = EditorGUILayout.Toggle("Target player in the end", _targetPlayerInTheEnd);
			}
		}
#endif
	}
}
```

# Known issues
Scriptable objects (whic every EventAction is) is stored by Unity as a separate entity and right now there's no protection from unexpected issues like this one: if you create a copy of your Scenario object - both of them will share the same references to actions, so editing action in one scenario will affect another.