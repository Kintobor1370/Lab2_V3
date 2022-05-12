#include "pch.h"
#include "framework.h"
#include "SplineLibrary.h"
#include "mkl.h"
#include "mkl_vsl.h"
#include "mkl_df_types.h"

extern "C"  _declspec(dllexport)
void SplineBuild(int nx, double* Scope, double* NodeArray, double* ValueArray, double* Der, double* Result)
{
	int ny = 1;
	int df_check;
	DFTaskPtr* Task = new DFTaskPtr;
	df_check = dfdNewTask1D(Task, nx, NodeArray, DF_SORTED_DATA, ny, ValueArray, DF_MATRIX_STORAGE_ROWS);

	double* scoeff = new double[ny * DF_PP_CUBIC * (nx - 1)];
	df_check = dfdEditPPSpline1D(*Task, DF_PP_CUBIC, DF_PP_NATURAL, DF_BC_1ST_LEFT_DER | DF_BC_1ST_RIGHT_DER, Der, DF_NO_IC, NULL, scoeff, DF_NO_HINT);

	df_check = dfdConstruct1D(*Task, DF_PP_SPLINE, DF_METHOD_STD);

	int ndorder = 1;
	int* dorder = new int[ndorder] {1};
	df_check = dfdInterpolate1D(*Task, DF_INTERP, DF_METHOD_PP, nx, Scope, DF_UNIFORM_PARTITION, ndorder, dorder, NULL, Result, DF_MATRIX_STORAGE_ROWS, NULL);

	df_check = dfDeleteTask(Task);
}