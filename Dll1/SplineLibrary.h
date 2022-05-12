#pragma once

extern "C"  _declspec(dllexport)
void SplineBuild(int nx, double* Scope, double* NodeArray, double* ValueArray, double* Der, double* Result);