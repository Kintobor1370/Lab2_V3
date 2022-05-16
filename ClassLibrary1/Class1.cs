using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClassLibrary1
{
    public enum SPf
    {
        CubPol,
        Exp,
        Random
    }

    //___________________________________________________ИЗМЕРЕННЫЕ ДАННЫЕ_______________________________________________________
    public class MeasuredData
    {
        public int Num { get; set; }                                                            // Число узлов неравномерной сетки
        public double[] Scope { get; set; }                                                     // Массив концов отрезка [a,b]
        public SPf Func { get; set; }
        public double[] NodeArray                                                               // Узлы неравномерной сетки
        {
            get
            {
                if (node_ar == null || node_ar.Length != Num || node_ar[0] != Scope[0] || node_ar[node_ar.Length-1] != Scope[1])
                    RandomNodesGenerate();
                return node_ar;
            }
        }
        public double[] ValueArray                                                              // Массив значений в узлах неравномерной сетки
        {
            get
            {
                double[] res = new double[Num];
                switch (Func)
                {
                    case SPf.CubPol:                                                            // Кубический многочлен вида: y = x^3 + 3x^2 - 6x - 18
                        for (int i = 0; i < Num; i++)
                            res[i] = Math.Pow(NodeArray[i], 3) + 3 * Math.Pow(NodeArray[i], 2) - 6 * NodeArray[i] - 18;
                        break;

                    case SPf.Exp:                                                               // Экспонента
                        for (int i = 0; i < Num; i++)
                            res[i] = Math.Exp(NodeArray[i]);
                        break;

                    case SPf.Random:                                                            // Генератор псевдослучайных чисел Random
                        Random Gen = new Random();
                        for (int i = 0; i < Num; i++)
                        {
                            double a = Gen.Next();                                              // Целая часть
                            double b = Gen.NextDouble();                                        // Дробная часть
                            res[i] = a + b;
                        }
                        break;
                }
                return res;
            }
        }

        public MeasuredData(int n = 2, double min = 0, double max = 0, SPf f = SPf.Random)
        {
            Num = n;
            Scope = new double[2];
            Scope[0] = min;
            Scope[1] = max;
            Func = f;
            node_ar = null;
        }

        private double[] node_ar { get; set; }                                             // Массив для хранения узлов неравномерной сетки, к которому можно получить доступ только через NodeArray
        void RandomNodesGenerate()                                                         // Генерация узлов неравномерной стеик
        {
            node_ar = new double[Num];
            Random Gen = new Random();
            node_ar[0] = Scope[0];
            node_ar[1] = Scope[1];
            for (int i = 2; i < Num; i++)
            {
                double next_node = Scope[0] + (Scope[1] - Scope[0]) * Gen.NextDouble();     // NextDouble() генерирует псевдослучайное число типа double в диапазоне [0,1]
                for (int j = 0; j < i; j++)
                    while (next_node == node_ar[j])
                    {
                        next_node = Scope[0] + (Scope[1] - Scope[0]) * Gen.NextDouble();
                        j = 0;
                    }
                node_ar[i] = next_node;
            }
            Array.Sort(node_ar);
        }
    }

    //_______________________________________________ДАННЫЕ ДЛЯ СОЗДАНИЯ СПЛАЙНОВ____________________________________________________
    public class SplineParameters
    {
        public int Num { get; set; }                                                            // Число узлов равномерной сетки
        public double[] Scope { get; set; }                                                     // Массив концов отрезка [a,b]
        public double[] NodeArray                                                               // Массив узлов равномерной сетки
        {
            get
            {
                double[] res = new double[Num];
                double step = (Scope[1] - Scope[0]) / (Num - 1);
                for (int i = 0; i < Num; i++)
                    res[i] = Scope[0] + i * step;
                return res;
            }
        }
        public double[] Derivative1 { get; set; }                                               // Значения первой производной на концах отрезка для 1-го сплайна
        public double[] Derivative2 { get; set; }                                               // Значения первой производной на концах отрезка для 2-го сплайна

        public SplineParameters(int n = 2, double min = 0, double max = 0, double d1_left = 1, double d1_right = 1, double d2_left = 0, double d2_right = 0)
        {
            Num = n;
            Scope = new double[2];
            Scope[0] = min;
            Scope[1] = max;

            Derivative1 = new double[2];
            Derivative1[0] = d1_left;
            Derivative1[1] = d1_right;

            Derivative2 = new double[2];
            Derivative2[0] = d2_left;
            Derivative2[1] = d2_right;
        }
    }

    //_____________________________________________________ДАННЫЕ СПЛАЙНОВ___________________________________________________________
    public class SplinesData
    {
        [DllImport("C:\\Users\\User\\Desktop\\prog\\C#\\Sem6\\Lab2_V3\\x64\\Debug\\Dll1.dll")]  // !! Надо изменить абсолютный путь!!
        static extern void SplineBuild(int nx, int nsites, double[] Scope, double[] NodeArray, double[] ValueArray, double[] Der, double[] Result);
        public MeasuredData Data { get; set; }
        public SplineParameters Parameters { get; set; }
        public double[] NodeArray { get { return Parameters.NodeArray; } }
        private double[] SplineInterpolationResult1 { get; set; }                               // Массив результатов сплайн интерполяции с первым граничным условием
        private double[] SplineInterpolationResult2 { get; set; }                               // Массив результатов сплайн интерполяции с первым граничным условием

        public SplinesData(MeasuredData md, SplineParameters sp)
        {
            Data = new MeasuredData(md.Num, md.Scope[0], md.Scope[1], md.Func);
            Parameters = new SplineParameters(sp.Num, sp.Scope[0], sp.Scope[1],
                                              sp.Derivative1[0], sp.Derivative1[1],
                                              sp.Derivative2[0], sp.Derivative2[1]);
        }

        public void BuildSpline()
        {
            try
            {
                SplineInterpolationResult1 = new double[Parameters.Num * 2];
                SplineInterpolationResult2 = new double[Parameters.Num * 2];

                SplineBuild(Data.Num, Parameters.Num, Data.Scope, Data.NodeArray, Data.ValueArray,
                            Parameters.Derivative1, SplineInterpolationResult1);
                SplineBuild(Data.Num, Parameters.Num, Data.Scope, Data.NodeArray, Data.ValueArray,
                            Parameters.Derivative2, SplineInterpolationResult2);
            }
            catch (Exception ex)
            { throw new Exception("Error in SplinesData.BuildSpline()", ex); }
        }

        public double[] Spline1ValueArray
        {
            get
            {
                double[] res = new double[Parameters.Num];
                for(int i = 0; i < Parameters.Num; i++)
                    res[i] = SplineInterpolationResult1[i * 2];
                return res;
            }
        }
        public double[] Spline2ValueArray
        {
            get
            {
                double[] res = new double[Parameters.Num];
                for (int i = 0; i < Parameters.Num; i++)
                    res[i] = SplineInterpolationResult2[i * 2];
                return res;
            }
        }
        public double[] Spline1DerivativeArray
        {
            get
            {
                double[] res = new double[Parameters.Num];
                for (int i = 0; i < Parameters.Num; i++)
                    res[i] = SplineInterpolationResult1[i * 2 + 1];
                return res;
            }
        }
        public double[] Spline2DerivativeArray
        {
            get
            {
                double[] res = new double[Parameters.Num];
                for (int i = 0; i < Parameters.Num; i++)
                    res[i] = SplineInterpolationResult2[i * 2 + 1];
                return res;
            }
        }
    }
}