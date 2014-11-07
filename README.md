IF3DLoggingProxy
================
# Description
IF3DLoggingProxy. Says it all.

## Changelog
### 1.0.3.1
FIXED: When an Exception happened during processing the webResponse, it wasn't disposed and the connection would stay open, causing all further requests to be stalled and timeing out.
Note that this happened for all requests resulting in a CHUNKED response. This still doesn't work (503 response) but won't break Proxy anymore.

### 1.0.3.0
Use log4net for per-handler (and general error) logging (gives syslog output option)

### 1.0.2.1
Fix: Only place long (>8) contentIds in subfolders; try-catch in migration scenario

### 1.0.2.0
Change: Place assets in folders based on prefix (STBTS\01\010a\010a49a8sd.log)
Change: Separate return codes for GET, POST/PUT and DELETE timeouts (must adapt config!)

### 1.0.1.3
Fix: Not logging entire <html> responses never got committed to source control and was gone. Built-in again.

### 1.0.1.2
* Fix: The per-day logfile feature never got committed to source control and was gone. Built-in again.
* Wrapped log writing in try/catch for improved reliability

### 1.0.1.1
Fix: Settings weren't used for handler #2.

### 1.0.1.0
* Log REQUEST time (plus duration), instead of RESPONSE time.
* Log duration

### 1.0.0.4
Made the response code to use on Timeout configurable.

### 1.0.0.3
Fix: Don't log NUL characters (use `ToArray()` instead of `GetBuffer()`).

### 1.0.0.2
Made timout a setting

### 1.0.0.1
Fix: On timeout (or other non-webresponse exception) respond 503 instead of 200). 
