using System;
using System.IO;
using System.Diagnostics;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Collections.Generic;

namespace FahSmpAffinityChanger
{
    public partial class FahSmpAffinityChanger : ServiceBase
    {
        // количество процессов по-умолчанию для одного SMP клиента
        const int PROCESSES_PER_CLIENT = 4;

        AffinityChanger changer = new AffinityChanger();

        #region Конструктор
        /// <summary>
        /// Конструктор
        /// </summary>
        public FahSmpAffinityChanger()
        {
            InitializeComponent();
        } 
        #endregion

        #region Установка интервала таймера
        /// <summary>
        /// Установка интервала таймера
        /// </summary>
        void SetTimerInterval()
        {
            double defaultValue = 600000;
            string keyName = @"HKEY_LOCAL_MACHINE\SOFTWARE\nick4eva's software\FAH SMP Affinity Changer";
            string valueName = "CheckInterval";

            // если нет ветки в реестре
            if (Registry.GetValue(keyName, valueName, defaultValue) == null)
            {
                // ставим параметр по умолчанию
                Registry.SetValue(keyName, valueName, defaultValue, RegistryValueKind.DWord);
            }

            // если стоит нулевое значение или нет параметра
            if ((int)Registry.GetValue(keyName, valueName, 0) == 0)
            {
                // ставим параметр по умолчанию
                Registry.SetValue(keyName, valueName, defaultValue, RegistryValueKind.DWord);
            }

            // выставляем интервал таймера из реестра
            timer.Interval = (int)Registry.GetValue(keyName, valueName, defaultValue);
        } 
        #endregion

        #region Функция сравнения для сортировки сервисов по объему используемой памяти
        /// <summary>
        /// Функция сравнения для сортировки сервисов по объему используемой памяти
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        int CompareProcessesByMemoryUsage(Process x, Process y)
        {
            if (x.WorkingSet64 > y.WorkingSet64)
            {
                return 1;
            }
            else if (x.WorkingSet64 < y.WorkingSet64)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        } 
        #endregion

        #region Функция сравнения для сортировки сервисов по пути используемого файла
        /// <summary>
        /// Функция сравнения для сортировки сервисов по пути используемого файла
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        int CompareProcessesByFilePath(Process x, Process y)
        {
            return string.Compare(x.MainModule.FileName, y.MainModule.FileName, false);
        }
        #endregion

        #region Запуск сервиса
        /// <summary>
        /// Запуск сервиса
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {           
            // устанавливаем интервал таймера
            SetTimerInterval();

            // запускаем таймер
            timer.Start();
        } 
        #endregion

        #region Остановка сервиса
        /// <summary>
        /// Остановка сервиса
        /// </summary>
        protected override void OnStop()
        {
            // останавливаем таймер
            timer.Stop();
        } 
        #endregion

        #region Обработчик таймера
        /// <summary>
        /// Обработчик таймера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            // устанавливаем интервал таймера
            SetTimerInterval();

            // получаем список процессов SMP клиента по имени (пока что это FahCore_a1)
            Process[] processes = Process.GetProcessesByName("FahCore_a1");

            // получаем количество запущенных клиентов
            int clientsCount = processes.Length / PROCESSES_PER_CLIENT;

            // сортируем список процессов по пути запуска
            Array.Sort<Process>(processes, CompareProcessesByFilePath);

            // если количество процессов кратно PROCESSES_PER_CLIENT (по умолчанию один SMP клиент запускает 4 процесса, но может быть запущено несколько SMP клиентов)
            /*if ((processes.Length > 0) && ((processes.Length % PROCESSES_PER_CLIENT) == 0))
            {
                // получаем количество SMP клиентов
                changer.smpClientsCount = processes.Length % PROCESSES_PER_CLIENT;
            }
            // иначе
            else
            {
                // выходим
                return;
            }*/

            // создаем массив SMP клиентов
            // для этого нужно разбить список найденых процессов по parent ID запустившего их процесса
            // и в зависимости от 3 параметров (кол-во процессоров, кол-во SMP клиентов, кол-во процессов у SMP клиента)

            #region Обработка для одного SMP клиента и 2 процессоров
            // если запущен один SMP клиент и в системе 2 процессора
            if ((clientsCount == 1) && (changer.CpuCount == 2))
            {
                // сортируем список процессов по потреблению памяти
                Array.Sort<Process>(processes, CompareProcessesByMemoryUsage);

                // перебираем все процессы
                for (int i = 0; i < PROCESSES_PER_CLIENT; i++)
                {
                    // если первый или четвертый процесс
                    if ((i == 0) || (i == 3))
                    {
                        // выставляем маску соответствия процессоров для первого процессора
                        changer.SetAffinity(processes[i], 0);
                    }
                    // если второй или третий процесс
                    else
                    {
                        // выставляем маску соответствия процессоров для второго процессора
                        changer.SetAffinity(processes[i], 1);
                    }
                }
            } 
            #endregion

