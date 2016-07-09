# Expression Serializer 

| CI | Status |
| --- | --- |
| travis.ci | [![Build Status](https://travis-ci.org/PatriotBob/expression-serializer.svg?branch=master)](https://travis-ci.org/PatriotBob/expression-serializer) |
| appveyor | [![Build Status](https://ci.appveyor.com/api/projects/status/m8v32lpgsufsguvg?svg=true)](https://ci.appveyor.com/api/projects/status/m8v32lpgsufsguvg?svg=true) |

The main goal of this project is to allow lambda expressions to be serialized and deserialized from json.

## What is done

Basic serializing and deserlizing expresssions.
* Most binary/unary operators.
* Member Access
* Functions (public/static and System.Linq.Enumerable extension methods)
* Conditionals

## What is left to be done.

* Nested lambdas.
* Probably a bunch more.