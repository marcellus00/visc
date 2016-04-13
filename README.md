# visc
Extendable visual scenario editor for Unity

visc is an easy and customisable tool for creation of time-based rich and action packed scenarios for your Unity game, allowing you to control and modify events, actions and actor.

![](https://github.com/marcellus00/visc/blob/master/screenshots/timeline.png?raw=true)

Basically its just a timeline edtior with custom actions, current version contains basic ones like 'move an object from point to point' or 'trigger an animation event', but you can easily create your own, like 'make character sing', 'center camera on stage' or 'do a barrel roll'

![](https://github.com/marcellus00/visc/blob/master/screenshots/eventactioneditor.png?raw=true)

Extend the class "EventAction", override methods OnEditorGui, OnStart, OnUpdate, OnStop and you're good to go!

Created for use in Unity 5. 