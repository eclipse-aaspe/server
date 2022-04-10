#!/usr/bin/env bash
dotnet AasxServerBlazor.dll --rest --no-security --data-path ./aasxs --host 0.0.0.0 $OPTIONSAASXSERVER
