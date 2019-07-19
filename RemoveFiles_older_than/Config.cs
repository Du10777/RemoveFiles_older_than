using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace RemoveFiles_older_than
{
    class Config
    {
        //Проверка есть ли файл конфига
        //Если нет - создать шаблонный пример и сообщить про это
        //попытаться его считать
        //Если были ошибки во время чтения конфига - вывести сообщения и закрыть прогу
        //Проверить не стандартный ли это конфиг
        //Если стандартный - вывести сообщения и закрыть прогу

        public static bool SilentMode = false;

        public static void Open()
        {
            string exeFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
            string CfgFileName = exeFileName.Remove(exeFileName.Length - 4) + ".cfg";

            if (!File.Exists(CfgFileName))
            {
                CreateDefaultConfig(CfgFileName);
                Environment.Exit(1);
            }

            ReadConfig(CfgFileName);
            CheckConfig();
        }

        static void CreateDefaultConfig(string CfgFileName)
        {
            List<string> lines = new List<string>();
            lines.Add("# Эта программа удаляет старые файлы (определяет по времени создания) из определенного каталога");
            lines.Add("# После чего удаляет пустые файлы (размер 0 байт)");
            lines.Add("# И пустые каталоги");
            lines.Add("");
            lines.Add("# Запуск с ключом -silent отключает вывод выскакивающих сообщений об ошибке, которые не получилось записать в лог");
            lines.Add("# Если такие сообщения и будут - они не куда не выведутся и программа продолжит работу (либо закроется, если ошибка была фатальной)");
            lines.Add("");
            lines.Add("# Каталог, в котором надо искать старые файлы и пустые файлы/папки");
            lines.Add("Folder: " + defaultFolder);
            lines.Add("");
            lines.Add("# Срок жизни файлов, после превышения которого они будут удалены");
            lines.Add("# Формат - дни_часы:минуты:секунды");
            lines.Add("FilesLifeTime: " + defaultFilesLifeTime);
            lines.Add("");
            lines.Add("# Исключения. Полный путь к файлу/папке");
            lines.Add("# Эти файлы/папки будут пропущены и не удалены");
            lines.Add("Exclusion: " + defaultExclusion1);
            lines.Add("Exclusion: " + defaultExclusion2);
            lines.Add("Exclusion: " + defaultExclusion3);
            lines.Add("Exclusion: " + defaultExclusion4);
            lines.Add("");
            lines.Add("# Путь к файлу с логами. Не обязательный параметр.");
            lines.Add("# Если его не указать - логи будут храниться рядом с файлом программы.");
            lines.Add("# В этом случае убедитесь, что у пользователя (от имени которого запускается программа) есть право на запись в этот каталог");
            lines.Add("Logs: " + defaultLogsFileName);
            lines.Add("");
            lines.Add("# Максимальный размер лога (в байтах).");
            lines.Add("# при его превышении будет удален кусок файла из начала, что бы общий размер не превышал заданного максимального размера.");
            lines.Add("# Выполняется во время завершения работы программы");
            lines.Add("MaxLogSize: " + defaultMaxLogSize);

            try
            {
                File.WriteAllLines(CfgFileName, lines.ToArray());
            }
            catch (Exception ex)
            {
                Log.Add("Не могу сохранить шаблонный конфиг. " + ex.Message);
            }


            string Message = "Файл конфигурации не обнаружен.\r\n";
            Message += "Создан шаблонный файл конфигурации.\r\n";
            Message += "Ознакомтесь с ним и отредактируйте под свои нужды.\r\n";
            Message += "Путь к файлу:\r\n";
            Message += CfgFileName;
            MessageBox.Show(Message);
        }




        static void ReadConfig(string CfgFileName)
        {
            string[] lines = File.ReadAllLines(CfgFileName);

            foreach (string line in lines)
                ReadConfigLine(line);
        }
        static void ReadConfigLine(string line)
        {
            if (line.Length == 0)//Пропуск пустых строк
                return;
            if (line.StartsWith("#"))//Пропуск комментария
                return;

            string ValueName = line.Split(':')[0];
            switch (ValueName)
            {
                case "Folder":
                    Folder = GetValue(line);
                    break;
                case "FilesLifeTime":
                    FilesLifeTime = StringToTimeSpan(GetValue(line));
                    break;
                case "Exclusion":
                    Exclusions.Add(GetValue(line).ToLower());
                    break;
                case "Logs":
                    LogsFileName = GetValue(line);
                    break;
                case "MaxLogSize":
                    MaxLogSize = StringToULong(GetValue(line));
                    break;
                default:
                    break;
            }
        }

        static string GetValue(string line)
        {
            int ValueNameLength = line.Split(':')[0].Length;//Узнать длинну имени параметра
            string result = line.Remove(0, ValueNameLength + 1);//Удалить из строки имя параметра + символ двоеточия

            //Если вначале строки остались пробелы - удалять их пока они не исчезнут
            while (true)
            {
                if (result.StartsWith(" "))
                    result = result.Remove(0, 1);
                else
                    break;
            }

            return result;
        }

        static TimeSpan StringToTimeSpan(string Value)
        {
            string[] arr = Value.Split('_');
            if (arr.Length != 2)
                ConvertError(Value);

            string sDays = arr[0];

            arr = arr[1].Split(':');
            if (arr.Length != 3)
                ConvertError(Value);

            string sHours = arr[0];
            string sMinutes = arr[1];
            string sSeconds = arr[2];

            //Попытка конвертации
            try
            {
                int iDays = Convert.ToInt32(sDays);
                int iHours = Convert.ToInt32(sHours);
                int iMinutes = Convert.ToInt32(sMinutes);
                int iSeconds = Convert.ToInt32(sSeconds);
                TimeSpan result = new TimeSpan(days: iDays, hours: iHours, minutes: iMinutes, seconds: iSeconds);
                return result;
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }

            ConvertError(Value);
            throw new Exception();//Эта строка не должна выполниться. Программа должна закрыться на выполнении предидущей функции
        }
        static void ConvertError(string Value)
        {
            string Message = "Не могу сконвертировать время в понятное для программы. Пришел параметр:\r\n";
            Message += Value + "\r\n";
            Message += "\r\n";
            Message += "А должен быть формат: дни_часы:минуты:секунды\r\n";
            Message += "Правильный пример 1 день, 2 часа, 3 минуты, 4 секунды: 01_02:03:04";
            Log.Add(Message);
            Environment.Exit(-2);
        }

        static long StringToULong(string Value)
        {
            long result = 0;

            try
            {
                result = Convert.ToInt64(Value);
            }
            catch (Exception)
            {
                string Message = "Не могу сконвертировать максимальный размер лога. Пришел параметр:\r\n";
                Message += Value + "\r\n";
                Log.Add(Message);

                Environment.Exit(-6);
            }

            return result;
        }



        static void CheckConfig()
        {
            if (Folder == defaultFolder)
            {
                Log.Add("Вы используете папку по умолчанию. Её использовать нельзя. Задайте другое имя папки");
                Environment.Exit(-3);
            }
            if (TimeSpan.Compare(FilesLifeTime, new TimeSpan(days: 1, hours: 2, minutes: 3, seconds: 4)) == 0)
            {
                Log.Add("Вы используете \"Срок жизни файлов\" по умолчанию. Его использовать нельзя. Задайте другой Срок жизни файлов");
                Environment.Exit(-4);
            }
            if (LogsFileName.Length == 0)
            {
                string exeFileName = System.Reflection.Assembly.GetExecutingAssembly().Location;
                LogsFileName = exeFileName.Remove(exeFileName.Length - 4) + ".log";
            }
            if (MaxLogSize < 0)
            {
                Log.Add("Не задан максимальный размер лога. Будет использоваться размер лога по умолчанию: 10 Мб");
                MaxLogSize = defaultMaxLogSize;
            }
        }


        public static string Folder;
        public static TimeSpan FilesLifeTime;
        public static List<string> Exclusions = new List<string>();
        public static string LogsFileName;
        public static long MaxLogSize = -1;

        static string defaultFolder = @"D:\ExampleFolder";
        static string defaultFilesLifeTime = "1_02:03:04";
        static string defaultExclusion1 = @"D:\ExampleFolder\_Description.txt";
        static string defaultExclusion2 = @"D:\ExampleFolder\_PermanentFolder";
        static string defaultExclusion3 = @"D:\ExampleFolder\_PermanentFolder\";
        static string defaultExclusion4 = "";
        static string defaultLogsFileName = @"D:\ExampleLogs.txt";
        static long defaultMaxLogSize = 10*1024*1024;
    }
}
