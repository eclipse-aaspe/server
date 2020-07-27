# AASX Server

AASX Server serves Industrie 4.0 AASX packages accessible by REST, OPC UA and MQTT protocols.

The AASX Server is based on code of AASX Package Explorer (https://github.com/admin-shell-io/aasx-package-explorer).

The binaries are available in the [Releases section](https://github.com/admin-shell-io/aasx-server/releases).  

# Build Container on Linux/MacOS

To run inside a Docker container on Linux/MacOS:
* Build the container with `src/buildContainer.sh`
* Run the container with `src/runContainer.sh`

You can then connect to the ports as ususal. 

# Build Container on Windows

For Windows, there is still no script to build the container. 
You can build the container manually by using the command line.

Build your container with:
```
cd src/
docker build -t aasxserver-img .
```

And run with:
```
docker run -d -p 51210:51210 -p 51310:51310 --name AasxServer aasxserver-img
```
