#!/bin/sh

WORKSPACE="$(pwd)"
for dir in src; do
    [ -d "${dir}" ] || continue
    cd "${dir}"
    dotnet restore
done
cd "${WORKSPACE}"
for dir in test; do
    [ -d "${dir}" ] || continue
    cd "${dir}"
    dotnet restore
done
cd "${WORKSPACE}"