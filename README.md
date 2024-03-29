# lm-connect-sdk

## Overview

The LM Connect SDK from Full Swing Golf allows third party applications to connect to a Full Swing Golf Kit LM and be notified when new shot data is ready.

In order to connect you must first request an AccountId and AccountKey from Full Swing Golf.

The following data is provided per shot:

* Club Speed - mph 
* Ball Speed - mph 
* Smash Factor 
* Attack Angle - degrees 
* Club Path - degrees 
* Vertical Launch Angle - degrees 
* Horizontal Launch Angle - degrees (negative is left, positive is right) 
* Face Angle - degrees 
* Spin Rate - rpm 
* Spin Axis - degrees 
* Carry Distance - yards 
* Total Distance - yards 
* Side - yards (negative is left, positive is right) 
* Side Total - yards (negative is left, positive is right) 
* Apex - yards 

NOTES: If a value is null it means it could not be calculated.

## Full Documentation

Full documenation is available at the following link:

[Full documentation](https://fsglm.z19.web.core.windows.net/sdk-api-documentation/)

## Release Notes

### 1.8.0-alpha5

* Updated to 1.8.0-alpha5 version of SDK
* Example code for reconnecting to the LM on a disconnect event

### 1.8.0-alpha4

* Updated to 1.8.0-alpha4 version of SDK
* Example code for setting power state of the LM

### 1.8.0-alpha1

* Updated to 1.8.0-alpha1 version of SDK
* Example code for setting play mode to support short-shot (chipping) mode
  
### 1.7.2

* Updated to 1.7.2 version of SDK
* Example code added for pulling shot videos
* Example code added for pulling shot point clouds
* Example code added for seeing if a value was measured or predicted
* Example code added for storing credentials in user secrets in .NET Core project
* Support added for selecting an LM when more than 9 are discovered
* Added support for normalized shot results

### 1.6.0-alpha1

* Updated to 1.6.0-alpha1 version of SDK

### 1.5.0-alpha1

* Updated to 1.5.0-alpha1 version of SDK
* Example code added for seeing when shot video was available

### 1.4.0-alpha1

* Updated to 1.4.0-alpha1 version of SDK
* Example code added for setting club type and other session based parameters

### 1.3.0-alpha2

* Updated to 1.3.0-alpha2 version of SDK
* Miscellaneous bug fixes
