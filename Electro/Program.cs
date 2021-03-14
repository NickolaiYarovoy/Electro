using System;
using System.IO;

namespace Electro
{
    class Program
    {
        public static double[] allPowers = new double[672]; // Массив, в который записаны все значения потребления энергии в конкретный час
        public static int hourNow = 0;
        static void Main(string[] args)
        {
            string path = @"..\..\..\Data\Шаблон.csv";
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.Default)) //Считываем данные о загрузки из файла.
            {
                string line;
                for(int i = 0; i < 672; i++)
                {
                    line = sr.ReadLine();
                    allPowers[i] = Convert.ToDouble(line.Split(';')[3]);
                }
            }
            FindComb.System(allPowers);
        }
    }

    public static class PTU1 // Первый энергоблок ПТУ. Всегда включен
    {
        public const double nominalPower = 210.0; // Номинальная мощность установки
        public static double nowPower = 0; // Текущая мощность установки (обновляется каждый час)
        public static double Efficiency(double outputPower) // Метод, возращающий КПД системы энергоблоков.
        {
            double a = outputPower / nominalPower;
            if (PTU2.on)
            {
                if (a >= 1.3) return (0.102 * a * a + 0.053 * a) / (a - 0.568);
                else return (0.123 * a * a + 0.15 * a) / (a - 0.0315);
            }
            else return 0.217 + a / 7;
        }
        public static double EfficiencySolo(double outputPower) // Метод, возращающий КПД одного энергоблока данного типа.
        {
            double a = outputPower / nominalPower;
            return 0.217 + a / 7;
        }
    }
    public static class PTU2 // Второй энергоблок ПТУ. Может быть как включенным, так и выключенным
    {
        public const double nominalPower = 210.0; // Номинальная мощность установки
        public static double nowPower = 0; // Текущая мощность установки (обновляется каждый час)
        public static bool on = false; // Флаг, указывающий на текущение состояние энергоблока (включен/выключен)
        public static bool start = false; // Флаг, указывающий на текущее состояние энергоблока (запускается)
        public static int hours = 73; // Число полных часов, прошедщих с момента последнего включения энергоблока
    }
    public static class PGU1 // Первый энергоблок ПГУ. Всегда включен
    {
        public const double nominalPower = 120.0; // Номинальная мощность установки
        public static double nowPower = 0; // Текущая мощность установки (обновляется каждый час)
        public static double Efficiency(double outputPower) // Метод, возращающий КПД системы энергоблоков. См §1.1
        {
            double a = outputPower / nominalPower;
            if (PGU2.on)
            {
                if (a >= 1.3) return (0.223 * a * a - 0.109 * a) / (a - 0.797);
                else return (0.27 * a * a + 0.0567 * a) / (a - 0.568);
            }
            else return 0.189 + a * 0.371;
        }
        public static double EfficiencySolo(double outputPower) // Метод, возращающий КПД одного энергоблока данного типа.
        {
            double a = outputPower / nominalPower;
            return 0.189 + a * 0.371;
        }
    }
    public static class PGU2 // Второй энергоблок ПГУ. Может быть как включенным, так и выключенным
    {
        public const double nominalPower = 120.0; // Номинальная мощность установки
        public static double nowPower = 0; // Текущая мощность установки (обновляется каждый час)
        public static bool on = false; // Флаг, указывающий на текущение состояние энергоблока (включен/выключен)
        public static bool start = false; // Флаг, указывающий на текущее состояние энергоблока (запускается)
        public static int hours = 73; // Число полных часов, прошедщих с момента последнего включения энергоблока
    }
    public static class GTU1 // Первый энергоблок ПГУ. Всегда включен
    {
        public const double nominalPower = 80.0; // Номинальная мощность установки
        public static double nowPower = 0; // Текущая мощность установки (обновляется каждый час)
        public static double Efficiency(double outputPower) // Метод, возращающий КПД системы энергоблоков. См §1.2
        {
            double a = outputPower / nominalPower;
            if (GTU2.on)
            {
                if (a >= 1.05) return (0.4 * a * a * a * a * a * a - 3.42 * a * a * a * a * a + 11.673 * a * a * a * a - 19.945 * a * a * a + 17.205 * a * a - 5.913 * a) / (a * a * a * a * a - 8.55 * a * a * a * a + 29.183 * a * a * a - 49.863 * a * a + 43.468 * a - 15.238);
                else return (1.4 * a * a * a * a * a * a - 5.32 * a * a * a * a * a + 8.005 * a * a * a * a - 6.182 * a * a * a + 2.851 * a * a - 0.128 * a) / (a * a * a * a * a - 3.8 * a * a * a * a + 5.718 * a * a * a - 4.416 * a * a + 3.631 * a - 0.171);
            }
            else
            {
                if (a <= 0.05) return a / 0.05 * 0.07;
                else return 0.878 * a * a * a * a * a - 3.117 * a * a * a * a + 4.375 * a * a * a - 3.173 * a * a + 1.436 * a;
            }
        }
        public static double EfficiencySolo(double outputPower) // Метод, возращающий КПД одного энергоблока данного типа.
        {
            double a = outputPower / nominalPower;
            return 0.878 * a * a * a * a * a - 3.117 * a * a * a * a + 4.375 * a * a * a - 3.173 * a * a + 1.436 * a;
        }
        public static double EfficiencyDuo(double outputPower) // Метод, возращающий КПД системы двух энергоблоков. См §1.2
        {
            double a = outputPower / nominalPower;
            if (a >= 1.05) return (0.4 * a * a * a * a * a * a - 3.42 * a * a * a * a * a + 11.673 * a * a * a * a - 19.945 * a * a * a + 17.205 * a * a - 5.913 * a) / (a * a * a * a * a - 8.55 * a * a * a * a + 29.183 * a * a * a - 49.863 * a * a + 43.468 * a - 15.238);
            else return (1.4 * a * a * a * a * a * a - 5.32 * a * a * a * a * a + 8.005 * a * a * a * a - 6.182 * a * a * a + 2.851 * a * a - 0.128 * a) / (a * a * a * a * a - 3.8 * a * a * a * a + 5.718 * a * a * a - 4.416 * a * a + 3.631 * a - 0.171);
        }
    }
    public static class GTU2 // Второй энергоблок ПГУ. Может быть как включенным, так и выключенным
    {
        public const double nominalPower = 80.0; // Номинальная мощность установки
        public static double nowPower = 0; // Текущая мощность установки (обновляется каждый час)
        public static bool on = false; // Флаг, указывающий на текущение состояние энергоблока (включен/выключен)
        public static bool start = false; // Флаг, указывающий на текущее состояние энергоблока (запускается)
    }

}
