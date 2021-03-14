using System;
using System.Collections.Generic;
using System.Text;

namespace Electro
{
    public class FindComb // В данном классе реализованы методы, ищущие оптимальную комбинацию энергоблоков
    {
        public static double[] maxPower = { 200, 280, 320, 400, 410, 490, 530, 610}; 
        public static double[] minPower = { 103, 107, 139, 143, 166, 170, 202, 206 }; // Массивы, содержащие максимальные и минимальные границы для каждой комбинации энергоблоков 
        public static void System(double[] powers) //Метод, реализующий выбор комбинации на момент старта. Получает на вход 24 значения мощностей, потребляемых в системой в каждый часиз прогноза
        {
            double max = powers[0], min = powers[0]; // Переменные, находящие максимальную и минимальную потребляемую мощности

            for(int i = 0; i < 24; i++)
            {
                max = Math.Max(max, powers[i]);
                min = Math.Max(min, powers[i]);
            }

            int mode = 0;

            if (max <= FindComb.maxPower[mode + ((mode + 1) % 2)])
            {
                if (min >= FindComb.minPower[mode]) // Система может работать ближайшие сутки без изменения режима ПТУ2/ПГУ2
                {
                }
                else
                {
                    if (PTU2.on)
                    {
                        if (powers[0] <= FindComb.maxPower[mode - 4]) // Если можем отключить энергоблок в данный час
                        {
                            int countof = 0; // Счетчик числа часов, в течение которых может быть отключен энергоблок
                            for (int i = 1; i < 24; i++) if (powers[i] <= FindComb.maxPower[mode - 4] && i == countof + 1) countof++;
                            if (countof >= 4)
                            {
                                PTU2.on = false; // Отключаем энергоблок, т.к. он имеет наименьший КПД
                                PTU2.hours = 1;
                                mode -= 4;
                            }
                        }
                    }
                    if (PGU2.on)
                    {
                        if (powers[0] < FindComb.minPower[mode]) // Если в системе все еще остается переизбыток энергии
                        {
                            PGU2.on = false; // Отключаем энергоблок
                            PGU2.hours = 1;
                            mode -= 2;
                        }
                    }
                }
            }
            else
            {
                if (!PGU2.on && !PGU2.start)
                {
                    PGU2.on = true; // Если в течение первых суток не хватает мощности, то включаем второй ПГУ
                    PGU2.hours = 0;
                }

                if (!PTU2.on && !PTU2.start)
                {
                    if (max > FindComb.maxPower[mode + ((mode + 1) % 2)]) // Если после запуска энергоблока ПГУ все равно есть нехватка мощности
                    {
                        PTU2.on = true;
                        PTU2.hours = 0; // Если мощности все еще не хватает, то включаем второй ПТУ
                    }
                }

            }


            if (GTU2.on)
            {
                double freePower = powers[0] - PGU1.nominalPower * (PGU2.on ? 2 : 1) - 0.3 * PTU1.nominalPower * (PTU2.on ? 2 : 1) - 0.1 * GTU1.nominalPower; // Находим избыток энергии, после базового распределения (ПГУ - 100%, остальные - минимальная загрузка)
                if (freePower > 0)
                {
                    if (freePower < 1.5 * GTU1.nominalPower)
                    {
                        if (GTU1.EfficiencySolo(0.1 * GTU1.nominalPower + freePower - 80) < PTU1.Efficiency(0.3 * PTU1.nominalPower * (PTU2.on ? 2 : 1) + freePower))
                        {
                            mode--;
                            GTU2.on = false;
                        }
                    }
                }
            }// Включение/выключение ГТУ2
            else
            {
                if (powers[1] > FindComb.minPower[mode + 1]) // Рассматриваем следующий час, чтобы понять необходимость включать второй энергоблок ГТУ
                {
                    double freePower = powers[1] - PGU1.nominalPower * (PGU2.on ? 2 : 1) - 0.3 * PTU1.nominalPower * (PTU2.on ? 2 : 1) - 0.1 * GTU1.nominalPower; // Находим избыток энергии, после базового распределения (ПГУ - 100%, остальные - минимальная загрузка)
                    if (freePower > 0)
                    {
                        if (freePower > 1.5 * GTU1.nominalPower)
                        {
                            mode++;
                            GTU2.start = true;
                        }
                        else if (GTU1.EfficiencySolo(0.1 * GTU1.nominalPower + freePower - 80) > PTU1.Efficiency(0.3 * PTU1.nominalPower * (PTU2.on ? 2 : 1) + freePower))
                        {
                            mode++;
                            GTU2.start = true;
                        }
                    }
                }
            }


            ModeStartSetup(mode); //Устанавливаем системе выбранные параметры
            PowerDistr.Distrib(powers[0]);
            DayGo.NewHour(mode, 1);
        }
        static void ModeStartSetup(int mode)
        {
            if (mode > 3)
            {
                PTU2.on = true;
                PTU2.hours = 0;
            }
            if (mode % 4 > 1)
            {
                PGU2.on = true;
                PGU2.hours = 0;
            }
            if(mode % 2 == 1)
            {
                GTU2.on = true;
            } //Выставление начального положения
        }
    }

    
}
