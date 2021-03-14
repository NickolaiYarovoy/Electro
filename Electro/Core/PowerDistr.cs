using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Electro
{
    public class PowerDistr //В данном классе собраны методы по распределению производимой мощности по энергоблокам, а также запись данных в файл
    {
        const double gas = 0.106;

        public static void Distrib(double power) // Распределение энергии на основе текущей комбинации энергоблоков
        {
            double freePower = power - 0.3 * PTU1.nominalPower - 0.3 * PGU1.nominalPower - 0.05 * GTU1.nominalPower;
            if (PTU2.on)
            {
                PTU2.nowPower = 0.3 * PTU2.nominalPower;
                freePower -= 0.3 * PTU2.nominalPower;
            }
            else PTU2.nowPower = 0;
            if (PGU2.on)
            {
                PGU2.nowPower = 0.3 * PGU2.nominalPower;
                freePower -= 0.3 * PGU2.nominalPower;
            }
            else PGU2.nowPower = 0;
            if (GTU2.on)
            {
                GTU2.nowPower = 0.05 * GTU2.nominalPower;
                freePower -= 0.05 * GTU2.nominalPower;
            } // Определение нераспределенной мощности
            else GTU2.nowPower = 0;
            PTU1.nowPower = PTU1.nominalPower * 0.3;
            PGU1.nowPower = PGU1.nominalPower * 0.3;
            GTU1.nowPower = GTU1.nominalPower * 0.05;
            if (freePower > 0.7 * PGU1.nominalPower)
            {
                freePower -= 0.7 * PGU1.nominalPower;
                PGU1.nowPower = PGU1.nominalPower;
                if (PGU2.on)
                {
                    if (freePower > 0.7 * PGU2.nominalPower)
                    {
                        freePower -= 0.7 * PGU2.nominalPower;
                        PGU2.nowPower = PGU2.nominalPower;
                    }
                    else
                    {
                        PGU2.nowPower += freePower;
                        freePower = 0;
                    }
                }
                if (GTU2.on)
                {
                    if(freePower > 1.9 * GTU1.nominalPower) 
                    {
                        freePower -= 2 * 0.95 * GTU1.nominalPower;
                        GTU1.nowPower = GTU1.nominalPower;
                        GTU2.nowPower = GTU2.nominalPower;

                        if(freePower > 0.7 * PTU1.nominalPower)
                        {
                            PTU1.nowPower = PTU1.nominalPower;
                            freePower -= 0.7 * PTU1.nominalPower;
                            PTU2.nowPower += freePower;
                            freePower = 0;
                        }
                        else
                        {
                            PTU1.nowPower += freePower;
                            freePower = 0;
                        }

                    }
                    else if(freePower > 1.5*GTU1.nominalPower) // При загрузке в 80% КПД энергоблоков выше, чем максимальный КПД энергоблоков ПТУ
                    {
                        GTU1.nowPower = GTU1.nominalPower;
                        freePower -= 0.95 * GTU1.nominalPower;
                        GTU2.nowPower += freePower;
                        freePower = 0;
                    }   
                    else if(GTU1.Efficiency(0.1*GTU1.nominalPower + freePower) > PTU1.Efficiency((0.3 + 0.3 * Convert.ToDouble(PTU2.on)) * PTU1.nominalPower + freePower))
                    {
                        if(freePower > 0.95 * GTU1.nominalPower)
                        {
                            GTU1.nowPower = GTU1.nominalPower;
                            freePower -= 0.95 * GTU1.nominalPower;
                            GTU2.nowPower += freePower;
                            freePower = 0;
                        }
                        else
                        {
                            GTU1.nowPower += freePower;
                            freePower = 0;
                        }
                    }
                    else
                    {
                        PTU1.nowPower += freePower;
                        freePower = 0;
                    }
                }
                else
                {
                    if(freePower > 0.95 * GTU1.nominalPower)
                    {
                        GTU1.nowPower = GTU1.nominalPower;
                        freePower -= 0.95 * GTU1.nominalPower;

                        if (freePower > 0.7 * PTU1.nominalPower)
                        {
                            PTU1.nowPower = PTU1.nominalPower;
                            freePower -= 0.7 * PTU1.nominalPower;
                            PTU2.nowPower += freePower;
                            freePower = 0;
                        }
                        else
                        {
                            PTU1.nowPower += freePower;
                            freePower = 0;
                        }
                    }
                    else if(GTU1.Efficiency(0.05 * GTU1.nominalPower + freePower) > PTU1.Efficiency((0.3 + 0.3 * Convert.ToDouble(PTU2.on)) * PTU1.nominalPower + freePower))
                    {
                        GTU1.nowPower += freePower;
                        freePower = 0;
                    }
                    else
                    {
                        PTU1.nowPower += freePower;
                        freePower = 0;
                    }
                }
            }
            else
            {
                PGU1.nowPower += freePower;
                freePower = 0;
            }
            WriteDay();
        }

        public static void WriteDay() // Записывает данные текущего дня (мощности каждого энергоблока, их состояние и потребление топлива) в файл out.csv
        {
            string print = Convert.ToString(PTU1.nowPower) + ";" + Convert.ToString(PTU2.nowPower) + ";" + Convert.ToString(PGU1.nowPower) + ";" + Convert.ToString(PGU2.nowPower) + ";" +
                Convert.ToString(GTU1.nowPower) + ";" + Convert.ToString(GTU2.nowPower) + ";" + "Р" + ";";
            if (PTU2.on) print += "Р;Р;";
            else if (PTU2.start) print += "П;Р;";
            else print += "О;Р;";
            if (PGU2.on) print += "Р;Р;";
            else if (PGU2.start) print += "П;Р;";
            else print += "О;Р;";
            if (GTU2.on) print += "Р;";
            else if (GTU2.start) print += "П;";
            else print += "О;";

            print += Convert.ToString(PTU1.nowPower / PTU1.EfficiencySolo(PTU1.nowPower) * gas) + ";" + Convert.ToString(PTU2.start ? (0.3 * PTU2.nominalPower / PTU1.EfficiencySolo(0.3 * PTU2.nominalPower) * gas) : (PTU2.on ? (PTU2.nowPower / PTU1.EfficiencySolo(PTU2.nowPower) * gas) : 0)) + ";" +
                 Convert.ToString(PGU1.nowPower / PGU1.EfficiencySolo(PGU1.nowPower) * gas) + ";" + Convert.ToString(PGU2.start ? (0.3 * PGU2.nominalPower / PGU1.EfficiencySolo(0.3 * PGU2.nominalPower) * gas) : (PGU2.on ? (PGU2.nowPower / PGU1.EfficiencySolo(PGU2.nowPower) * gas) : 0)) + ";" +
                 Convert.ToString(GTU1.nowPower / GTU1.EfficiencySolo(GTU1.nowPower) * gas) + ";" + Convert.ToString(GTU2.start ? (0.3 * GTU2.nominalPower / GTU1.EfficiencySolo(0.3 * GTU1.nominalPower) * gas) : (GTU2.on ? (GTU2.nowPower / PTU1.EfficiencySolo(GTU2.nowPower) * gas) : 0)) + ";";
            string path = @"..\..\..\Data\out.csv";
            string lastData;
            using (StreamReader sr = new StreamReader(path, System.Text.Encoding.UTF8)) //Считываем данные о загрузки из файла.
            {
                lastData = sr.ReadToEnd();
            }
            using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.UTF8)) //Считываем данные о загрузки из файла.
            {
                sw.Write(lastData);
                sw.WriteLine(print);
            }
        }
    }
}
