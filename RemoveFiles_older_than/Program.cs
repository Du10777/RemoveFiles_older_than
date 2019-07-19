using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace RemoveFiles_older_than
{
    class Program
    {
        //Запуск с ключом -silent делает всё на автомате, без вывода сообщений пользователю
        //Без этого ключа - с выводом сообщений
        //При запуске создать конфиг с таким-же именем как у исполняемого файла программы
        //В конфиге нужны 4 переменные:
        //1. путь к папке для очистки
        //2. Срок жизни файла. Если файл старше чем указанное время - уго удаляем
        //3. Исключения. Полный путь к файлам (или папкам) исключениям
        //4. путь к файлу логов
        //Так-же надо вести логи
        //Если в конфиге не указано куда класть логи - класть их рядом

        static void Main(string[] args)
        {
            Config.SilentMode = IsSilent(args);

            Config.Open();
            Log.Open(Config.LogsFileName);
            Log.Add("--------------------Запуск программы------------------------");

            CheckFlugFile();


            DeleteOldFiles(Config.Folder);
            DeleteEmptyFolders(Config.Folder);

            Log.Add("--------------------Завершение программы--------------------");
            Log.Close();
        }

        static bool IsSilent(string[] args)
        {
            if (args.Length < 1)
                return false;
            if (args[0].ToLower() == "-silent")
                return true;

            return false;
        }


        static void CheckFlugFile()
        {//Ожидание завершения работы программы CreationDate_Changer
            string FlugFileName = Path.Combine(Config.Folder, "CreationDate_Changer_FlugFile");
            if (!File.Exists(FlugFileName))
                return;

            Log.Add("Найден файл CreationDate_Changer_FlugFile. Его наличие означает, что программа CreationDate_Changer еще не закончила свою работу. Ожидаю, пока файл пропадёт...");
            while (File.Exists(FlugFileName))
            {
                System.Threading.Thread.Sleep(1000);
            }
        }


        static void DeleteOldFiles(string FolderPath)
        {
            if (!Directory.Exists(FolderPath))
            {
                Log.Add("Не могу найти каталог для зачистки: " + FolderPath);
                Environment.Exit(-1);
            }

            string[] list = Directory.GetFiles(FolderPath, "*", SearchOption.AllDirectories);
            foreach (string fileName in list)
            {
                if (IsFileOld(fileName))
                    FileDelete(fileName);
            }

            DeleteEmptyFiles(list);
        }
        
        static bool IsFileOld(string fileName)
        {
            if (IsFileInExceptionsList(fileName))
                return false;

            DateTime Created = File.GetCreationTime(fileName);
            DateTime TimeLimitBorder = DateTime.Now.Subtract(Config.FilesLifeTime);

            //Если дата создания раньше границы - файл старый
            int CompareResult = DateTime.Compare(TimeLimitBorder, Created);
            // результат < 0 - Файл не старый
            // результат = 0 - Файл на границе
            // результат > 0 - Файл старый
            if (CompareResult > 0)
                return true;

            return false;
        }

        static bool IsFileInExceptionsList(string fileName)
        {
            fileName = fileName.ToLower();
            foreach (string excl in Config.Exclusions)
            {
                if (fileName.StartsWith(excl))//Если файл является исключением или находится в папке исключения
                    return true;
            }
            return false;
        }

        static void FileDelete(string fileName)
        {
            try
            {
                File.Delete(fileName);
                Log.Add("Удален файл: " + fileName);
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }
        }

        static void DeleteEmptyFiles(string[] list)
        {
            foreach (string file in list)
            {
                if (new FileInfo(file).Length == 0)
                {
                    FileDelete(file);
                    Log.Add("Удален пустой файл: " + file);
                }
            }
        }




        static void DeleteEmptyFolders(string FolderPath)
        {
            string[] list = Directory.GetDirectories(FolderPath);

            foreach (string dir in list)
            {
                if (IsFolderEmpty(dir))//Если папка пуста
                {
                    DirectoryDelete(dir);//Удалить
                    continue;//И перейти к следующей
                }
                else//Если не пуста
                    DeleteEmptyFolders(dir);//Попробовать "почистить" её подкаталоги

                //После очистки проверить снова
                if (IsFolderEmpty(dir))//Если папка пуста
                    DirectoryDelete(dir);//Удалить
            }
        }

        static bool IsFolderEmpty(string FolderPath)
        {
            IEnumerable<string> list = Directory.EnumerateFileSystemEntries(FolderPath);
            if (list.Count() == 0)
                return true;

            return false;
        }

        static void DirectoryDelete(string folderName)
        {
            try
            {
                Directory.Delete(folderName);
                Log.Add("Удален пустой каталог: " + folderName);
            }
            catch (Exception ex)
            {
                Log.Add(ex.Message);
            }
        }
    }
}
