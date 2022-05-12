using System;
using System.IO;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Controls;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Legends;
using ClassLibrary1;

namespace WpfApp1
{
    public class Data
    {
        public string xTitle { get; set; }                                                          // Надпись по оси Ox
        public string yTitle { get; set; }                                                          // Надпись по оси Oy
        public double[] X { get; set; }                                                             // Значения абсцисс 
        public List<double[]> YL { get; set; }                                                      // Значения ординат 

        public List<string> legends { get; set; }                                                   // Legends 
        public ObservableCollection<string> strList { get; set; }

        public Data()
        {
            strList = new ObservableCollection<string>();
            legends = new List<string>();
            YL = new List<double[]>();
            xTitle = "x";
            yTitle = "f(x)";
        }

        public void AddDefaults(int nX, double[] XArray, double[] YArray)                                                             // nX - кол-во точек по оси Х
        {
            try
            {
                X = new double[nX];
                double[] Y = new double[nX];
                for (int i = 0; i < nX; i++)
                {
                    X[i] = XArray[i];
                    Y[i] = YArray[i];
                }
                YL.Add(Y);
                legends.Add("Измеренные данные");
                //DataToStringList();
            }
            catch (Exception ex)
            { MessageBox.Show("Ошибка в AddDefaults\n " + ex.Message); }
        }

        void DataToStringList()
        {
            for (int i = 0; i < YL.Count; i++)
            {
                strList.Add(legends[i]);
                for (int j = 0; j < X.Length; j++)
                {
                    strList.Add($"x = {X[j].ToString("F2")} Y = {YL[i][j].ToString("F4")}");
                }
            }
        }
    }

    public class ChartData
    {
        Data data;
        public PlotModel plotModel { get; private set; }

        public ChartData(Data data)
        {
            this.data = data;
            this.plotModel = new PlotModel { Title = "Measured Data Chart" };
            AddSeries();
        }

        public void AddSeries()
        {
            this.plotModel.Series.Clear();
            for (int i = 0; i < data.YL.Count; i++)
            {
                OxyColor color = OxyColors.Blue;
                //(i == 0) ? color = OxyColors.Blue : color = OxyColors.Orange;

                LineSeries lineSeries = new LineSeries();
                for (int j = 0; j < data.X.Length; j++)
                    lineSeries.Points.Add(new DataPoint(data.X[j], data.YL[i][j]));
                lineSeries.Color = color;


                lineSeries.MarkerType = MarkerType.Circle;
                lineSeries.MarkerSize = 4;
                lineSeries.MarkerStroke = color;
                lineSeries.MarkerFill = color;
                lineSeries.Title = data.legends[i];

                Legend legend = new Legend();
                plotModel.Legends.Add(legend);
                this.plotModel.Series.Add(lineSeries);
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
                SplinesData.Data.Scope = value;
                OnPropertyChanged("SplinesData.Data");
            }
        }
        public ObservableCollection<string> Nodes_and_Values { get; set; }

        public ViewData(SplinesData sd, SplineParameters sp)
        {
            this.SplinesData = sd;
            this.SplineParameters = sp;

            this.Nodes_and_Values = new ObservableCollection<string>();
            Nodes_and_Values.CollectionChanged += Collection_Changed;
        }

        //..........................Коллекция узлов неравномерной сетки и значений функции в них (для вывода в ListBox)
        public void CreateCollection()
        {
            Nodes_and_Values.Clear();
            for (int i = 0; i < NonUniformNum; i++)
                Nodes_and_Values.Add("X = " + SplinesData.Data.NodeArray[i].ToString() +
                                 ";\nY = " + SplinesData.Data.ValueArray[i].ToString());
        }
        void Collection_Changed(object? sender, NotifyCollectionChangedEventArgs e)
        { OnPropertyChanged("Nodes_and_Values"); }
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        //..........................Реализация интерфейса IDataErrorInfo
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

 //______________________________________________________ГЛАВНОЕ ОКНО ПРИЛОЖЕНИЯ WPF_______________________________________________________
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

            //Chart chart = new Chart();
            //ChartView chartView;
            //OxyPlotModel oxyPlotModel;

            this.DataContext = ViewData;
            SPfBox.ItemsSource = Enum.GetValues(typeof(SPf));
            Nodes_and_Values_List.ItemsSource = ViewData.Nodes_and_Values;
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
            ViewData.CreateCollection();
            chart_data = new Data();
            chart_data.AddDefaults(ViewData.NonUniformNum, ViewData.SplinesData.Data.NodeArray, ViewData.SplinesData.Data.ValueArray);
          //this.DataContext = chart_data;

            ViewData.ChartData = new ChartData(chart_data);
            GridOxyPlot.DataContext = ViewData.ChartData;
        }

        public static RoutedCommand MakeSP = new RoutedCommand("MakeSP", typeof(WpfApp1.MainWindow));
        private void CanMakeSPHandler(object sender, CanExecuteRoutedEventArgs e)
        {
            TextBox ValidationElement = UniformNumBox;
            if (ValidationElement == null)
                return;
            if (Validation.GetHasError(ValidationElement) == true || ViewData.Nodes_and_Values.Count == 0)
            {
                e.CanExecute = false;
                return;
            }
            e.CanExecute = true;
        }
        private void MakeSPHandler(object sender, ExecutedRoutedEventArgs e)
        {

        }

        private void Splines_Click(object sender, RoutedEventArgs e)
        {

        }
    }

    //______________________________________________________________КОНВЕРТЕРЫ________________________________________________________________
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
