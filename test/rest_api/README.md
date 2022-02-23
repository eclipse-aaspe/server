# API Tests with Postman/Newman

Here you will find the most important resources to run extensive API tests and thus check the server-side implementation.

[Postman](https://www.postman.com/) is a widely used tool for automated API testing. Postman allows us to run different test scenarios and validate the results via scripts, or you can just manage API requests for development and/or explorative testing. Postman has many features, please check the documentation.

[Newman](https://github.com/postmanlabs/newman) is a CLI runner for Postman and can be executed headless in Docker containers or github actions.

## System under Test
It should be obvious that you have to build and run the AAS API Server you want to test. Some Tests also requires predefined test data (e.g. as AASX Package). [Here](data) you find some predefined AASX's for testing. 


## Running and Managing Tests
### Postman Collections
A Postman collection is like a folder where you can manage your API requests. But you can also define variables and reuse these within your collection

Inside the [postman folder](postman) you find some json-files ending with `*.json.postman_collection`. You can import them in your postman application or use them with newman to run the collection.

### Testing your API with Newman and get a Report
Newman already provides [docker images](https://hub.docker.com/r/postman/newman) which you can reuse. Important Notes:
1. Use `-v $PWD/postman:/etc/newman` to make all needed jsons available within the docker container. 
2. Use `--environment="DockerEnvironment.json.postman_environment"` to set important variables like baseUrl of your system under test.
E.g. in [DockerEnvironment](postman/DockerEnvironment.json.postman_environment), the baseUrl is set to http://host.docker.internal:5001, expecting your system running on your docker host listening on port 5001.
3. Use `--reporters cli,json --reporter-json-export report.json` if you like to get a report in json format. The file gets saved in the volume you mounted to /etc/newman.

A full example might look like: `docker run -v $PWD/postman:/etc/newman -t --rm postman/newman:5.3.1-alpine run SubmodelElementTest.json.postman_collection --environment="DockerEnvironment.json.postman_environment" --reporters cli,json --reporter-json-export report.json`

#### Getting a HTML Report
If you like to get a nice, human-readable HTML report, consider building and using [this](docker/Dockerfile) Docker-Image. It uses the newman image as base and installs a HTML reporter to the image.

##### Build Image
`docker build --tag postman/newman/html --file .\docker\Dockerfile .`

##### Run Container
run the container as previously but add htmlextra as reporter
`docker run -v $PWD/postman:/etc/newman -t --rm postman/newman/html run SubmodelElementTest.json.postman_collection --environment="DockerEnvironment.json.postman_environment" --reporters cli,htmlextra --reporter-htmlextra-export report.html`
After running, you can open report.html with your browser.