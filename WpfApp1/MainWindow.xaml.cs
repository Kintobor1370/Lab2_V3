using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Legends;
using ClassLibrary1;

namespace WpfApp1
{
    //______________________________________________КЛАСС ДАННЫХ ДЛЯ ПОСТРОЕНИЯ ГРАФИКИ_______________________________________________
    public class Data
    {
        public string xTitle { get; set; }                                                          // Надпись по оси Ox
        public string yTitle { get; set; }                                                          // Надпись по оси Oy
        public double[] X { get; set; }                                                             // Значения абсцисс 
        public double[] Y { get; set; }
        public double[] Splines_X { get; set; }
        public List<double[]> Splines_Y_List { get; set; }
        public List<string> legends { get; set; }
        public ObservableCollection<string> strList { get; set; }

        public Data()
        {
            strList = new ObservableCollection<string>();
            legends = new List<string>();
            Splines_Y_List = new List<double[]>();
            xTitle = "x";
            yTitle = "F(x)";
        }

        public void AddMeasuredData(int nX, double[] XArray, double[] YArray)                                                             // nX - кол-во точек по оси Х
        {
            try
            {
                X = new double[nX];
                Y = new double[nX];
                for (int i = 0; i < nX; i++)
                {
                    X[i] = XArray[i];
                    Y[i] = YArray[i];
                }
                legends.Add("Измеренные данные");
            }
            catch (Exception ex)
            { MessageBox.Show("Ошибка в AddDefaults!\n " + ex.Message); }
        }

        public void AddSplinesData(int nx, double[] Scope, double[] YArray, bool first_der_set)
        {
            try
            {
                Splines_X = new double[nx];
                double[] Splines_Y = new double[nx];
                double step = (Scope[1] - Scope[0]) / (nx - 1);
                for (int i = 0; i < nx; i++)
                {
                    Splines_X[i] = Scope[0] + step*i;
                    Splines_Y[i] = YArray[i];
                }
                Splines_Y_List.Add(Splines_Y);
                string der_set = first_der_set ? "Первый" : "Второй";
                legends.Add("Значения сплайнов: " + der_set + " набор производных");
            }
            catch (Exception ex)
            { MessageBox.Show("Ошибка в AddSplinesData!\n " + ex.Message); }
        }
    }

    //____________________________КЛАСС ДЛЯ ВЗАИМОДЕЙСТВИЯ С ЭЛЕМЕНТАМИ УПРАВЛЕНИЯ ГРАФИКИ WPF (OxyPlot)______________________________
    public class ChartData
    {
        public PlotModel plotModel { get; private set; }

        public ChartData()
        {
            this.plotModel = new PlotModel { Title = "Measured Data & Splines Charts" };
        }

        public void AddSeries(Data data, string option)
        {
            Legend legend = new Legend();
            OxyColor color;
            if(option == "MD")
            {
                this.plotModel.Series.Clear();
                color = OxyColors.Green;

                LineSeries lineSeries = new LineSeries();
                for (int i = 0; i < data.X.Length; i++)
                    lineSeries.Points.Add(new DataPoint(data.X[i], data.Y[i]));

                lineSeries.MarkerType = MarkerType.Circle;
                lineSeries.MarkerSize = 4;
                lineSeries.Color = color;
                lineSeries.MarkerStroke = color;
                lineSeries.MarkerFill = color;
                lineSeries.Title = data.legends[0];

                plotModel.Legends.Add(legend);
                this.plotModel.Series.Add(lineSeries);
            }
            
            else if(option == "SD")
            {
                for (int i = 0; i < data.Splines_Y_List.Count; i++)
                {
                    color = i == 0 ? OxyColors.Blue : OxyColors.Orange;
                    LineSeries SplineSeries = new LineSeries
                    { InterpolationAlgorithm = InterpolationAlgorithms.CanonicalSpline };
                    for (int j = 0; j < data.Splines_X.Length; j++)
                        SplineSeries.Points.Add(new DataPoint(data.Splines_X[j], data.Splines_Y_List[i][j]));


                    SplineSeries.MarkerType = MarkerType.Circle;
                    SplineSeries.MarkerSize = 4;
                    SplineSeries.Color = color;
                    SplineSeries.MarkerStroke = color;
                    SplineSeries.MarkerFill = color;
                    SplineSeries.Title = data.legends[i+1];
                    
                    //plotModel.Legends.Add(legend);
                    this.plotModel.Series.Add(SplineSeries);
                }
            }
        }
    }

    public class ViewData : INotifyPropertyChanged, IDataErrorInfo
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public SplineParameters SplineParameters { get; set; }
        public SplinesData SplinesData { get; set; }
        public ChartData ChartData { get; set; }
        public int NonUniformNum
        {
            get
            { return SplinesData.Data.Num; }
            set
            {
                SplinesData.Data.Num = value;
                OnPropertyChanged("SplinesData.Data");
            }
        }
        public int UniformNum
        {
            get
            { return SplinesData.Parameters.Num; }
            set
            {
                SplinesData.Parameters.Num = value;
                OnPropertyChanged("SplinesData.Parameters");
            }
        }
        public double[] Scope
        {
            get
            { return SplinesData.Data.Scope; }
            set
            {
                SplinesData.Data.Scope[0] = value[0];
                SplinesData.Data.Scope[1] = value[1];
                OnPropertyChanged("SplinesData.Data");
            }
        }
        public ObservableCollection<string> MeasuredDataCollection { get; set; }
        public ObservableCollection<string> SplinesDataCollection { get; set; }

