#!/usr/bin/env bash
dotnet AasxServerCore.dll --rest --no-security --data-path ./aasxs --host 0.0.0.0