            #region Обработка для двух SMP клиентов и 4 процессоров
            // если запущено два SMP клиента и в системе 4 процессора
            if ((clientsCount == 2) && (changer.CpuCount == 4))
            {
                SetAffinityForClients(processes, clientsCount);

                /*// разбиваем список процессов на 2 списка по SMP клиентам
                Process[] client1 = new Process[] { processes[0], processes[1], processes[2], processes[3] };
                Process[] client2 = new Process[] { processes[4], processes[5], processes[6], processes[7] };

                // сортируем список процессов первого SMP клиента по потреблению памяти
                Array.Sort<Process>(client1, CompareProcessesByMemoryUsage);
                // сортируем список процессов второго SMP клиента по потреблению памяти
                Array.Sort<Process>(client2, CompareProcessesByMemoryUsage);

                // выставляем привязку процессов в зависимости от номера SMP клиента
                changer.SetAffinity(client1[0], 0);
                changer.SetAffinity(client1[1], 2);
                changer.SetAffinity(client1[2], 2);
                changer.SetAffinity(client1[3], 0);
                changer.SetAffinity(client2[0], 1);
                changer.SetAffinity(client2[1], 3);
                changer.SetAffinity(client2[2], 3);
                changer.SetAffinity(client2[3], 1);*/
            } 
            #endregion

            #region Обработка для четырех SMP клиентов и 8 процессоров
            // если запущено четыре SMP клиента и в системе 8 процессоров
            if ((clientsCount == 4) && (changer.CpuCount == 4))
            {
                SetAffinityForClients(processes, clientsCount);

                /*// разбиваем список процессов на 4 списка по SMP клиентам
                Process[] client1 = new Process[] { processes[0], processes[1], processes[2], processes[3] };
                Process[] client2 = new Process[] { processes[4], processes[5], processes[6], processes[7] };
                Process[] client3 = new Process[] { processes[8], processes[9], processes[10], processes[11] };
                Process[] client4 = new Process[] { processes[12], processes[13], processes[14], processes[15] };

                // сортируем список процессов первого SMP клиента по потреблению памяти
                Array.Sort<Process>(client1, CompareProcessesByMemoryUsage);
                // сортируем список процессов второго SMP клиента по потреблению памяти
                Array.Sort<Process>(client2, CompareProcessesByMemoryUsage);
                // сортируем список процессов третьего SMP клиента по потреблению памяти
                Array.Sort<Process>(client3, CompareProcessesByMemoryUsage);
                // сортируем список процессов четвертого SMP клиента по потреблению памяти
                Array.Sort<Process>(client4, CompareProcessesByMemoryUsage);

                // выставляем привязку процессов в зависимости от номера SMP клиента
                changer.SetAffinity(client1[0], 0);
                changer.SetAffinity(client1[1], 2);
                changer.SetAffinity(client1[2], 2);
                changer.SetAffinity(client1[3], 0);
                changer.SetAffinity(client2[0], 1);
                changer.SetAffinity(client2[1], 3);
                changer.SetAffinity(client2[2], 3);
                changer.SetAffinity(client2[3], 1);
                changer.SetAffinity(client3[0], 4);
                changer.SetAffinity(client3[1], 6);
                changer.SetAffinity(client3[2], 6);
                changer.SetAffinity(client3[3], 4);
                changer.SetAffinity(client4[0], 5);
                changer.SetAffinity(client4[1], 7);
                changer.SetAffinity(client4[2], 7);
                changer.SetAffinity(client4[3], 5);*/
            }
            #endregion
        }
        #endregion

        #region Установка привязки к ядрам процессоров для SMP клиентов
        /// <summary>
        /// Установка привязки к ядрам процессоров для SMP клиентов
        /// </summary>
        /// <param name="processes"></param>
        /// <param name="clientsCount"></param>
        private void SetAffinityForClients(Process[] processes, int clientsCount)
        {
            List<Process[]> clients = new List<Process[]>();

            for (int i = 0; i < clientsCount; i++)
            {
                clients.Add(new Process[PROCESSES_PER_CLIENT]);

                for (int j = 0; j < PROCESSES_PER_CLIENT; j++)
                {
                    clients[i][j] = processes[i * PROCESSES_PER_CLIENT + j];
                }

                Array.Sort<Process>(clients[i], CompareProcessesByMemoryUsage);

                for (int j = 0; j < PROCESSES_PER_CLIENT; j++)
                {
                    // для 1 и 4 процесса ставим привязку к одному ядру
                    if ((j == 0) || (j == 3))
                    {
                        changer.SetAffinity(clients[i][j], i);
                    }
                    // а для 2 и 3 процесса - к другому ядру
                    else
                    {
                        changer.SetAffinity(clients[i][j], i + 2);
                    }
                }
            }
        }
        #endregion
    }
}
