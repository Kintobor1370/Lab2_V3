#pragma once

extern "C"  _declspec(dllexport)
void SplineBuild(int nx, int nsites, double* Scope, double* NodeArray, double* ValueArray, double* Der, double* Result);