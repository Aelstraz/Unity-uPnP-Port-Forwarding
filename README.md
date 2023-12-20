## C#/Unity uPnP Port Forwarding (NAT)

Uses the [Open.NAT](https://github.com/lontivero/Open.NAT) (based on [Mono.NAT](https://github.com/alanmcgovern/Mono.Nat)) library to automatically forward ports on the users router via the Universal Plug And Play (uPnP) protocol. The most common usage is to allow users to host their own peer-to-peer servers online, without the need to manually forward their ports according to the games network settings. Note that your router must support uPnP and have it enabled in order for this to work, otherwise the user will still have to manually forward their ports. It is recommended that you check out the included example scene and example script to learn more about usage. Tested on Win & Mac.

### IMPORTING/INSTALLING:
Simply add the entire folder into your Assets folder.

### USAGE:
The uPnP Helper Class is a static self-contained class that will provide all the methods/functions required. The uPnP Helper Class also has internal documentation. Since the class is static, you do not have to create an instance of the class or attach the script to a GameObject. Below are examples of code and explanations:
***
	uPnPHelper.LogErrors = true;
Optional. Sets error logging mode. Error logging mode writes any critical issues into the error log during port forwarding. Useful for testing, although not really recommended to be active when releasing your software. It must be enabled BEFORE starting port forwarding in order to function.
***
	uPnPHelper.DebugMode = true;
Optional. Sets debug mode. Debug mode writes detailed information into the debug log during port forwarding. Useful for testing, although not really recommended to be active when releasing your software. It must be enabled BEFORE starting port forwarding in order to function.
***
	uPnPHelper.Start("UDP", 8193, "Unity uPnP Port Forward Test.");
This is the main method that will map the desired port number on your local router. It runs runs asynchronously. It takes a string, integer and string. The first variable is the protocol that you wish to use. ("UDP" and "TCP" are the only two recognised protocols). The second variable is the desired port number that you wish to map, while the third variable is the lifetime of the port map (0 = infinity), finally the fourth variable is the mapping description. The mapping description is shown by routers so you know what port mapping is for what. The method will first check for any existing mappings, just in case the desired mapping has already been created. Errors and debug messages are logged if log errors and debug mode are on (see above methods).
***
	uPnPHelper.CloseAll();
Optional. Removes any port mappings created during the session and closes listening sockets. It is recommended to use this method as a cleanup when closing the server.
***
	uPnPHelper.GetNATType();
Optional. Returns a string with the status of the port mapping. This can either be used to update the applications user about the status of the port forwarding, or can even be used to line up other methods based on the status of the port mapping.
***
	uPnPHelper.GetLastErrorMessage();
	uPnPHelper.GetLastDebugMessage();
Optional. Returns the last error/debug message generated in session.
***
	uPnPHelper.GetErrorMessageArray();
	uPnPHelper.GetDebugMessageArray();
Optional. Returns all error/debug messages generated in session as an array.
***
	uPnPHelper.GetErrorMessages();
	uPnPHelper.GetDebugMessages();
Optional. Returns all error/debug messages generated in session as a single formatted string.
***
### Twitter: [@aelstraz](https://twitter.com/Aelstraz)
