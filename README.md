# lm-connect-sdk

The LM Connect SDK from Full Swing Golf allows third party applications to connect to a Full Swing Golf Kit LM and be notified when new shot data is ready.

In order to connect you must first request an AccountId and AccountKey from Full Swing Golf.

In terms of units, here are how things are defined currently:
*Club Speed - mph 
*Ball Speed - mph 
*Smash Factor 
*Attack Angle - degrees 
*Club Path - degrees 
*Vertical Launch Angle - degrees 
*Horizontal Launch Angle - degrees (negative is left, positive is right) 
*Face Angle - degrees 
*Spin Rate - rpm 
*Spin Axis - degrees 
*Carry Distance - yards 
*Total Distance - yards 
*Side - yards (negative is left, positive is right) 
*Side Total - yards (negative is left, positive is right) 
*Apex - yards 

NOTES: If a value is null it means it could not be calculated.