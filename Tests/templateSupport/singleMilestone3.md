As part of this release we had [16 commits](https://github.com/Particular/ServiceControl/compare/1.0.0-Beta3...1.0.0-Beta4) which resulted in [5 issues](https://github.com/Particular/ServiceControl/issues?milestone=7&state=closed) being closed.


## Features

### [#96 EndpointPlugin to support NServiceBus v3.x](https://github.com/Particular/ServiceControl/issues/96)

Requirements:

1. Supported features: Heartbeats and CustomChecks (no need for Saga state etc.)
* Minimum version supported: TBD, based on supportability limitations of earlier v3.x releases
* Deployment: as a separate DLL from the V4.x plugin 
   * (see https://github.com/Particular/Housekeeping/issues/100#issuecomment-27755301)

Related to https://github.com/Particular/Housekeeping/issues/100

<!---
@huboard:{"order":73.5}
-->




## Bugs

### [#122 Retry functionality broken in ServiceControl](https://github.com/Particular/ServiceControl/issues/122)

I'm using the ExceptionHandling sample, but can't get ServiceInsight to retry a failed message. We think (meaning, also @dannycohen) that it is a ServiceControl bug (or something wrong on my install) and that's why the bug is here.

Repro:
I start the ExceptionHandling from the sample, start in MyServer.WithoutSLR
I hit S so I will get a failed message. I then hit Continue (F5) in Visual Studio until I'm out of exceptions
I close the sample
I go to ServiceInsight, find the mssage, right click and try to retry it.
The message disappears from the view (expected)
I hit refresh (F5) and the message reappears, but still on a Failure status (not retrying)

The message looks like this on SC (response from http://localhost:33333/api/messages/52f50ddf-dbb2-4fd1-815c-a27b00f2c273-NewNoSLREndpoint. (note: I renamed the project to NewNoSLREndpoint to properly understand, but the same happens on the default name):
*Content trimmed. See [full issue](https://github.com/Particular/ServiceControl/issues/122)*

### [#110 particular.servicecontrol queue not created automatically during install](https://github.com/Particular/ServiceControl/issues/110)

Following user feedback. 


### [#90 Support more than one saga data per message](https://github.com/Particular/ServiceControl/issues/90)

Current saga headers assume that only one saga is associated with a message.
As a result, only one saga data can be represented in the existing saga-related headers (SagaType, SagaTypeId etc.)

This needs to change since more than one saga can be associated with a message.

Illustration of current Saga-related headers in SI:

![image](https://f.cloud.github.com/assets/3889023/1446428/fe3f959c-4236-11e3-9e99-1b55b0b6b405.png)




### [#86 Update "Management Pack" to "ServiceControl for NServiceBus"](https://github.com/Particular/ServiceControl/issues/86)

In the Modify window of the SC installer: 

![image](https://f.cloud.github.com/assets/3889023/1437032/ac3b4c4a-4165-11e3-9bbd-296f479b0469.png)




## Where to get it
You can download this release from:
- Our [website](http://particular.net/downloads)
- Or [nuget](https://www.nuget.org/profiles/nservicebus/)