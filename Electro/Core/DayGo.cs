using System;
using System.Collections.Generic;
using System.Text;

namespace Electro
{
    public class DayGo //Класс, в котором осуществляются ежечасное отслеживание новых данных, а также переход на новую комбинацию при необходимости
    {
        public static void NewHour(int mode, int hourNum) // Основной метод, в котором рассматриваются данные о новом часе и при необходимости запускается переход на новую фазу
        {
            PTU2.hours = (PTU2.hours != 0) ? (PTU2.hours + 1) : 0; // Если вторая установка не включена, то увеличить время с последнего запуска на час
            PGU2.hours = (PGU2.hours != 0) ? (PGU2.hours + 1) : 0; // Если вторая установка не включена, то увеличить время с последнего запуска на час
            PTU2.on = (PTU2.hours == 0);
            PTU2.start = (PTU2.hours < 0);
            PGU2.on = (PGU2.hours == 0);
            PGU2.start = (PGU2.hours < 0); // Если время равно 0, то переводим установки в включенное состояние из сотояния пуска

            

            if (GTU2.start)
            {
                GTU2.start = false;
                GTU2.on = true;
            }

            mode = 4 * (PTU2.on ? 1 : 0) + 2 * (PGU2.on ? 1 : 0) + (GTU2.on ? 1 : 0);

            double[] powers = getData(hourNum);
            if (hourNum == 671)
            {
                PowerDistr.Distrib(powers[0]);
                return;
            }

            double max = powers[0], min = powers[0]; // Переменные, находящие максимальную и минимальную потребляемую мощности
            int k = Math.Min(24, 672 - hourNum);
            for (int i = 1; i < k; i++)
            {
                max = Math.Max(max, powers[i]);
                min = Math.Min(min, powers[i]);
            }


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
                            for (int i = 1; i < k; i++) if (powers[i] <= FindComb.maxPower[mode - 4] && i == countof + 1) countof++;
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
                    int time = -1;
                    for (int i = 0; i < k; i++) if (powers[i] > FindComb.maxPower[mode + ((mode + 1) % 2)] && time == -1) time = i; // Находим время, когда возникнет нехватка

                    if (!(((PGU2.hours <= 8) ? 4 : ((PGU2.hours + 1 <= 72) ? 5 : 7)) <= time)) // Если в следующем часу будет поздно запускать энергоблок
                    {
                        PGU2.start = true;
                        PGU2.hours = -((PGU2.hours <= 8) ? 4 : ((PGU2.hours + 1 <= 72) ? 5 : 7)); // Запускаем энергоблок
                    }
                }

                if (!PTU2.on && !PTU2.start)
                {
                    if (max > FindComb.maxPower[mode + ((mode + 1) % 2)]) // Если после запуска энергоблока ПГУ все равно есть нехватка мощности
                    {
                        int time = -1;
                        for (int i = 0; i < k; i++) if (powers[i] > FindComb.maxPower[mode + ((mode + 1) % 2)] && time == -1) time = i; // Находим время, когда возникнет нехватка

                        if (!(((PTU2.hours <= 8) ? 4 : ((PTU2.hours + 1 <= 72) ? 8 : 11)) <= time)) // Если в следующем часу будет поздно запускать энергоблок
                        {
                            PTU2.start = true;
                            PTU2.hours = -((PTU2.hours <= 8) ? 4 : ((PTU2.hours + 1 <= 72) ? 8 : 11)); // Запускаем энергоблок
                        }
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

            PowerDistr.Distrib(powers[0]);
            if (PGU2.hours == -1){ mode += 2; }
            if (PTU2.hours == -1) mode += 4;

            if (hourNum != 671) NewHour(mode, hourNum + 1);


        }

        static double[] getData(int hourNum)
        {
            if (hourNum < 648)
            {
                double[] data = new double[24];
                for (int i = 0; i < 24; i++)
                {
                    data[i] = Program.allPowers[i + hourNum];
                }
                return data;
            }
            else
            {
                double[] data = new double[672-hourNum];
                for (int i = 0; i < 672 - hourNum; i++)
                {
                    data[i] = Program.allPowers[i + hourNum];
                }
                return data;
            }
        }
    }
}
