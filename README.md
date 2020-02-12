Server of I40 .AASX packages accessable by REST, OPC UA and MQTT

AASX Server - based on code of AASX Package Explorer

Binary download with short explanation at
https://github.com/admin-shell/aasx-server

To run inside a Docker container on Linux/MacOS:
Build the container with buildContainer.sh
Run the container with runContainer.sh

You can then connect to the ports as ususal. For Windows, there is no script yet. You can do that manually by using the command line (for non Linux systems):
Build your container with:
docker build -t aasxserver-img .
And run with:
docker run -d -p 51210:51210 -p 51310:51310 --name AasxServer aasxserver-img
