#include "pch.h"
#include "framework.h"
#include "SplineLibrary.h"
#include "mkl.h"
#include "mkl_vsl.h"
#include "mkl_df_types.h"

extern "C"  _declspec(dllexport)
void SplineBuild(int nx, int nsites, double* Scope, double* NodeArray, double* ValueArray, double* Der, double* Result)
{
	int ny = 1;
	int df_check;
	DFTaskPtr Task = new DFTaskPtr;

	df_check = dfdNewTask1D(&Task, nx, NodeArray, DF_NON_UNIFORM_PARTITION, ny, ValueArray, DF_MATRIX_STORAGE_ROWS);

	double* scoeff = new double[ny * DF_PP_CUBIC * (nx - 1)];
	df_check = dfdEditPPSpline1D(Task, DF_PP_CUBIC, DF_PP_NATURAL, DF_BC_1ST_LEFT_DER | DF_BC_1ST_RIGHT_DER, Der, DF_NO_IC, NULL, scoeff, DF_NO_HINT); // здесь

	df_check = dfdConstruct1D(Task, DF_PP_SPLINE, DF_METHOD_STD);

	int ndorder = 2;
	int* dorder = new int[ndorder] { 1, 1 };
	double* res = new double[nsites * ny * ndorder];
	df_check = dfdInterpolate1D(Task, DF_INTERP, DF_METHOD_PP, nsites, Scope, DF_UNIFORM_PARTITION, ndorder, dorder, NULL, Result, DF_MATRIX_STORAGE_ROWS, NULL);

	df_check = dfDeleteTask(&Task);

	for (int i = 0; i < nsites * ny * ndorder; i++)
		Result[i] = res[i];
}