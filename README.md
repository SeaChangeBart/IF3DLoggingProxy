IF3DLoggingProxy
================

IF3DLoggingProxy

Changelog
=========
1.0.1.0
-------
* Log REQUEST time (plus duration), instead of RESPONSE time.
* Log duration

1.0.0.4
-------
Made the response code to use on Timeout configurable.

1.0.0.3
-------
Fix: Don't log NUL characters (use `ToArray()` instead of `GetBuffer()`).

1.0.0.2
-------
Made timout a setting

1.0.0.1
-------
Fix: On timeout (or other non-webresponse exception) respond 503 instead of 200). 
