Time Control
============

By default, `Crest` uses the current game time given by `Time.time` when simulating and rendering the water.
In some situations it is useful to control this time, such as an in-game pause or to synchronise wave conditions over a network.
This is achieved through what we call *TimeProviders*, and a few use cases are described below.

.. note::
   The :ref:`dynamic-waves-section` simulation must progress frame by frame and can not be set to use a specific time, and also cannot be synchronised accurately over a network.


Supporting Pause
----------------

One way to pause time is to set `Time.timeScale` to 0.
In many cases it is desirable to leave `Time.timeScale` untouched so that animations continue to play, and instead pause only the water.
To achieve this, attach a *TimeProviderCustom* component to a GameObject and assign it to the *Time Provider* parameter on the *OceanRenderer* component.
Then time can be paused by setting the *_paused* variable on the *TimeProviderCustom* component to *false*.

The *TimeProviderCustom* also allows driving any time to the system which may give more flexibility for specific use cases.

A final alternative option is to create a new class that implements the *ITimeProvider* interface and call *OceanRenderer.Instance.PushTimeProvider()* to apply it to the system.


Network Synchronisation
-----------------------

A requirement in networked games is to have a common sense of time across all clients.
This can be specified using an offset between the clients `Time.time` and that of a server.

This is supported by attaching a *TimeProviderNetworked.cs* component to a GameObject, assigning it to the *Time Provider* parameter on the *OceanRenderer* component, and at run-time setting *TimeProviderNetworked.TimeOffsetToServer* to the time difference between the client and the server.

If using the :link:`Mirror <https://assetstore.unity.com/packages/tools/network/mirror-129321?aid=1011lic2K>` network system, set this property to the :link:`network time offset <https://mirror-networking.com/docs/api/Mirror.NetworkTime.html#Mirror_NetworkTime_offset>`.


Timelines and cutscenes
-----------------------

One use case for this is for cutscenes/timelines when the waves conditions must be known in advance and repeatable.
For this case you may attach a *Cutscene Time Provider* component to a GameObject and assign it to the *Ocean Renderer* component.
This component will take the time from a `Playable Director` component which plays a cutscene `Timeline`.
Alternatively, a *Time Provider Custom* component can be used to feed any time into the system, and this time value can be keyframed, giving complete control over timing.