        public ViewData(SplinesData sd, SplineParameters sp)
        {
            this.SplinesData = sd;
            this.SplineParameters = sp;
            this.MeasuredDataCollection = new ObservableCollection<string>();
            this.SplinesDataCollection = new ObservableCollection<string>();

            MeasuredDataCollection.CollectionChanged += MDCollection_Changed;
            SplinesDataCollection.CollectionChanged += SDCollection_Changed;
        }

        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //....................Коллекция узлов неравномерной сетки и значений функции в них (для вывода в ListBox)....................
        public void CreateMDCollection()
        {
            MeasuredDataCollection.Clear();
            for (int i = 0; i < NonUniformNum; i++)
                MeasuredDataCollection.Add("x = " + SplinesData.Data.NodeArray[i].ToString() +
                                 ";\nF(x) = " + SplinesData.Data.ValueArray[i].ToString());
        }
        void MDCollection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("MeasuredDataCollection"); }

        //...........................Коллекция значений сплайнов и первыхпроизводных (для вывода в ListBox)..........................
        public void CreateSDCollection()
        {
            SplinesDataCollection.Clear();

            SplinesDataCollection.Add("Первый набор производных:");
            SplinesDataCollection.Add("F'(a) = " + SplinesData.Parameters.Derivative1[0].ToString() +
                                 ";    F'(b) = " + SplinesData.Parameters.Derivative1[1].ToString());
            SplinesDataCollection.Add("F(a) = " + SplinesData.Spline1ValueArray[0].ToString() +
                                 ";    F'(a) = " + SplinesData.Spline1DerivativeArray[0].ToString());
            SplinesDataCollection.Add("F(a+h) = " + SplinesData.Spline1ValueArray[1].ToString() +
                                 ";    F'(a+h) = " + SplinesData.Spline1DerivativeArray[1].ToString());
            SplinesDataCollection.Add("F(b-h) = " + SplinesData.Spline1ValueArray[UniformNum-2].ToString() +
                                 ";    F'(b-h) = " + SplinesData.Spline1DerivativeArray[UniformNum-2].ToString());
            SplinesDataCollection.Add("F(b) = " + SplinesData.Spline1ValueArray[UniformNum-1].ToString() +
                                 ";    F'(b) = " + SplinesData.Spline1DerivativeArray[UniformNum-1].ToString());

            SplinesDataCollection.Add("");
            
            SplinesDataCollection.Add("Второй набор производных:");
            SplinesDataCollection.Add("F'(a) = " + SplinesData.Parameters.Derivative2[0].ToString() +
                                 ";    F'(b) = " + SplinesData.Parameters.Derivative2[1].ToString());
            SplinesDataCollection.Add("F(a) = " + SplinesData.Spline2ValueArray[0].ToString() +
                                 ";    F'(a) = " + SplinesData.Spline2DerivativeArray[0].ToString());
            SplinesDataCollection.Add("F(a+h) = " + SplinesData.Spline2ValueArray[1].ToString() +
                                 ";    F'(a+h) = " + SplinesData.Spline2DerivativeArray[1].ToString());
            SplinesDataCollection.Add("F(b-h) = " + SplinesData.Spline2ValueArray[UniformNum - 2].ToString() +
                                 ";    F'(b-h) = " + SplinesData.Spline2DerivativeArray[UniformNum - 2].ToString());
            SplinesDataCollection.Add("F(b) = " + SplinesData.Spline2ValueArray[UniformNum - 1].ToString() +
                                 ";    F'(b) = " + SplinesData.Spline2DerivativeArray[UniformNum - 1].ToString());
        }
        void SDCollection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("SplinesDataCollection"); }

        //........................................Реализация интерфейса IDataErrorInfo...............................................
        public string Error { get; }
        public string this[string PropertyName] { get { return GetValidationError(PropertyName); } }
        private string GetValidationError(string PropertyName)
        {
            string err_message = String.Empty;
            switch (PropertyName)
            {
                case "NonUniformNum":
                    if (NonUniformNum <= 2)
                        err_message = "Число узлов должно быть больше 2";
                    break;
                case "Scope":
                    if (Scope[0] >= Scope[1])
                        err_message = "Левый конец отрезка не может быть больше правого";
                    break;
                case "UniformNum":
                    if (UniformNum <= 2)
                        err_message = "Число узлов должно быть больше 2";
                    break;
                default:
                    err_message = String.Empty;
                    break;
            }
            return err_message;
        }
    }

    //__________________________________________________ГЛАВНОЕ ОКНО ПРИЛОЖЕНИЯ WPF___________________________________________________
    public partial class MainWindow : Window
    {
        public ViewData ViewData;
        public Data chart_data;

        public MainWindow()
        {
            InitializeComponent();

            MeasuredData MeasuredData = new MeasuredData();
            SplineParameters SplineParameters = new SplineParameters();
            SplinesData SplinesData = new SplinesData(MeasuredData, SplineParameters);
            ViewData = new ViewData(SplinesData, SplineParameters);

            this.DataContext = ViewData;
            SPfBox.ItemsSource = Enum.GetValues(typeof(SPf));
            MeasuredDataList.ItemsSource = ViewData.MeasuredDataCollection;
        }

        public static RoutedCommand MakeMD = new RoutedCommand("MakeMD", typeof(WpfApp1.MainWindow));
        private void CanMakeMDHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            TextBox[] ValidationElements = { NonUniformNumBox, ScopeBox };
            foreach (TextBox ValidationElement in ValidationElements)
            {
                if (ValidationElement == null)
                    continue;
                if (Validation.GetHasError(ValidationElement) == true)
                {
                    e.CanExecute = false;
                    return;
                }
                e.CanExecute = true;
            }
        }
        private void MakeMDHandler(object sender, ExecutedRoutedEventArgs e)
        {
            ViewData.CreateMDCollection();
            chart_data = new Data();
            chart_data.AddMeasuredData(ViewData.NonUniformNum, ViewData.SplinesData.Data.NodeArray, ViewData.SplinesData.Data.ValueArray);

            ViewData.ChartData = new ChartData();
            ViewData.ChartData.AddSeries(chart_data, "MD");
            GridOxyPlot.DataContext = ViewData.ChartData;
        }

        public static RoutedCommand MakeSD = new RoutedCommand("MakeSD", typeof(WpfApp1.MainWindow));
        private void CanMakeSDHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            TextBox ValidationElement = UniformNumBox;
            if (ValidationElement == null)
                return;
            if (Validation.GetHasError(ValidationElement) == true || ViewData.MeasuredDataCollection.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }
        private void MakeSDHandler(object sender, ExecutedRoutedEventArgs e)
        {
            ViewData.CreateSDCollection();

            if (chart_data.Splines_Y_List.Count == 0)
            {
                chart_data.AddSplinesData(ViewData.UniformNum, ViewData.Scope, ViewData.SplinesData.Spline1ValueArray, true);
                chart_data.AddSplinesData(ViewData.UniformNum, ViewData.Scope, ViewData.SplinesData.Spline2ValueArray, false);
                ViewData.ChartData.AddSeries(chart_data, "SD");
            }
            GridOxyPlot.DataContext = ViewData.ChartData;
        }
    }

    //__________________________________________________________КОНВЕРТЕРЫ____________________________________________________________
    public class IntConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string Num = value.ToString();
                return Num;
            }
            catch
            { return "INT_EX"; }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object obj = null;
            try
            {
                string str = value as string;
                int Num;
                Int32.TryParse(str, out Num);

                if (Num == 0)
                { Exception EX = new Exception(); throw (EX); }

                obj = Num;
                return obj;
            }
            catch
            { return obj; }
        }
    }

    public class ScopeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string Min = values[0].ToString();
                string Max = values[1].ToString();
                return Min + ";" + Max;
            }
            catch
            { return "SCOPE_EX"; }
        }
        public object[] ConvertBack(object values, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object[] obj = new object[2];
            try
            {
                string str_vals = values as string;
                double Min, Max;
                string[] vals = str_vals.Split(';', StringSplitOptions.RemoveEmptyEntries);
                double.TryParse(vals[0], out Min);
                double.TryParse(vals[1], out Max);

                //if (Min > Max)
                //{ Exception EX = new Exception(); throw (EX); }

                obj[0] = Min;
                obj[1] = Max;
                return obj;
            }
            catch
            { return obj; }
        }
    }

    public class DerivativeConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string LeftDer = values[0].ToString();
                string RightDer = values[1].ToString();
                return LeftDer + ";" + RightDer;
            }
            catch
            { return "DER_EX"; }
        }
        public object[] ConvertBack(object values, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            object[] obj = new object[2];
            try
            {
                string str_vals = values as string;
                string[] vals = str_vals.Split(';', StringSplitOptions.RemoveEmptyEntries);
                double LeftDer = double.Parse(vals[0]);
                double RightDer = double.Parse(vals[1]);

                obj[0] = LeftDer;
                obj[1] = RightDer;
                return obj;
            }
            catch
            { return obj; }
        }
    }

    public class DoubleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                double val = (double)value;
                return val.ToString();
            }
            catch
            { return "DOUBLE_EX"; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { throw new NotImplementedException(); }
    }

    public class SPfConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                string str = "Выбрано:\n";
                switch ((int)value)
                {
                    case 0:
                        str += "Кубический многочлен y = x^3 + 3x^2 - 6x - 18";
                        break;
                    case 1:
                        str += "Экспонента";
                        break;
                    case 2:
                        str += "Генератор псевдослучайных чисел";
                        break;
                    default: break;
                }
                return str;
            }
            catch
            { return "BOOL_EX"; }
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        { throw new NotImplementedException(); }
    }
}
